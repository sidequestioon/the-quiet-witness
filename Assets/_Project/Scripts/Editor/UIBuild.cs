using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace QuietWitness.EditorTools
{
    // Small helpers for the editor tools that build UI.
    internal static class UIBuild
    {
        public static Canvas SelectedCanvas(string toolName)
        {
            var selected = Selection.activeTransform;
            var canvas = selected != null ? selected.GetComponentInParent<Canvas>(true) : null;
            if (canvas == null)
                EditorUtility.DisplayDialog(toolName, "Select the Canvas (inside the Systems prefab) first.", "OK");
            return canvas;
        }

        public static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(Transform t, float minX, float minY, float maxX, float maxY)
        {
            var rt = (RectTransform)t;
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Image AddImage(Transform t, Color color)
        {
            var image = t.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static TextMeshProUGUI NewText(string name, Transform parent, string text,
                                              float size, FontStyles style, Color color)
        {
            var rt = NewUI(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button NewButton(string name, Transform parent, string label,
                                       float size, Color background, Color textColor)
        {
            var rt = NewUI(name, parent);
            var image = AddImage(rt, background);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = NewText("Label", rt, label, size, FontStyles.Bold, textColor);
            text.alignment = TextAlignmentOptions.Center;
            Stretch(text.transform, 0f, 0f, 1f, 1f);
            return button;
        }

        // A vertical scroll area. Returns the Content object: put list items or a text inside it.
        public static RectTransform NewScroll(string name, Transform parent, out ScrollRect scroll)
        {
            var root = NewUI(name, parent);
            scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var viewport = NewUI("Viewport", root);
            Stretch(viewport, 0f, 0f, 1f, 1f);
            AddImage(viewport, new Color(0f, 0f, 0f, 0f)); // catches the mouse wheel
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = NewUI("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        public static void Wire(Object target, params (string field, Object value)[] links)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in links)
            {
                var property = so.FindProperty(field);
                if (property == null) Debug.LogError($"[UIBuild] No field '{field}' on {target.GetType().Name}.");
                else property.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform child in t) SetLayer(child, layer);
        }

        public static void PlaceUnderFader(Transform root, Canvas canvas)
        {
            var fader = canvas.transform.Find("Fader");
            if (fader != null) root.SetSiblingIndex(fader.GetSiblingIndex());
        }

        public static void Finish(Transform root, string undoName)
        {
            Undo.RegisterCreatedObjectUndo(root.gameObject, undoName);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
        }
    }
}
