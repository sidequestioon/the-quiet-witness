using System.Linq;
using QuietWitness.Cutscenes;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // The close-up of the envelope in the office (opens after the envelope is picked up):
    // a full-screen canvas over the game, the envelope that tears itself open, the five things inside,
    // a prompt with an icon, and a blur + darkening of the room behind (InspectBlur volume).
    // Pixel art is shown 4x (one art pixel = 4 canvas units on a 1920x1080 canvas).
    // Made again on every run. Menu: Tools > The Quiet Witness > Setup > Envelope inspect
    public static class EnvelopeInspectSetup
    {
        private const string Art = ProjectRoot + "/Art/UI/Inspect";
        private const string PromptFolder = ProjectRoot + "/Art/UI/Prompt";
        private const string FontAsset = ProjectRoot + "/Art/Fonts/Jost-Medium SDF.asset";
        private const string BlurProfile = ProjectRoot + "/Art/UI/Inspect/InspectBlur.asset";
        private const string CanvasName = "EnvelopeInspect";
        private const string BlurName = "InspectBlur";
        private const float Scale = 4f;

        // The things in the envelope: where they land (canvas units from the centre), angle, what he says.
        private static readonly (string file, Vector2 at, float angle, string line)[] Items =
        {
            ("Item_Drawing",   new Vector2(-80f,   40f), -4f,
             "A projection booth, drawn in pencil by a steady hand. Whoever drew this was standing right there. Eleven forty."),
            ("Item_Ticket",    new Vector2(300f,  230f),  5f,
             "Riviera, last show, eleven thirty. Row seven, seat twelve. Somebody wants me in that seat."),
            ("Item_Money",     new Vector2(330f,   20f), -7f,
             "Forty dollars, crumpled like somebody held them too long. Not a fee. An advance."),
            ("Item_Matchbook", new Vector2(130f, -280f), 10f,
             "The all-night diner on Main. Nobody goes there for the coffee."),
            ("Item_FilmClip",  new Vector2(420f, -230f), -9f,
             "Two frames cut out of a reel. Too small to make out by eye."),
        };

        private const float IconPixels = 73f;     // the prompt icon, as big as in the room on a 1080p screen
        private const float LabelSize = 32f;
        private static readonly Color StoryText = new Color(0.94f, 0.85f, 0.64f);

        [MenuItem("Tools/The Quiet Witness/Setup/Envelope inspect")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Envelope inspect", "Stop Play mode first.", "OK");
                return;
            }

            var tear = Enumerable.Range(0, 15).Select(i => Load($"{Art}/EnvelopeBig_{i:00}.png")).ToArray();
            if (tear.Any(s => s == null)) return;
            var itemSprites = Items.Select(it => Load($"{Art}/{it.file}.png")).ToArray();
            if (itemSprites.Any(s => s == null)) return;
            var dot = Load(ProjectRoot + "/Art/UI/Icons/Icon_Dot.png");
            var dotHover = Load(ProjectRoot + "/Art/UI/Icons/Icon_Dot_Hover.png");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAsset);
            var profile = BlurVolumeProfile();

            if (!OpenScene(Scene("Office"))) return;
            foreach (var name in new[] { CanvasName, BlurName })
            {
                var old = GameObject.Find(name);
                if (old != null) Object.DestroyImmediate(old);
            }

            // Blur + darkening of the room, faded in by the inspect.
            var blurGo = new GameObject(BlurName);
            var volume = blurGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 0f;
            volume.sharedProfile = profile;

            // Canvas over everything.
            var canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                                          typeof(GraphicRaycaster), typeof(CanvasGroup));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvas.pixelPerfect = true;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            // Black over the blurred room.
            var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(canvasGo.transform, false);
            var dim = dimGo.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0f);
            dim.raycastTarget = false;
            var dr = (RectTransform)dimGo.transform;
            dr.anchorMin = Vector2.zero; dr.anchorMax = Vector2.one; dr.offsetMin = dr.offsetMax = Vector2.zero;

            var envelope = Picture(canvasGo.transform, "Envelope", tear[0], Vector2.zero);
            var items = new InspectItem[Items.Length];
            for (int i = 0; i < Items.Length; i++)
            {
                var picture = Picture(canvasGo.transform, Items[i].file, itemSprites[i], Vector2.zero);
                picture.raycastTarget = true;                 // can be grabbed with the mouse
                var item = picture.gameObject.AddComponent<InspectItem>();
                item.lines = new[] { Items[i].line };
                // its little dot, in the top-right corner
                var dotImage = Picture(picture.transform, "Dot", dot, Vector2.zero);
                dotImage.raycastTarget = true;
                var size = picture.rectTransform.sizeDelta;
                dotImage.rectTransform.anchoredPosition = new Vector2(size.x / 2f - 28f, size.y / 2f - 28f);
                dotImage.gameObject.AddComponent<InspectDot>();
                var iso = new SerializedObject(item);
                iso.FindProperty("dot").objectReferenceValue = dotImage;
                iso.FindProperty("dotSprite").objectReferenceValue = dot;
                iso.FindProperty("dotHover").objectReferenceValue = dotHover;
                iso.ApplyModifiedPropertiesWithoutUndo();
                items[i] = item;
            }

            // Prompt: the hand icon, its name above it - like the prompt in the room.
            var promptGo = new GameObject("Prompt", typeof(RectTransform));
            promptGo.transform.SetParent(canvasGo.transform, false);
            var prompt = (RectTransform)promptGo.transform;
            prompt.sizeDelta = new Vector2(IconPixels, IconPixels);
            prompt.anchoredPosition = new Vector2(0f, -400f);
            var hand = Load($"{PromptFolder}/Prompt_Use_Story.png");
            var icon = Picture(prompt, "Icon", hand, Vector2.zero);
            icon.rectTransform.sizeDelta = new Vector2(IconPixels, IconPixels);
            icon.raycastTarget = true;
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(prompt, false);
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.fontSize = LabelSize;
            label.alignment = TextAlignmentOptions.Bottom;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.color = StoryText;
            label.text = "Open";
            label.fontSharedMaterial = Underlay(label);
            var lr = (RectTransform)labelGo.transform;
            lr.pivot = new Vector2(0.5f, 0f);
            lr.sizeDelta = new Vector2(500f, 60f);
            lr.anchoredPosition = new Vector2(0f, IconPixels / 2f + 4f);

            // What he says about a thing: a black plate with his words.
            var captionGo = new GameObject("Caption", typeof(RectTransform), typeof(Image));
            captionGo.transform.SetParent(canvasGo.transform, false);
            var plate = captionGo.GetComponent<Image>();
            plate.color = new Color(0.055f, 0.05f, 0.063f, 1f);
            plate.raycastTarget = false;
            var tailGo = new GameObject("Tail", typeof(RectTransform), typeof(Image));
            tailGo.transform.SetParent(captionGo.transform, false);
            var tail = tailGo.GetComponent<Image>();
            tail.raycastTarget = false;
            ((RectTransform)tailGo.transform).sizeDelta = new Vector2(20f, 12f);
            var captionTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            captionTextGo.transform.SetParent(captionGo.transform, false);
            var captionText = captionTextGo.GetComponent<TextMeshProUGUI>();
            if (font != null) captionText.font = font;
            captionText.fontSize = 30f;
            captionText.alignment = TextAlignmentOptions.Center;
            captionText.color = new Color(0.96f, 0.94f, 0.91f);
            captionText.raycastTarget = false;
            captionGo.SetActive(false);

            var inspect = canvasGo.AddComponent<EnvelopeInspect>();
            var so = new SerializedObject(inspect);
            so.FindProperty("group").objectReferenceValue = canvasGo.GetComponent<CanvasGroup>();
            so.FindProperty("blur").objectReferenceValue = volume;
            so.FindProperty("dim").objectReferenceValue = dim;
            so.FindProperty("envelope").objectReferenceValue = envelope;
            SetObjectList(so, "tearFrames", tear);
            so.FindProperty("emptyEnvelope").objectReferenceValue = tear[9];
            SetObjectList(so, "items", items);
            var targets = so.FindProperty("itemTargets");
            var angles = so.FindProperty("itemAngles");
            targets.arraySize = Items.Length;
            angles.arraySize = Items.Length;
            for (int i = 0; i < Items.Length; i++)
            {
                targets.GetArrayElementAtIndex(i).vector2Value = Items[i].at;
                angles.GetArrayElementAtIndex(i).floatValue = Items[i].angle;
            }
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.FindProperty("promptIcon").objectReferenceValue = icon;
            so.FindProperty("promptLabel").objectReferenceValue = label;
            so.FindProperty("handIcon").objectReferenceValue = hand;
            so.FindProperty("closeIcon").objectReferenceValue = Load($"{PromptFolder}/Prompt_Leave_Story.png");
            so.FindProperty("caption").objectReferenceValue = (RectTransform)captionGo.transform;
            so.FindProperty("captionText").objectReferenceValue = captionText;
            so.FindProperty("captionPlate").objectReferenceValue = plate;
            so.FindProperty("captionTail").objectReferenceValue = tail;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SaveOpenScene();
            Debug.Log("[Setup] Envelope inspect done. Play the prologue and take the envelope.");
        }

        // A pixel-art picture at 4x, centred.
        private static Image Picture(Transform parent, string name, Sprite sprite, Vector2 at)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height) * Scale;
            rect.anchoredPosition = at;
            return image;
        }

        // The same soft dark shadow under the name as in the room.
        private static Material Underlay(TMP_Text text)
        {
            const string path = ProjectRoot + "/Art/UI/Inspect/PromptLabel.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(text.fontSharedMaterial);
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.8f));
            mat.SetFloat("_UnderlaySoftness", 0.5f);
            mat.SetFloat("_UnderlayDilate", 0.4f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static VolumeProfile BlurVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(BlurProfile);
            if (profile != null)
            {
                // the first version was lighter
                if (profile.TryGet<ColorAdjustments>(out var old) && Mathf.Approximately(old.postExposure.value, -1.3f))
                {
                    old.postExposure.Override(-1.8f);
                    EditorUtility.SetDirty(profile);
                }
                return profile;
            }
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, BlurProfile);
            var dof = profile.Add<DepthOfField>();
            dof.mode.Override(DepthOfFieldMode.Gaussian);   // sprites write no depth: the whole room blurs
            dof.gaussianStart.Override(0f);
            dof.gaussianEnd.Override(0.5f);
            dof.gaussianMaxRadius.Override(1.5f);
            dof.highQualitySampling.Override(true);
            var colors = profile.Add<ColorAdjustments>();
            colors.postExposure.Override(-1.8f);
            colors.saturation.Override(-40f);
            var vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.45f);
            vignette.smoothness.Override(0.6f);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
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
            if (settings.spriteMode != (int)SpriteImportMode.Single || settings.spriteAlignment != (int)SpriteAlignment.Center)
            {
                settings.spriteMode = (int)SpriteImportMode.Single;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
