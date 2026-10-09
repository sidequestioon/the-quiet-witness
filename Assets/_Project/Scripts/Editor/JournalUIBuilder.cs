using QuietWitness.Dialogue;
using QuietWitness.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static QuietWitness.EditorTools.UIBuild;

namespace QuietWitness.EditorTools
{
    // Editor-only helper: builds the case file (journal) window and the "New clue" notice
    // under the selected Canvas and wires them. Run it once in the Systems prefab.
    // Menu: Tools > The Quiet Witness > Build Journal UI
    public static class JournalUIBuilder
    {
        private const string ToolName = "Build Journal UI";

        private static readonly Color Shade = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color Paper = new Color32(0xEE, 0xE6, 0xD3, 0xFF);
        private static readonly Color Line = new Color32(0xDD, 0xD2, 0xBB, 0xFF);
        private static readonly Color InkColor = new Color32(0x2B, 0x26, 0x22, 0xFF);
        private static readonly Color Faded = new Color32(0x7A, 0x70, 0x66, 0xFF);
        private static readonly Color Red = new Color32(0xB3, 0x26, 0x1E, 0xFF);
        private static readonly Color ToastBg = new Color(0.07f, 0.06f, 0.06f, 0.92f);

        [MenuItem("Tools/The Quiet Witness/" + ToolName)]
        private static void Build()
        {
            var canvas = SelectedCanvas(ToolName);
            if (canvas == null) return;
            if (canvas.GetComponentInChildren<JournalPanel>(true) != null)
            {
                EditorUtility.DisplayDialog(ToolName, "This Canvas already has a Journal.", "OK");
                return;
            }

            BuildToast(canvas);
            var root = BuildJournal(canvas);

            Finish(root, ToolName);
            Debug.Log("[Journal] UI built under " + canvas.name + ".");
        }

