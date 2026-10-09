using QuietWitness.Court;
using QuietWitness.Dialogue;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static QuietWitness.EditorTools.UIBuild;

namespace QuietWitness.EditorTools
{
    // Editor-only helper: builds the courtroom screen under the selected Canvas.
    // Menu: Tools > The Quiet Witness > Build Court UI (the Setup > Court script calls it too).
    public static class CourtUIBuilder
    {
        private const string ToolName = "Build Court UI";

        private static readonly Color Wood = new Color(0.12f, 0.08f, 0.06f, 0.97f);
        private static readonly Color Cream = new Color32(0xEE, 0xE6, 0xD3, 0xFF);
        private static readonly Color Amber = new Color32(0xE8, 0xB0, 0x5C, 0xFF);
        private static readonly Color CardBg = new Color(0.22f, 0.16f, 0.12f, 1f);
        private static readonly Color Red = new Color32(0xB3, 0x26, 0x1E, 0xFF);
        private static readonly Color Line = new Color32(0xDD, 0xD2, 0xBB, 0xFF);
        private static readonly Color InkColor = new Color32(0x2B, 0x26, 0x22, 0xFF);

        [MenuItem("Tools/The Quiet Witness/" + ToolName)]
        private static void Build()
        {
            var canvas = SelectedCanvas(ToolName);
            if (canvas == null) return;
            if (canvas.GetComponentInChildren<CourtPanel>(true) != null)
            {
                EditorUtility.DisplayDialog(ToolName, "This Canvas already has a Court.", "OK");
                return;
            }
            Finish(BuildAll(canvas), ToolName);
        }

        internal static Transform BuildAll(Canvas canvas)
        {
            var root = NewUI("Court", canvas.transform);
            Stretch(root, 0f, 0f, 1f, 1f);
            PlaceUnderFader(root, canvas);

            var window = NewUI("Window", root);
            Stretch(window, 0f, 0f, 1f, 1f);
            AddImage(window, Wood);

            var header = NewText("Header", window, "The People v. ...", 28, FontStyles.Italic, Line);
            header.alignment = TextAlignmentOptions.Center;
            Stretch(header.transform, 0.05f, 0.9f, 0.95f, 0.97f);

            var stage = NewText("Stage", window, "All rise.", 44, FontStyles.Bold, Amber);
            stage.alignment = TextAlignmentOptions.Center;
            Stretch(stage.transform, 0.08f, 0.7f, 0.92f, 0.88f);

            var body = NewText("Body", window, "Text.", 30, FontStyles.Normal, Cream);
            body.alignment = TextAlignmentOptions.Top;
            Stretch(body.transform, 0.12f, 0.5f, 0.88f, 0.69f);

            // Evidence to present.
            var evidence = NewUI("Evidence", window);
            Stretch(evidence, 0.12f, 0.15f, 0.88f, 0.48f);
            var grid = evidence.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(640f, 64f);
            grid.spacing = new Vector2(16f, 12f);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            var card = NewUI("CardTemplate", evidence);
            var cardImage = AddImage(card, CardBg);
            var cardButton = card.gameObject.AddComponent<Button>();
            cardButton.targetGraphic = cardImage;
            var cardLabel = NewText("Label", card, "Evidence", 26, FontStyles.Normal, Cream);
            cardLabel.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(cardLabel.transform, 0f, 0f, 1f, 1f);
            ((RectTransform)cardLabel.transform).offsetMin = new Vector2(18, 0);
            var cardView = card.gameObject.AddComponent<DialogueChoiceButton>();
            Wire(cardView, ("label", cardLabel));

            var pass = NewButton("Pass", window, "Let it pass", 26, Line, InkColor);
            Stretch(pass.transform, 0.12f, 0.05f, 0.38f, 0.12f);
            var next = NewButton("Continue", window, "Continue", 26, Red, Cream);
            Stretch(next.transform, 0.62f, 0.05f, 0.88f, 0.12f);

            var panel = root.gameObject.AddComponent<CourtPanel>();
            Wire(panel,
                ("window", window.gameObject),
                ("header", header),
                ("stage", stage),
                ("body", body),
                ("evidenceArea", evidence.gameObject),
                ("cardTemplate", cardView),
                ("passButton", pass),
                ("continueButton", next));

            SetLayer(root, canvas.gameObject.layer);
            card.gameObject.SetActive(false);
            window.gameObject.SetActive(false);
            return root;
        }
    }
}
