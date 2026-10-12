using QuietWitness.Cutscenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // Makes the plate with the hero's words in the envelope close-up editable outside Play mode:
    // the plate and its tail get real pictures (Caption_Plate / Caption_Tail, 9-sliced pixel corners),
    // and the Caption is switched on with a sample text so it can be seen and styled in the Scene view.
    // Only touches the Caption: lines, dots and positions you tuned stay as they are.
    // Menu: Tools > The Quiet Witness > Setup > Envelope caption (editable)
    public static class EnvelopeCaptionSetup
    {
        private const string Plate = ProjectRoot + "/Art/UI/Inspect/Caption_Plate.png";
        private const string Tail = ProjectRoot + "/Art/UI/Inspect/Caption_Tail.png";

        [MenuItem("Tools/The Quiet Witness/Setup/Envelope caption (editable)")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Envelope caption", "Stop Play mode first.", "OK");
                return;
            }
            var plateSprite = Load(Plate, new Vector4(1, 1, 1, 1));
            var tailSprite = Load(Tail, Vector4.zero);
            if (plateSprite == null || tailSprite == null) return;

            if (!OpenScene(Scene("Office"))) return;
            var inspect = Object.FindFirstObjectByType<EnvelopeInspect>(FindObjectsInactive.Include);
            if (inspect == null)
            {
                EditorUtility.DisplayDialog("Envelope caption", "Run Setup > Envelope inspect first.", "OK");
                return;
            }
            var so = new SerializedObject(inspect);
            var caption = so.FindProperty("caption").objectReferenceValue as RectTransform;
            var plate = so.FindProperty("captionPlate").objectReferenceValue as Image;
            var tail = so.FindProperty("captionTail").objectReferenceValue as Image;
            var text = so.FindProperty("captionText").objectReferenceValue as TMPro.TMP_Text;
            if (caption == null || plate == null || text == null)
            {
                EditorUtility.DisplayDialog("Envelope caption", "The Caption is missing: run Setup > Envelope inspect again.", "OK");
                return;
            }

            // One art pixel of the corners = 4 canvas units (the canvas counts 100 units per Unity unit).
            float multiplier = 100f / (plateSprite.pixelsPerUnit * 4f);
            plate.sprite = plateSprite;
            plate.type = Image.Type.Sliced;
            plate.pixelsPerUnitMultiplier = multiplier;
            if (tail != null)
            {
                tail.sprite = tailSprite;
                tail.rectTransform.sizeDelta = new Vector2(5f, 3f) * 4f;
                tail.color = plate.color;
            }
            if (string.IsNullOrEmpty(text.text))
                text.text = "A projection booth, drawn in pencil by a steady hand. Whoever drew this was standing right there.";
            caption.gameObject.SetActive(true);
            caption.anchoredPosition = new Vector2(0f, -300f);
            EditorUtility.SetDirty(plate);
            EditorUtility.SetDirty(caption);
            Selection.activeGameObject = caption.gameObject;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SaveOpenScene();
            Debug.Log("[Setup] Envelope caption: select Caption / Text in the Hierarchy and style it. It hides itself in the game.");
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
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteBorder = border;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
