using QuietWitness.Core;
using QuietWitness.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // "NEW EVIDENCE" / "NEW ENTRY" in small yellow letters in the bottom-left corner (no plate),
    // on the case file's canvas (Systems).
    // Rebuilt on every run. Menu: Tools > The Quiet Witness > Setup > Case file: new entry notice
    public static class CaseFileNoticeSetup
    {
        private const string FontAsset = ProjectRoot + "/Art/Fonts/Jost-Medium SDF.asset";
        private const string ObjectivesPath = ProjectRoot + "/Cases/Objectives.asset";
        private const string Name = "Notice";

        private static readonly Color Yellow = new Color(0.94f, 0.85f, 0.64f, 1f);

        [MenuItem("Tools/The Quiet Witness/Setup/Case file: new entry notice")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Notice", "Stop Play mode first.", "OK");
                return;
            }
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAsset);
            var objectives = AssetDatabase.LoadAssetAtPath<ObjectiveList>(ObjectivesPath);

            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                var panel = root.GetComponentInChildren<CaseFilePanel>(true);
                if (panel == null)
                {
                    EditorUtility.DisplayDialog("Notice", "No case file on Systems: run Setup > Case file first.", "OK");
                    return;
                }
                var old = panel.transform.Find(Name);
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var notice = Rect(Name, panel.transform);
                notice.anchorMin = notice.anchorMax = Vector2.zero;
                notice.pivot = Vector2.zero;
                notice.anchoredPosition = new Vector2(56f, 52f);
                notice.sizeDelta = new Vector2(600f, 120f);
                var group = notice.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.blocksRaycasts = false;

                // just two words in yellow, no plate: NEW EVIDENCE / NEW ENTRY
                var plate = Rect("Words", notice);
                plate.anchorMin = plate.anchorMax = Vector2.zero;
                plate.pivot = Vector2.zero;
                plate.anchoredPosition = Vector2.zero;
                plate.sizeDelta = new Vector2(500f, 40f);
                var kind = Text("Kind", plate, font, 24f, Yellow);
                kind.characterSpacing = 10f;
                kind.alignment = TextAlignmentOptions.BottomLeft;
                kind.textWrappingMode = TextWrappingModes.NoWrap;
                var r = kind.rectTransform;
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                kind.text = "NEW EVIDENCE";
                // a soft dark shadow under the letters (like the names over things), so they read on any wall
                var shadow = AssetDatabase.LoadAssetAtPath<Material>(ProjectRoot + "/Art/UI/Inspect/PromptLabel.mat");
                if (shadow != null) kind.fontSharedMaterial = shadow;
                Image icon = null;
                TextMeshProUGUI title = null;

                var comp = notice.gameObject.AddComponent<CaseFileNotice>();
                var so = new SerializedObject(comp);
                so.FindProperty("group").objectReferenceValue = group;
                so.FindProperty("plate").objectReferenceValue = plate;
                so.FindProperty("icon").objectReferenceValue = icon;
                so.FindProperty("kind").objectReferenceValue = kind;
                so.FindProperty("title").objectReferenceValue = title;
                so.FindProperty("objectives").objectReferenceValue = objectives;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] New entry notice done. To see it: Systems > CaseFile > Notice > Preview.");
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) { t.font = font; t.fontSharedMaterial = font.material; }
            t.fontSize = size;
            t.fontStyle = FontStyles.Normal;
            t.color = color;
            t.alignment = TextAlignmentOptions.Left;
            t.raycastTarget = false;
            return t;
        }
    }
}
