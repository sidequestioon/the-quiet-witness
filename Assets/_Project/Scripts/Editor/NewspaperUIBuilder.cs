using QuietWitness.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static QuietWitness.EditorTools.UIBuild;

namespace QuietWitness.EditorTools
{
    // Editor-only helper: builds the newspaper front page and the yes/no box
    // under the selected Canvas and wires them. Run it once in the Systems prefab.
    // Menu: Tools > The Quiet Witness > Build Newspaper UI
    public static class NewspaperUIBuilder
    {
        private const string ToolName = "Build Newspaper UI";

        private static readonly Color Shade = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color Newsprint = new Color32(0xE9, 0xE4, 0xD8, 0xFF);
        private static readonly Color Paper = new Color32(0xEE, 0xE6, 0xD3, 0xFF);
        private static readonly Color Line = new Color32(0xDD, 0xD2, 0xBB, 0xFF);
        private static readonly Color InkColor = new Color32(0x1E, 0x1B, 0x18, 0xFF);
        private static readonly Color Faded = new Color32(0x6A, 0x62, 0x5A, 0xFF);
        private static readonly Color Red = new Color32(0xB3, 0x26, 0x1E, 0xFF);
        private static readonly Color Box = new Color(0.07f, 0.06f, 0.06f, 0.95f);

        [MenuItem("Tools/The Quiet Witness/" + ToolName)]
        private static void Build()
        {
            var canvas = SelectedCanvas(ToolName);
            if (canvas == null) return;
            if (canvas.GetComponentInChildren<NewspaperPanel>(true) != null)
            {
                EditorUtility.DisplayDialog(ToolName, "This Canvas already has a Newspaper.", "OK");
                return;
            }

            var paper = BuildAll(canvas);
            Finish(paper, ToolName);
            Debug.Log("[Newspaper] UI built. Now use ⋮ > Collect all newspapers on Newspaper Panel.");
        }

        // Builds the newspaper (and the yes/no box if missing). Used by the menu and by setup scripts.
        internal static Transform BuildAll(Canvas canvas)
        {
            var paper = BuildNewspaper(canvas);
            if (canvas.GetComponentInChildren<ConfirmPanel>(true) == null) BuildConfirm(canvas);
            return paper;
        }

