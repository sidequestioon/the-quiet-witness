using QuietWitness.Dialogue;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static QuietWitness.EditorTools.UIBuild;

namespace QuietWitness.EditorTools
{
    // Editor-only helper: builds the "Present evidence" window under the selected Canvas,
    // wires EvidencePicker and connects it to the existing DialoguePanel.
    // Menu: Tools > The Quiet Witness > Build Evidence Picker
    public static class EvidencePickerBuilder
    {
        private const string ToolName = "Build Evidence Picker";

        private static readonly Color Shade = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color Paper = new Color32(0xEE, 0xE6, 0xD3, 0xFF);
        private static readonly Color Line = new Color32(0xDD, 0xD2, 0xBB, 0xFF);
        private static readonly Color InkColor = new Color32(0x2B, 0x26, 0x22, 0xFF);
        private static readonly Color Faded = new Color32(0x7A, 0x70, 0x66, 0xFF);
        private static readonly Color Red = new Color32(0xB3, 0x26, 0x1E, 0xFF);

        [MenuItem("Tools/The Quiet Witness/" + ToolName)]
        private static void Build()
        {
            var canvas = SelectedCanvas(ToolName);
            if (canvas == null) return;

            var dialoguePanel = canvas.GetComponentInChildren<DialoguePanel>(true);
            if (dialoguePanel == null)
            {
                EditorUtility.DisplayDialog(ToolName, "Build the Dialogue UI first.", "OK");
                return;
            }
            if (canvas.GetComponentInChildren<EvidencePicker>(true) != null)
            {
                EditorUtility.DisplayDialog(ToolName, "This Canvas already has an Evidence Picker.", "OK");
                return;
            }

            var root = NewUI("EvidencePicker", canvas.transform);
            Stretch(root, 0f, 0f, 1f, 1f);
            PlaceUnderFader(root, canvas); // on top of the dialogue box, under the fader

            var window = NewUI("Window", root);
            Stretch(window, 0f, 0f, 1f, 1f);
            AddImage(window, Shade);

            var sheet = NewUI("Sheet", window);
            Stretch(sheet, 0.15f, 0.12f, 0.85f, 0.88f);
            AddImage(sheet, Paper);

            var title = NewText("Title", sheet, "Present evidence", 44, FontStyles.Bold, InkColor);
            Stretch(title.transform, 0.04f, 0.87f, 0.96f, 0.97f);

            // List of clues.
            var list = NewUI("List", sheet);
            Stretch(list, 0.04f, 0.06f, 0.42f, 0.84f);
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
            var itemLabel = NewText("Label", item, "Clue", 28, FontStyles.Normal, InkColor);
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(itemLabel.transform, 0f, 0f, 1f, 1f);
            ((RectTransform)itemLabel.transform).offsetMin = new Vector2(16, 0);
            var itemComponent = item.gameObject.AddComponent<DialogueChoiceButton>();
            Wire(itemComponent, ("label", itemLabel));

            var emptyText = NewText("EmptyText", sheet, "No evidence yet.", 26, FontStyles.Italic, Faded);
            Stretch(emptyText.transform, 0.04f, 0.7f, 0.42f, 0.84f);

            // Selected clue.
            var iconRt = NewUI("Icon", sheet);
            Stretch(iconRt, 0.48f, 0.6f, 0.66f, 0.84f);
            var icon = AddImage(iconRt, Color.white);
            icon.preserveAspect = true;

            var clueTitle = NewText("ClueTitle", sheet, "Clue title", 34, FontStyles.Bold, InkColor);
            Stretch(clueTitle.transform, 0.69f, 0.7f, 0.96f, 0.84f);

            var clueDescription = NewText("ClueDescription", sheet, "Clue description.", 26, FontStyles.Normal, InkColor);
            Stretch(clueDescription.transform, 0.48f, 0.18f, 0.96f, 0.56f);

            var back = NewButton("Back", sheet, "Back (Esc)", 26, Line, InkColor);
            Stretch(back.transform, 0.48f, 0.05f, 0.7f, 0.14f);
            var present = NewButton("Present", sheet, "Present", 28, Red, Paper);
            Stretch(present.transform, 0.74f, 0.05f, 0.96f, 0.14f);

            var picker = root.gameObject.AddComponent<EvidencePicker>();
            Wire(picker,
                ("window", window.gameObject),
                ("itemTemplate", itemComponent),
                ("emptyText", emptyText),
                ("clueIcon", icon),
                ("clueTitle", clueTitle),
                ("clueDescription", clueDescription),
                ("presentButton", present),
                ("backButton", back));
            Wire(dialoguePanel, ("picker", picker));

            SetLayer(root, canvas.gameObject.layer);
            item.gameObject.SetActive(false);
            window.gameObject.SetActive(false); // hidden, so it doesn't cover the Scene view

            Finish(root, ToolName);
            Debug.Log("[Dialogue] Evidence picker built and connected to " + dialoguePanel.name + ".");
        }
    }
}
