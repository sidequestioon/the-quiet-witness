using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // Two style passes:
    //  - Jost everywhere: every text in every scene and prefab gets the Jost font, plain (no bold, no italic);
    //    new texts get Jost too (TMP Settings > Default Font Asset). Materials made from Jost (e.g. the
    //    soft shadow under the prompt names) are kept.
    //  - The question box (Systems > Canvas > Confirm, e.g. "Am I done here?"): a dark plate like the
    //    other plates, Jost text, a yellow "Go" and a dark "Stay" button.
    // Menu: Tools > The Quiet Witness > Setup > Style: Jost everywhere / Style: question box
    public static class StyleSetup
    {
        private const string FontAsset = ProjectRoot + "/Art/Fonts/Jost-Medium SDF.asset";
        private const string TmpSettings = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string PlateSprite = ProjectRoot + "/Art/UI/Inspect/Caption_Plate.png";

        private static readonly Color Plain = new Color(0.96f, 0.94f, 0.91f, 1f);
        private static readonly Color Yellow = new Color(0.94f, 0.85f, 0.64f, 1f);
        private static readonly Color Dark = new Color(0.08f, 0.07f, 0.09f, 0.96f);
        private static readonly Color DarkButton = new Color(0.2f, 0.19f, 0.22f, 1f);

        [MenuItem("Tools/The Quiet Witness/Setup/Style: Jost everywhere")]
        private static void JostEverywhere()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorUtility.DisplayDialog("Style", "Stop Play mode first.", "OK"); return; }
            var jost = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAsset);
            if (jost == null) { Debug.LogError($"[Setup] Not found: {FontAsset}"); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            int changed = 0;
            // Prefabs
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { ProjectRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int n = Fix(root.GetComponentsInChildren<TMP_Text>(true), jost);
                    if (n > 0) { PrefabUtility.SaveAsPrefabAsset(root, path); changed += n; }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            // Scenes
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { ProjectRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int n = 0;
                foreach (var go in scene.GetRootGameObjects()) n += Fix(go.GetComponentsInChildren<TMP_Text>(true), jost);
                if (n > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); changed += n; }
            }
            // New texts start with Jost
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(TmpSettings);
            if (settings != null)
            {
                var so = new SerializedObject(settings);
                var prop = so.FindProperty("m_defaultFontAsset");
                if (prop != null) { prop.objectReferenceValue = jost; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
            AssetDatabase.SaveAssets();
            if (File.Exists(Scene("Office"))) OpenScene(Scene("Office"));
            Debug.Log($"[Setup] Jost everywhere: {changed} text(s) changed.");
        }

        private static int Fix(TMP_Text[] texts, TMP_FontAsset jost)
        {
            int n = 0;
            foreach (var t in texts)
            {
                bool dirty = false;
                if (t.font != jost)
                {
                    t.font = jost;
                    t.fontSharedMaterial = jost.material;
                    dirty = true;
                }
                else if (t.fontSharedMaterial == null || t.fontSharedMaterial.mainTexture != jost.atlasTexture)
                {
                    t.fontSharedMaterial = jost.material;     // a material from another font
                    dirty = true;
                }
                if (t.fontStyle != FontStyles.Normal)
                {
                    t.fontStyle = FontStyles.Normal;
                    dirty = true;
                }
                if (dirty) { EditorUtility.SetDirty(t); n++; }
            }
            return n;
        }

        [MenuItem("Tools/The Quiet Witness/Setup/Style: question box")]
        private static void QuestionBox()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorUtility.DisplayDialog("Style", "Stop Play mode first.", "OK"); return; }
            var jost = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAsset);
            var plate = AssetDatabase.LoadAssetAtPath<Sprite>(PlateSprite);
            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                var panel = root.GetComponentInChildren<QuietWitness.UI.ConfirmPanel>(true);
                if (panel == null) { Debug.LogError("[Setup] No question box (Confirm) on Systems."); return; }
                var window = panel.transform.Find("Window");
                var box = window != null ? window.Find("Box") : null;
                if (box == null) { Debug.LogError("[Setup] Confirm > Window > Box not found."); return; }

                // Darkened screen behind
                if (window.TryGetComponent<Image>(out var shade)) shade.color = new Color(0f, 0f, 0f, 0.55f);

                // The plate: fixed size in the middle
                var boxRect = (RectTransform)box;
                boxRect.anchorMin = boxRect.anchorMax = boxRect.pivot = new Vector2(0.5f, 0.5f);
                boxRect.anchoredPosition = Vector2.zero;
                boxRect.sizeDelta = new Vector2(720f, 250f);
                Plate(box.GetComponent<Image>(), plate, Dark);

                // No title for his question
                var title = box.Find("Title");
                if (title != null) title.gameObject.SetActive(false);

                // The question
                var message = box.Find("Message");
                if (message != null && message.TryGetComponent<TMP_Text>(out var text))
                {
                    var r = (RectTransform)message;
                    r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
                    r.offsetMin = new Vector2(44f, 96f); r.offsetMax = new Vector2(-44f, -30f);
                    Text(text, jost, 30f, Plain);
                    text.alignment = TextAlignmentOptions.Center;
                    text.textWrappingMode = TextWrappingModes.Normal;
                }

                // Buttons: Stay (dark) on the left, Go (yellow) on the right
                Button(box.Find("No"), plate, DarkButton, jost, Plain, new Vector2(-130f, 52f));
                Button(box.Find("Yes"), plate, Yellow, jost, new Color(0.1f, 0.09f, 0.1f, 1f), new Vector2(130f, 52f));

                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("[Setup] Question box styled. To see it: Systems > Canvas > Confirm > Window (tick it on in the prefab).");
        }

        private static void Plate(Image image, Sprite plate, Color color)
        {
            if (image == null) return;
            if (plate != null)
            {
                image.sprite = plate;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 100f / (plate.pixelsPerUnit * 4f);   // pixel art at 4x
            }
            image.color = color;
        }

        private static void Text(TMP_Text text, TMP_FontAsset jost, float size, Color color)
        {
            if (jost != null) { text.font = jost; text.fontSharedMaterial = jost.material; }
            text.fontSize = size;
            text.fontStyle = FontStyles.Normal;
            text.color = color;
        }

        private static void Button(Transform t, Sprite plate, Color fill, TMP_FontAsset jost, Color label, Vector2 at)
        {
            if (t == null) return;
            var r = (RectTransform)t;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = at;
            r.sizeDelta = new Vector2(220f, 58f);
            Plate(t.GetComponent<Image>(), plate, fill);
            if (t.TryGetComponent<UnityEngine.UI.Button>(out var b))
            {
                var c = b.colors;
                c.normalColor = Color.white;
                c.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                c.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                c.selectedColor = Color.white;
                b.colors = c;
            }
            var labelText = t.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
            if (labelText != null) { Text(labelText, jost, 26f, label); labelText.alignment = TextAlignmentOptions.Center; }
        }
    }
}