        private static Transform BuildNewspaper(Canvas canvas)
        {
            var root = NewUI("Newspaper", canvas.transform);
            Stretch(root, 0f, 0f, 1f, 1f);
            PlaceUnderFader(root, canvas);

            var window = NewUI("Window", root);
            Stretch(window, 0f, 0f, 1f, 1f);
            AddImage(window, Shade);

            var sheet = NewUI("Page", window);
            Stretch(sheet, 0.18f, 0.03f, 0.82f, 0.97f);
            AddImage(sheet, Newsprint);

            var masthead = NewText("Masthead", sheet, "THE MILLBROOK COURIER", 64, FontStyles.Bold, InkColor);
            masthead.alignment = TextAlignmentOptions.Center;
            Stretch(masthead.transform, 0.03f, 0.88f, 0.97f, 0.98f);

            var dateLine = NewText("DateLine", sheet, "Evening Edition", 22, FontStyles.Italic | FontStyles.UpperCase, Faded);
            dateLine.alignment = TextAlignmentOptions.Center;
            Stretch(dateLine.transform, 0.03f, 0.845f, 0.97f, 0.88f);

            var rule = NewUI("Rule", sheet);
            Stretch(rule, 0.03f, 0.838f, 0.97f, 0.842f);
            AddImage(rule, InkColor).raycastTarget = false;

            var headline = NewText("Headline", sheet, "HEADLINE", 58, FontStyles.Bold | FontStyles.UpperCase, InkColor);
            headline.alignment = TextAlignmentOptions.Center;
            Stretch(headline.transform, 0.03f, 0.66f, 0.97f, 0.83f);

            var lead = NewText("Lead", sheet, "Lead paragraph.", 28, FontStyles.Normal, InkColor);
            Stretch(lead.transform, 0.05f, 0.5f, 0.95f, 0.65f);

            // Articles in two columns.
            var articles = NewUI("Articles", sheet);
            Stretch(articles, 0.03f, 0.07f, 0.97f, 0.49f);
            var grid = articles.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(560f, 190f);
            grid.spacing = new Vector2(16f, 16f);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            var article = NewUI("ArticleTemplate", articles);
            var articleImage = AddImage(article, new Color(1f, 1f, 1f, 0f)); // invisible, but clickable
            var articleButton = article.gameObject.AddComponent<Button>();
            articleButton.targetGraphic = articleImage;
            var circle = NewUI("Circle", article);
            Stretch(circle, 0f, 0f, 1f, 1f);
            var outline = AddImage(circle, new Color(0f, 0f, 0f, 0f));
            outline.raycastTarget = false;
            var outlineLine = circle.gameObject.AddComponent<Outline>();
            outlineLine.effectColor = Red;
            outlineLine.effectDistance = new Vector2(4f, 4f);
            // Outline needs a visible graphic: give the circle a faint red tint.
            outline.color = new Color(Red.r, Red.g, Red.b, 0.08f);
            var articleTitle = NewText("Title", article, "Article title", 26, FontStyles.Bold, InkColor);
            Stretch(articleTitle.transform, 0.03f, 0.74f, 0.97f, 0.97f);
            var articleBody = NewText("Body", article, "Article text.", 22, FontStyles.Normal, InkColor);
            Stretch(articleBody.transform, 0.03f, 0.03f, 0.97f, 0.74f);
            var articleView = article.gameObject.AddComponent<NewspaperArticleView>();
            Wire(articleView, ("title", articleTitle), ("body", articleBody), ("circle", circle.gameObject));

            var hint = NewText("Hint", sheet, "Click an article to circle it  ·  Esc / E - put down  ·  N - read again",
                20, FontStyles.Italic, Faded);
            hint.alignment = TextAlignmentOptions.Center;
            Stretch(hint.transform, 0.03f, 0.01f, 0.97f, 0.06f);

            var panel = root.gameObject.AddComponent<NewspaperPanel>();
            Wire(panel,
                ("window", window.gameObject),
                ("masthead", masthead),
                ("dateLine", dateLine),
                ("headline", headline),
                ("lead", lead),
                ("articleTemplate", articleView));

            SetLayer(root, canvas.gameObject.layer);
            circle.gameObject.SetActive(false);
            article.gameObject.SetActive(false);
            window.gameObject.SetActive(false);
            return root;
        }

        private static void BuildConfirm(Canvas canvas)
        {
            var root = NewUI("Confirm", canvas.transform);
            Stretch(root, 0f, 0f, 1f, 1f);
            PlaceUnderFader(root, canvas); // above everything else

            var window = NewUI("Window", root);
            Stretch(window, 0f, 0f, 1f, 1f);
            AddImage(window, new Color(0f, 0f, 0f, 0.5f));

            var box = NewUI("Box", window);
            Stretch(box, 0.3f, 0.32f, 0.7f, 0.68f);
            AddImage(box, Box);

            var title = NewText("Title", box, "Title", 36, FontStyles.Bold, Paper);
            title.alignment = TextAlignmentOptions.Center;
            Stretch(title.transform, 0.05f, 0.75f, 0.95f, 0.93f);
            var message = NewText("Message", box, "Message.", 28, FontStyles.Italic, Paper);
            message.alignment = TextAlignmentOptions.Center;
            Stretch(message.transform, 0.06f, 0.3f, 0.94f, 0.73f);

            var no = NewButton("No", box, "Not yet", 26, Line, InkColor);
            Stretch(no.transform, 0.08f, 0.07f, 0.46f, 0.24f);
            var yes = NewButton("Yes", box, "Yes", 26, Red, Paper);
            Stretch(yes.transform, 0.54f, 0.07f, 0.92f, 0.24f);

            var panel = root.gameObject.AddComponent<ConfirmPanel>();
            Wire(panel,
                ("window", window.gameObject),
                ("title", title),
                ("message", message),
                ("yesButton", yes),
                ("yesLabel", yes.GetComponentInChildren<TextMeshProUGUI>()),
                ("noButton", no),
                ("noLabel", no.GetComponentInChildren<TextMeshProUGUI>()));

            SetLayer(root, canvas.gameObject.layer);
            window.gameObject.SetActive(false);
        }
    }
}
