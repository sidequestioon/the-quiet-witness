using QuietWitness.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // The floppy disk that shows in the bottom-right corner while the game saves.
    // Lives on the Systems canvas, so it is in every scene. Safe to run again.
    // Menu: Tools > The Quiet Witness > Setup > Save icon
    public static class SaveIndicatorSetup
    {
        private const string Folder = ProjectRoot + "/Art/UI/Save";
        private const string Name = "SaveIndicator";
        private const float Scale = 4f;          // 18 px picture -> 72 px on a 1920x1080 screen
        private const float Margin = 32f;

        [MenuItem("Tools/The Quiet Witness/Setup/Save icon")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Save icon", "Stop Play mode first.", "OK");
                return;
            }
            var frames = new Sprite[3];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = Load($"{Folder}/Save_Floppy_{i}.png");
                if (frames[i] == null) return;
            }

            EditSystemsCanvas(canvas =>
            {
                var old = canvas.transform.Find(Name);
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var go = new GameObject(Name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                go.transform.SetParent(canvas.transform, false);
                go.transform.SetAsLastSibling();                       // over everything else
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-Margin, Margin);
                rect.sizeDelta = new Vector2(frames[0].rect.width, frames[0].rect.height) * Scale;

                var image = go.GetComponent<Image>();
                image.sprite = frames[0];
                image.raycastTarget = false;
                image.preserveAspect = true;

                var indicator = go.AddComponent<SaveIndicator>();
                var so = new SerializedObject(indicator);
                so.FindProperty("image").objectReferenceValue = image;
                SetObjectList(so, "frames", frames);
                so.ApplyModifiedPropertiesWithoutUndo();
            });
            Debug.Log("[Setup] Save icon done. It shows in the bottom-right corner when the game saves.");
        }

        private static Sprite Load(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[Setup] Not found: {path}");
                return null;
            }
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
