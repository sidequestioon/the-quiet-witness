using QuietWitness.Board;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static QuietWitness.EditorTools.UIBuild;

namespace QuietWitness.EditorTools
{
    // Editor-only helper: builds the evidence board window under the selected Canvas
    // and wires BoardPanel. Run it once in the Systems prefab.
    // Menu: Tools > The Quiet Witness > Build Board UI
    public static class BoardUIBuilder
    {
        private const string ToolName = "Build Board UI";

        private static readonly Color Shade = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color Cork = new Color32(0x8B, 0x6B, 0x4A, 0xFF);
        private static readonly Color Paper = new Color32(0xEE, 0xE6, 0xD3, 0xFF);
        private static readonly Color InkColor = new Color32(0x2B, 0x26, 0x22, 0xFF);
        private static readonly Color Faded = new Color32(0x7A, 0x70, 0x66, 0xFF);
        private static readonly Color Amber = new Color32(0xE8, 0xB0, 0x5C, 0xFF);
        private static readonly Color Thread = new Color32(0xC0, 0x1F, 0x1F, 0xFF);

        [MenuItem("Tools/The Quiet Witness/" + ToolName)]
        private static void Build()
        {
            var canvas = SelectedCanvas(ToolName);
            if (canvas == null) return;
            if (canvas.GetComponentInChildren<BoardPanel>(true) != null)
            {
                EditorUtility.DisplayDialog(ToolName, "This Canvas already has a Board.", "OK");
                return;
            }

            var root = NewUI("Board", canvas.transform);
            Stretch(root, 0f, 0f, 1f, 1f);
            PlaceUnderFader(root, canvas);

            var window = NewUI("Window", root);
            Stretch(window, 0f, 0f, 1f, 1f);
            AddImage(window, Shade);

            var sheet = NewUI("Cork", window);
            Stretch(sheet, 0.05f, 0.06f, 0.95f, 0.94f);
            AddImage(sheet, Cork);

            var title = NewText("Title", sheet, "Evidence board", 44, FontStyles.Bold, Paper);
            Stretch(title.transform, 0.03f, 0.9f, 0.6f, 0.98f);
            var hint = NewText("CloseHint", sheet, "Esc - close", 22, FontStyles.Italic, Paper);
            hint.alignment = TextAlignmentOptions.TopRight;
            Stretch(hint.transform, 0.6f, 0.92f, 0.97f, 0.98f);

            // Cards in a grid.
            var cards = NewUI("Cards", sheet);
            Stretch(cards, 0.03f, 0.24f, 0.97f, 0.88f);
            var grid = cards.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(260f, 150f);
            grid.spacing = new Vector2(48f, 40f);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(10, 10, 20, 10);

            var card = NewUI("CardTemplate", cards);
            var cardImage = AddImage(card, Paper);
            var cardButton = card.gameObject.AddComponent<Button>();
            cardButton.targetGraphic = cardImage;
            var kind = NewText("Kind", card, "CLUE", 18, FontStyles.Bold, Faded);
            Stretch(kind.transform, 0.06f, 0.72f, 0.94f, 0.92f);
            var cardTitle = NewText("Title", card, "Card title", 26, FontStyles.Bold, InkColor);
            Stretch(cardTitle.transform, 0.06f, 0.08f, 0.94f, 0.72f);
            var cardView = card.gameObject.AddComponent<BoardCardView>();
            Wire(cardView, ("background", cardImage), ("kind", kind), ("title", cardTitle));

            // Threads drawn above the cards; they never block clicks.
            var threads = NewUI("Threads", sheet);
            Stretch(threads, 0.03f, 0.24f, 0.97f, 0.88f);
            var thread = NewUI("ThreadTemplate", threads);
            thread.anchorMin = thread.anchorMax = new Vector2(0.5f, 0.5f);
            thread.sizeDelta = new Vector2(100f, 4f);
            AddImage(thread, Thread).raycastTarget = false;

            // Bottom strip: selected card and result.
            var detail = NewText("Detail", sheet, "Card description.", 26, FontStyles.Normal, Paper);
            Stretch(detail.transform, 0.03f, 0.03f, 0.66f, 0.21f);
            var status = NewText("Status", sheet, "Pick two cards to connect them.", 28, FontStyles.Bold, Amber);
            status.alignment = TextAlignmentOptions.TopRight;
            Stretch(status.transform, 0.68f, 0.03f, 0.97f, 0.21f);

            var panel = root.gameObject.AddComponent<BoardPanel>();
            Wire(panel,
                ("window", window.gameObject),
                ("cardsRoot", cards),
                ("cardTemplate", cardView),
                ("threadsRoot", threads),
                ("threadTemplate", thread),
                ("detailText", detail),
                ("statusText", status));

            SetLayer(root, canvas.gameObject.layer);
            card.gameObject.SetActive(false);
            thread.gameObject.SetActive(false);
            window.gameObject.SetActive(false); // hidden, so it doesn't cover the Scene view

            Finish(root, ToolName);
            Debug.Log("[Board] UI built under " + canvas.name + ". Now use ⋮ > Collect all conclusions on Board Panel.");
        }
    }
}
