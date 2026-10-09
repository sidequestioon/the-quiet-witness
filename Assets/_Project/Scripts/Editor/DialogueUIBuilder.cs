using QuietWitness.Dialogue;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static QuietWitness.EditorTools.UIBuild;

namespace QuietWitness.EditorTools
{
    // Editor-only helper: builds the dialogue box under the selected Canvas
    // and wires every reference of DialoguePanel. Run it once in the Systems prefab.
    // Menu: Tools > The Quiet Witness > Build Dialogue UI
    public static class DialogueUIBuilder
    {
        private const string ToolName = "Build Dialogue UI";

        private static readonly Color Box = new Color(0.07f, 0.06f, 0.06f, 0.92f);
        private static readonly Color Cream = new Color32(0xEE, 0xE6, 0xD3, 0xFF);
        private static readonly Color Amber = new Color32(0xE8, 0xB0, 0x5C, 0xFF);
        private static readonly Color ChoiceBg = new Color(0.15f, 0.13f, 0.12f, 0.95f);

        [MenuItem("Tools/The Quiet Witness/" + ToolName)]
        private static void Build()
        {
            var canvas = SelectedCanvas(ToolName);
            if (canvas == null) return;
            if (canvas.GetComponentInChildren<DialoguePanel>(true) != null)
            {
                EditorUtility.DisplayDialog(ToolName, "This Canvas already has a Dialogue box.", "OK");
                return;
            }

            var root = NewUI("Dialogue", canvas.transform);
            Stretch(root, 0f, 0f, 1f, 1f);
            PlaceUnderFader(root, canvas);

            var window = NewUI("Window", root);
            Stretch(window, 0f, 0f, 1f, 1f);

            // Box at the bottom.
            var box = NewUI("Box", window);
            Stretch(box, 0.08f, 0.04f, 0.92f, 0.3f);
            AddImage(box, Box);

            var speakerBox = NewUI("Speaker", box);
            Stretch(speakerBox, 0.025f, 0.78f, 0.6f, 0.95f);
            var speakerText = NewText("SpeakerText", speakerBox, "Name", 30, FontStyles.Bold, Amber);
            Stretch(speakerText.transform, 0f, 0f, 1f, 1f);

            var lineText = NewText("LineText", box, "Dialogue line.", 30, FontStyles.Normal, Cream);
            Stretch(lineText.transform, 0.025f, 0.1f, 0.975f, 0.76f);

            var hint = NewText("ContinueHint", box, "E", 24, FontStyles.Bold, Amber);
            hint.alignment = TextAlignmentOptions.BottomRight;
            Stretch(hint.transform, 0.9f, 0.05f, 0.98f, 0.25f);

            // Choices above the box, growing upwards.
            var choices = NewUI("Choices", window);
            Stretch(choices, 0.08f, 0.32f, 0.62f, 0.75f);
            var layout = choices.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var item = NewUI("ChoiceTemplate", choices);
            var itemImage = AddImage(item, ChoiceBg);
            var itemButton = item.gameObject.AddComponent<Button>();
            itemButton.targetGraphic = itemImage;
            var itemSize = item.gameObject.AddComponent<LayoutElement>();
            itemSize.minHeight = 52;
            itemSize.preferredHeight = 52;
            var itemLabel = NewText("Label", item, "1. Choice", 28, FontStyles.Normal, Cream);
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(itemLabel.transform, 0f, 0f, 1f, 1f);
            ((RectTransform)itemLabel.transform).offsetMin = new Vector2(16, 0);
            var choiceButton = item.gameObject.AddComponent<DialogueChoiceButton>();
            Wire(choiceButton, ("label", itemLabel));

            var panel = root.gameObject.AddComponent<DialoguePanel>();
            Wire(panel,
                ("window", window.gameObject),
                ("speakerBox", speakerBox.gameObject),
                ("speakerText", speakerText),
                ("lineText", lineText),
                ("continueHint", hint.gameObject),
                ("choiceTemplate", choiceButton));

            SetLayer(root, canvas.gameObject.layer);
            item.gameObject.SetActive(false);
            window.gameObject.SetActive(false); // hidden, so it doesn't cover the Scene view

            Finish(root, ToolName);
            Debug.Log("[Dialogue] UI built under " + canvas.name + ".");
        }
    }
}