        private static Transform BuildJournal(Canvas canvas)
        {
            var root = NewUI("Journal", canvas.transform);
            Stretch(root, 0f, 0f, 1f, 1f);
            PlaceUnderFader(root, canvas);

            var window = NewUI("Window", root);
            Stretch(window, 0f, 0f, 1f, 1f);
            AddImage(window, Shade);

            var sheet = NewUI("Sheet", window);
            Stretch(sheet, 0.1f, 0.08f, 0.9f, 0.92f);
            AddImage(sheet, Paper);

            var title = NewText("Title", sheet, "Case file", 44, FontStyles.Bold, InkColor);
            Stretch(title.transform, 0.04f, 0.89f, 0.6f, 0.97f);
            var hint = NewText("CloseHint", sheet, "J / Esc - close", 22, FontStyles.Italic, Faded);
            hint.alignment = TextAlignmentOptions.TopRight;
            Stretch(hint.transform, 0.6f, 0.91f, 0.96f, 0.97f);

            // Tabs.
            var evidenceTab = NewButton("Tab_Evidence", sheet, "Evidence", 26, Line, InkColor);
            Stretch(evidenceTab.transform, 0.04f, 0.8f, 0.22f, 0.87f);
            var conversationsTab = NewButton("Tab_Conversations", sheet, "Conversations", 26, Line, InkColor);
            Stretch(conversationsTab.transform, 0.23f, 0.8f, 0.45f, 0.87f);
            // The open tab is not clickable: show it darker.
            foreach (var tab in new[] { evidenceTab, conversationsTab })
            {
                var colors = tab.colors;
                colors.disabledColor = new Color(0.75f, 0.7f, 0.6f, 1f);
                tab.colors = colors;
            }

            // Evidence page.
            var evidencePage = NewUI("EvidencePage", sheet);
            Stretch(evidencePage, 0.04f, 0.04f, 0.96f, 0.77f);

            var listContent = NewScroll("List", evidencePage, out var listScroll);
            Stretch(listScroll.transform, 0f, 0f, 0.4f, 1f);

            var item = NewUI("ClueTemplate", listContent);
            var itemImage = AddImage(item, Line);
            var itemButton = item.gameObject.AddComponent<Button>();
            itemButton.targetGraphic = itemImage;
            var itemSize = item.gameObject.AddComponent<LayoutElement>();
            itemSize.minHeight = 56;
            itemSize.preferredHeight = 56;
            var itemLabel = NewText("Label", item, "Clue", 28, FontStyles.Normal, InkColor);
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(itemLabel.transform, 0f, 0f, 1f, 1f);
            ((RectTransform)itemLabel.transform).offsetMin = new Vector2(16, 0);
            var clueTemplate = item.gameObject.AddComponent<DialogueChoiceButton>();
            Wire(clueTemplate, ("label", itemLabel));

            var emptyEvidence = NewText("EmptyText", evidencePage, "No evidence yet.", 26, FontStyles.Italic, Faded);
            Stretch(emptyEvidence.transform, 0f, 0.85f, 0.4f, 1f);

            var iconRt = NewUI("Icon", evidencePage);
            Stretch(iconRt, 0.45f, 0.62f, 0.62f, 1f);
            var icon = AddImage(iconRt, Color.white);
            icon.preserveAspect = true;

            var clueTitle = NewText("ClueTitle", evidencePage, "Clue title", 36, FontStyles.Bold, InkColor);
            Stretch(clueTitle.transform, 0.65f, 0.86f, 1f, 1f);
            var keyLabel = NewText("KeyLabel", evidencePage, "KEY EVIDENCE", 24, FontStyles.Bold, Red);
            Stretch(keyLabel.transform, 0.65f, 0.77f, 1f, 0.86f);
            var clueDescription = NewText("ClueDescription", evidencePage, "Clue description.", 28, FontStyles.Normal, InkColor);
            Stretch(clueDescription.transform, 0.45f, 0f, 1f, 0.58f);

            // Conversations page.
            var conversationsPage = NewUI("ConversationsPage", sheet);
            Stretch(conversationsPage, 0.04f, 0.04f, 0.96f, 0.77f);
            var logContent = NewScroll("Log", conversationsPage, out var logScroll);
            Stretch(logScroll.transform, 0f, 0f, 1f, 1f);
            var logText = NewText("LogText", logContent, "Log.", 26, FontStyles.Normal, InkColor);

            var panel = root.gameObject.AddComponent<JournalPanel>();
            Wire(panel,
                ("window", window.gameObject),
                ("evidenceTab", evidenceTab),
                ("conversationsTab", conversationsTab),
                ("evidencePage", evidencePage.gameObject),
                ("conversationsPage", conversationsPage.gameObject),
                ("clueTemplate", clueTemplate),
                ("emptyEvidence", emptyEvidence),
                ("clueIcon", icon),
                ("clueTitle", clueTitle),
                ("keyLabel", keyLabel.gameObject),
                ("clueDescription", clueDescription),
                ("logText", logText),
                ("logScroll", logScroll));

            SetLayer(root, canvas.gameObject.layer);
            item.gameObject.SetActive(false);
            conversationsPage.gameObject.SetActive(false);
            window.gameObject.SetActive(false); // hidden, so it doesn't cover the Scene view
            return root;
        }

        private static void BuildToast(Canvas canvas)
        {
            var root = NewUI("JournalToast", canvas.transform);
            Stretch(root, 0.7f, 0.86f, 0.98f, 0.97f);
            PlaceUnderFader(root, canvas);
            AddImage(root, ToastBg).raycastTarget = false;
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f; // invisible until a clue is found
            group.blocksRaycasts = false;
            group.interactable = false;

            var text = NewText("Text", root, "New clue: <b>Title</b>", 28, FontStyles.Normal, Paper);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(text.transform, 0f, 0f, 1f, 1f);
            ((RectTransform)text.transform).offsetMin = new Vector2(20, 0);

            var toast = root.gameObject.AddComponent<JournalToast>();
            Wire(toast, ("group", group), ("text", text));

            SetLayer(root, canvas.gameObject.layer);
            Undo.RegisterCreatedObjectUndo(root.gameObject, ToolName);
        }
    }
}
