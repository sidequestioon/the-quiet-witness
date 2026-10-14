using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace QuietWitness.EditorTools
{
    // Small helpers for setups that build screens out of UI objects.
    internal static class UiBuild
    {
        public static readonly Color Plain = new Color(0.96f, 0.94f, 0.91f, 1f);
        public static readonly Color Yellow = new Color(0.94f, 0.85f, 0.64f, 1f);
        public static readonly Color Plate = new Color(0.055f, 0.05f, 0.063f, 0.92f);

        public static GameObject Canvas(Transform parent, string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            canvas.pixelPerfect = true;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            return go;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        // Anchored to the middle of the parent unless an anchor is given.
        public static void Place(RectTransform r, Vector2 at, Vector2 size, Vector2? anchor = null, Vector2? pivot = null)
        {
            r.anchorMin = r.anchorMax = anchor ?? new Vector2(0.5f, 0.5f);
            r.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            r.anchoredPosition = at;
            r.sizeDelta = size;
        }

        public static Image Img(string name, Transform parent, Sprite sprite, Color color, bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        // A pixel-art picture at a whole scale, centred at "at".
        public static Image Pixel(string name, Transform parent, Sprite sprite, float scale, Vector2 at, bool raycast = false)
        {
            var image = Img(name, parent, sprite, Color.white, raycast);
            Place(image.rectTransform, at, new Vector2(sprite.rect.width, sprite.rect.height) * scale);
            return image;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, float size, Color color,
                                           TextAlignmentOptions align, string text = "")
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }

        // Single sprite, centre pivot (and a 9-slice border if given).
        public static Sprite LoadSprite(string path, Vector4 border = default)
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

        // The prompt like in the room: the icon, its name above with a soft shadow.
        public static (RectTransform root, Image icon, TextMeshProUGUI label) Prompt(Transform parent, TMP_FontAsset font,
                                                                                   Material labelMaterial, Sprite icon, Vector2 at)
        {
            var root = Rect("Prompt", parent);
            Place(root, at, new Vector2(240f, 140f));
            var image = Img("Icon", root, icon, Color.white, raycast: true);
            Place(image.rectTransform, new Vector2(0f, -30f), new Vector2(73f, 73f));
            var label = Text("Label", root, font, 32f, Yellow, TextAlignmentOptions.Bottom, "Take");
            Place(label.rectTransform, new Vector2(0f, 20f), new Vector2(400f, 50f));
            if (labelMaterial != null) label.fontSharedMaterial = labelMaterial;
            return (root, image, label);
        }

        // The plate with what the hero thinks: sliced plate picture, text inside (wraps).
        public static (RectTransform plate, TextMeshProUGUI text) Caption(Transform parent, TMP_FontAsset font, Sprite plateSprite,
                                                                       Vector2 topCentre)
        {
            var plate = Img("Caption", parent, plateSprite, Plate);
            if (plateSprite != null)
            {
                plate.type = Image.Type.Sliced;
                plate.pixelsPerUnitMultiplier = 100f / (plateSprite.pixelsPerUnit * 4f);   // pixel art at 4x
            }
            Place(plate.rectTransform, topCentre, new Vector2(600f, 90f), pivot: new Vector2(0.5f, 1f));
            var text = Text("Text", plate.rectTransform, font, 30f, Plain, TextAlignmentOptions.Center, "...");
            text.textWrappingMode = TextWrappingModes.Normal;
            Stretch(text.rectTransform);
            return (plate.rectTransform, text);
        }
    }
}
