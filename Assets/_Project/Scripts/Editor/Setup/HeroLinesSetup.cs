using System.IO;
using QuietWitness.Cutscenes;
using QuietWitness.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // 1. Moves everything the hero says into one file, Resources/HeroLines.asset: the line when he gets
    //    up in the prologue and the Lines of every hotspot in the office and the darkroom. Each hotspot
    //    then points at its line by id (Hotspot Actions > Line). The old Lines stay as they were (unused).
    // 2. Makes the speech plate into objects under Hero Speech (Speech, Plate, Tail) that can be styled
    //    outside Play mode: tick Hero Speech > Preview to see it over the hero.
    // Safe to run again: lines already in the file and plate objects already made are never changed.
    // Menu: Tools > The Quiet Witness > Setup > Hero lines + speech plate
    public static class HeroLinesSetup
    {
        public const string AssetPath = ProjectRoot + "/Resources/HeroLines.asset";
        private const string SpeechArt = ProjectRoot + "/Art/UI/Speech";
        private static readonly string[] Scenes = { "Office", "Darkroom" };

        // The lines file (made if missing).
        public static HeroLines Asset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<HeroLines>(AssetPath);
            if (asset != null) return asset;
            if (!AssetDatabase.IsValidFolder(ProjectRoot + "/Resources")) AssetDatabase.CreateFolder(ProjectRoot, "Resources");
            asset = ScriptableObject.CreateInstance<HeroLines>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            return asset;
        }

        public static string IdFor(string scene, string objectName, int index) =>
            (scene + "." + objectName + (index > 0 ? "." + (index + 1) : "")).ToLowerInvariant().Replace(' ', '_');

        // Adds a line if there is none with this id yet; an existing line is never changed. Returns the id.
        public static string Ensure(string id, string text, int plateWidth = 0)
        {
            var asset = Asset();
            if (asset.lines.Exists(l => l != null && l.id == id)) return id;
            asset.lines.Add(new HeroLines.Line { id = id, text = text });   // plateWidth: not used any more
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return id;
        }

        [MenuItem("Tools/The Quiet Witness/Setup/Hero lines + speech plate")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Hero lines", "Stop Play mode first.", "OK");
                return;
            }
            var plateSprite = Load($"{SpeechArt}/Speech_Plate.png", new Vector4(1, 1, 1, 1));
            var tailSprite = Load($"{SpeechArt}/Speech_Tail.png", Vector4.zero);
            if (plateSprite == null || tailSprite == null) return;
            var asset = Asset();
            int before = asset.lines.Count;

            foreach (var scene in Scenes)
            {
                if (!File.Exists(Scene(scene))) continue;
                if (!OpenScene(Scene(scene))) return;

                // The prologue: what he says when he gets up.
                foreach (var intro in Object.FindObjectsByType<PrologueIntro>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var so = new SerializedObject(intro);
                    var line = so.FindProperty("getUpLine");
                    if (string.IsNullOrWhiteSpace(line.stringValue)) line.stringValue = "prologue.get_up";
                    Ensure(line.stringValue, Join(so.FindProperty("afterGettingUp")));
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // Every hotspot: its Lines go into the file, the hotspot points at them.
                foreach (var hotspot in Object.FindObjectsByType<HotspotActions>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var so = new SerializedObject(hotspot);
                    var actions = so.FindProperty("actions");
                    for (int i = 0; i < actions.arraySize; i++)
                    {
                        var action = actions.GetArrayElementAtIndex(i);
                        var line = action.FindPropertyRelative("line");
                        if (!string.IsNullOrWhiteSpace(line.stringValue)) continue;
                        string text = Join(action.FindPropertyRelative("lines"));
                        if (string.IsNullOrWhiteSpace(text)) continue;
                        line.stringValue = Ensure(IdFor(scene, hotspot.name, i), text);
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // The speech plate as objects.
                foreach (var speech in Object.FindObjectsByType<HeroSpeech>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    BuildPlate(speech, plateSprite, tailSprite);

                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                SaveOpenScene();
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"[Setup] Hero lines: {asset.lines.Count} line(s) in {AssetPath} ({asset.lines.Count - before} new). " +
                      "Speech plate: Investigator's prompt > Hero Speech > tick Preview to style it.");
        }

        private static string Join(SerializedProperty list)
        {
            if (list == null || !list.isArray) return string.Empty;
            var parts = new string[list.arraySize];
            for (int i = 0; i < list.arraySize; i++) parts[i] = list.GetArrayElementAtIndex(i).stringValue;
            return string.Join(" ", parts).Trim();
        }

        // Speech (text), Plate and Tail under Hero Speech, set up like the plate it used to make in code.
        private static void BuildPlate(HeroSpeech speech, Sprite plateSprite, Sprite tailSprite)
        {
            var so = new SerializedObject(speech);
            float size = so.FindProperty("fontSize").floatValue;
            Color textColor = so.FindProperty("textColor").colorValue;
            Color plateColor = so.FindProperty("plateColor").colorValue;
            int order = so.FindProperty("sortingOrder").intValue;
            var material = so.FindProperty("plateMaterial").objectReferenceValue as Material;

            var textProp = so.FindProperty("text");
            if (textProp.objectReferenceValue == null)
            {
                var go = Child(speech.transform, "Speech");
                if (!go.TryGetComponent<TextMeshPro>(out var text)) text = go.AddComponent<TextMeshPro>();
                var prompt = speech.GetComponent<InteractionPrompt>();
                if (prompt != null && prompt.Font != null) text.font = prompt.Font;
                text.fontSize = size;
                text.color = textColor;
                text.alignment = TextAlignmentOptions.Top;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.rectTransform.pivot = new Vector2(0.5f, 0f);
                text.sortingOrder = order;
                text.text = "";
                text.enabled = false;
                textProp.objectReferenceValue = text;
            }
            var plateProp = so.FindProperty("plate");
            if (plateProp.objectReferenceValue == null)
            {
                var plate = MakeRenderer(speech.transform, "Plate", plateSprite, plateColor, material, order - 1);
                plate.drawMode = SpriteDrawMode.Sliced;
                plate.size = new Vector2(1f, 0.5f);
                plateProp.objectReferenceValue = plate;
            }
            var tailProp = so.FindProperty("tail");
            if (tailProp.objectReferenceValue == null)
                tailProp.objectReferenceValue = MakeRenderer(speech.transform, "Tail", tailSprite, plateColor, material, order - 1);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SpriteRenderer MakeRenderer(Transform parent, string name, Sprite sprite, Color color, Material material, int order)
        {
            var go = Child(parent, name);
            if (!go.TryGetComponent<SpriteRenderer>(out var r)) r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            if (material != null) r.sharedMaterial = material;
            r.sortingOrder = order;
            r.enabled = false;
            return r;
        }

        private static GameObject Child(Transform parent, string name)
        {
            var found = parent.Find(name);
            if (found != null) return found.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Sprite Load(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[Setup] Not found: {path}");
                return null;
            }
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteMode != (int)SpriteImportMode.Single || settings.spriteAlignment != (int)SpriteAlignment.Center ||
                settings.spriteBorder != border)
            {
                settings.spriteMode = (int)SpriteImportMode.Single;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spriteBorder = border;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
