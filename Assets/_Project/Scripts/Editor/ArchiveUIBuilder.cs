using QuietWitness.Archive;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace QuietWitness.EditorTools
{
    // Editor-only helper: builds the archive window under the selected Canvas
    // and wires every reference of ArchivePanel. Run it once in the Systems prefab.
    // Menu: Tools > The Quiet Witness > Build Archive UI
    public static class ArchiveUIBuilder
    {
        private static readonly Color Shade = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color Paper = new Color32(0xEE, 0xE6, 0xD3, 0xFF);
        private static readonly Color Line = new Color32(0xDD, 0xD2, 0xBB, 0xFF);
        private static readonly Color Ink = new Color32(0x2B, 0x26, 0x22, 0xFF);
        private static readonly Color Faded = new Color32(0x7A, 0x70, 0x66, 0xFF);

        [MenuItem("Tools/The Quiet Witness/Build Archive UI")]
        private static void Build()
        {
            var selected = Selection.activeTransform;
            var canvas = selected != null ? selected.GetComponentInParent<Canvas>(true) : null;
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Build Archive UI",
                    "Select the Canvas (inside the Systems prefab) first.", "OK");
                return;
            }
            if (canvas.GetComponentInChildren<ArchivePanel>(true) != null)
            {
                EditorUtility.DisplayDialog("Build Archive UI",
                    "This Canvas already has an Archive.", "OK");
                return;
            }

            // Root: always active, holds the script.
            var root = NewUI("Archive", canvas.transform);
            Stretch(root, 0f, 0f, 1f, 1f);
            var fader = canvas.transform.Find("Fader");
            if (fader != null) root.SetSiblingIndex(fader.GetSiblingIndex()); // stay under the fader

            // Window: dims the game and blocks clicks behind it.
            var window = NewUI("Window", root);
            Stretch(window, 0f, 0f, 1f, 1f);
            AddImage(window, Shade);

            var sheet = NewUI("Sheet", window);
            Stretch(sheet, 0.12f, 0.1f, 0.88f, 0.9f);
            AddImage(sheet, Paper);

            var bookTitle = NewText("BookTitle", sheet, "Phone book", 44, FontStyles.Bold, Ink);
            Stretch(bookTitle.transform, 0.04f, 0.88f, 0.6f, 0.97f);

            var closeHint = NewText("CloseHint", sheet, "Esc - close", 22, FontStyles.Italic, Faded);
            closeHint.alignment = TextAlignmentOptions.TopRight;
            Stretch(closeHint.transform, 0.6f, 0.9f, 0.96f, 0.97f);

            // Search field.
            var searchGo = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            searchGo.name = "Search";
            searchGo.transform.SetParent(sheet, false);
            Stretch(searchGo.transform, 0.04f, 0.78f, 0.44f, 0.86f);
            var search = searchGo.GetComponent<TMP_InputField>();
            search.pointSize = 28;
            if (search.placeholder is TMP_Text placeholder) placeholder.text = "Search...";

            // List of records.
            var list = NewUI("List", sheet);
            Stretch(list, 0.04f, 0.06f, 0.44f, 0.76f);
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var item = NewUI("ItemTemplate", list);
            var itemImage = AddImage(item, Line);
            var itemButton = item.gameObject.AddComponent<Button>();
            itemButton.targetGraphic = itemImage;
            var itemSize = item.gameObject.AddComponent<LayoutElement>();
            itemSize.minHeight = 56;
            itemSize.preferredHeight = 56;
            var itemLabel = NewText("Label", item, "Record", 28, FontStyles.Normal, Ink);
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(itemLabel.transform, 0f, 0f, 1f, 1f);
            ((RectTransform)itemLabel.transform).offsetMin = new Vector2(16, 0);
            var listItem = item.gameObject.AddComponent<ArchiveListItem>();
            Wire(listItem, "label", itemLabel);

            var emptyText = NewText("EmptyText", sheet, "Type a name or a place.", 26, FontStyles.Italic, Faded);
            Stretch(emptyText.transform, 0.04f, 0.6f, 0.44f, 0.76f);

            // Open record.
            var details = NewUI("Details", sheet);
            Stretch(details, 0.5f, 0.06f, 0.96f, 0.86f);

            var photoRt = NewUI("Photo", details);
            Stretch(photoRt, 0f, 0.62f, 0.3f, 1f);
            var photo = AddImage(photoRt, Color.white);
            photo.preserveAspect = true;

            var entryTitle = NewText("EntryTitle", details, "Name", 36, FontStyles.Bold, Ink);
            Stretch(entryTitle.transform, 0.34f, 0.86f, 1f, 1f);
            var entrySubtitle = NewText("EntrySubtitle", details, "Job / address", 26, FontStyles.Italic, Faded);
            Stretch(entrySubtitle.transform, 0.34f, 0.74f, 1f, 0.86f);
            var entryBody = NewText("EntryBody", details, "Record text.", 26, FontStyles.Normal, Ink);
            Stretch(entryBody.transform, 0f, 0f, 1f, 0.58f);

            // Wire the panel.
            var panel = root.gameObject.AddComponent<ArchivePanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("window").objectReferenceValue = window.gameObject;
            so.FindProperty("bookTitle").objectReferenceValue = bookTitle;
            so.FindProperty("searchField").objectReferenceValue = search;
            so.FindProperty("itemTemplate").objectReferenceValue = listItem;
            so.FindProperty("emptyText").objectReferenceValue = emptyText;
            so.FindProperty("details").objectReferenceValue = details.gameObject;
            so.FindProperty("photo").objectReferenceValue = photo;
            so.FindProperty("entryTitle").objectReferenceValue = entryTitle;
            so.FindProperty("entrySubtitle").objectReferenceValue = entrySubtitle;
            so.FindProperty("entryBody").objectReferenceValue = entryBody;
            so.ApplyModifiedPropertiesWithoutUndo();

            SetLayer(root, canvas.gameObject.layer);
            item.gameObject.SetActive(false);
            window.gameObject.SetActive(false); // hidden, so it doesn't cover the Scene view

            Undo.RegisterCreatedObjectUndo(root.gameObject, "Build Archive UI");
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("[Archive] UI built under " + canvas.name + ".");
        }

        private static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void Stretch(Transform t, float minX, float minY, float maxX, float maxY)
        {
            var rt = (RectTransform)t;
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Image AddImage(Transform t, Color color)
        {
            var image = t.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, string text,
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

        private static void Wire(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform child in t) SetLayer(child, layer);
        }
    }
}
