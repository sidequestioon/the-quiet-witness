using System.Collections.Generic;
using QuietWitness.Core;
using QuietWitness.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // The Objectives page of the case file as a tree: the main goal with a circle and a small
    // "CURRENT OBJECTIVE" over it, a dashed line down the left, the steps on the line and the
    // mini steps under them; done ones crossed out. Also writes the goals for now:
    //   Find out who sent the envelope, and why.
    //     Be at the Riviera for the last show.
    //       Get downtown to the Riviera.
    //       Find row seven, seat twelve.
    //     Take a closer look at the film from the envelope.
    //       Find something to make the frames bigger.
    //       Enlarge the frames with the enlarger.     <- once he has found the enlarger
    //     Open the locked drawer.                     <- once he has tried the drawer
    //       Find the four numbers. I wrote them down somewhere.
    // Rebuilds only the Objectives page (the rest of the case file stays). Run after Setup > Case file.
    // Menu: Tools > The Quiet Witness > Setup > Case file: objectives look
    public static class ObjectivesLookSetup
    {
        private const string Art = ProjectRoot + "/Art/UI/CaseFile";
        private const string FontAsset = ProjectRoot + "/Art/Fonts/Jost-Medium SDF.asset";
        private const string ObjectivesPath = ProjectRoot + "/Cases/Objectives.asset";
        private const string Opened = "prologue:envelope_opened";

        private static readonly Color Plain = new Color(0.96f, 0.94f, 0.91f, 1f);
        private static readonly Color Yellow = new Color(0.94f, 0.85f, 0.64f, 1f);

        // id, main, mini, text, shown when, done when
        public static readonly (string id, bool main, bool mini, string text, string show, string done)[] Goals =
        {
            ("who_sent",     true,  false, "Find out who sent the envelope, and why.", Opened, ""),
            ("riviera",      false, false, "Be at the Riviera for the last show.", Opened, "riviera:seat"),
            ("riviera_go",   false, true,  "Get downtown to the Riviera.", Opened, "visited:riviera"),
            ("riviera_seat", false, true,  "Find row seven, seat twelve.", Opened, "riviera:seat"),
            ("film",         false, false, "Take a closer look at the film from the envelope.", Opened, "darkroom:film_enlarged"),
            ("film_find",    false, true,  "Find something to make the frames bigger.", Opened, "darkroom:enlarger_found"),
            ("film_enlarge", false, true,  "Enlarge the frames with the enlarger.", "darkroom:enlarger_found", "darkroom:film_enlarged"),
            ("drawer",       false, false, "Open the locked drawer.", "office:drawer_tried", "office:drawer_opened"),
            ("drawer_code",  false, true,  "Find the four numbers. I wrote them down somewhere.", "office:drawer_tried", "darkroom:photo_seen"),
        };

        [MenuItem("Tools/The Quiet Witness/Setup/Case file: objectives look")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Objectives", "Stop Play mode first.", "OK");
                return;
            }
            var objectives = AssetDatabase.LoadAssetAtPath<ObjectiveList>(ObjectivesPath);
            if (objectives == null)
            {
                EditorUtility.DisplayDialog("Objectives", "No Objectives list yet: run Setup > Case file first.", "OK");
                return;
            }
            FillGoals(objectives);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAsset);
            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                var panel = root.GetComponentInChildren<CaseFilePanel>(true);
                var page = panel != null ? panel.transform.Find("Window/ObjectivesPage") : null;
                if (page == null)
                {
                    EditorUtility.DisplayDialog("Objectives", "No case file on Systems: run Setup > Case file first.", "OK");
                    return;
                }
                var view = BuildPage((RectTransform)page, font, objectives);
                if (view == null) return;
                var so = new SerializedObject(panel);
                so.FindProperty("objectivesView").objectReferenceValue = view;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Objectives look done. Styles: Systems > CaseFile > Window > ObjectivesPage > Templates (Main, Step, Mini).");
        }

        public static void FillGoals(ObjectiveList objectives)
        {
            objectives.goals = new List<ObjectiveList.Goal>();
            foreach (var g in Goals)
                objectives.goals.Add(new ObjectiveList.Goal
                {
                    id = g.id, main = g.main, mini = g.mini, text = g.text, showWhen = g.show, doneWhen = g.done,
                });
            EditorUtility.SetDirty(objectives);
        }

        // Empties the page and builds the tree on it. Also used by Setup > Case file.
        public static ObjectivesView BuildPage(RectTransform page, TMP_FontAsset font, ObjectiveList objectives)
        {
            var circle = Load($"{Art}/Goal_Circle.png");
            var dash = Load($"{Art}/Goal_Dash.png");
            var tick = Load($"{Art}/Goal_Tick.png");
            var dot = Load($"{Art}/Goal_Dot.png");
            if (circle == null || dash == null || tick == null || dot == null) return null;

            for (int i = page.childCount - 1; i >= 0; i--) Object.DestroyImmediate(page.GetChild(i).gameObject);

            // Where the lines go: right of the menu, a little above the middle.
            var list = Rect("List", page);
            list.anchorMin = list.anchorMax = new Vector2(0.5f, 0.5f);
            list.pivot = new Vector2(0f, 1f);
            list.anchoredPosition = new Vector2(-360f, 250f);
            list.sizeDelta = new Vector2(1000f, 640f);

            var dashes = Img("Dashes", list, dash, new Color(Yellow.r, Yellow.g, Yellow.b, 0.5f));
            dashes.type = Image.Type.Tiled;
            dashes.pixelsPerUnitMultiplier = PixelScale(dash);
            Corner(dashes.rectTransform, new Vector2(0.5f, 1f), new Vector2(14f, -40f), new Vector2(4f, 100f));

            // The templates, hidden.
            var templates = Rect("Templates", page);
            templates.anchorMin = templates.anchorMax = new Vector2(0.5f, 0.5f);
            templates.pivot = new Vector2(0f, 1f);
            templates.anchoredPosition = list.anchoredPosition;
            templates.sizeDelta = list.sizeDelta;
            templates.gameObject.SetActive(false);

            // Main: circle + small caption, then the goal in yellow under it.
            var main = Row("Main", templates, font, 40f, Yellow, textLeft: 48f, textTop: 44f, spaceAfter: 30f);
            main.marker = Marker(main.transform, "Circle", circle, Yellow, new Vector2(14f, -14f), new Vector2(0.5f, 0.5f));
            var caption = Text("Caption", main.transform, font, 22f, new Color(Plain.r, Plain.g, Plain.b, 0.6f));
            caption.alignment = TextAlignmentOptions.Left;
            caption.characterSpacing = 14f;
            caption.text = "CURRENT OBJECTIVE";
            Corner(caption.rectTransform, new Vector2(0f, 0.5f), new Vector2(48f, -14f), new Vector2(700f, 32f));
            main.doneColor = new Color(Yellow.r, Yellow.g, Yellow.b, 0.35f);
            main.doneMarker = new Color(Yellow.r, Yellow.g, Yellow.b, 0.35f);

            // Step: a short tick off the dashed line.
            var step = Row("Step", templates, font, 30f, new Color(Plain.r, Plain.g, Plain.b, 0.92f), textLeft: 48f, textTop: 0f, spaceAfter: 14f);
            step.marker = Marker(step.transform, "Tick", tick, new Color(Yellow.r, Yellow.g, Yellow.b, 0.5f), new Vector2(12f, -21f), new Vector2(0f, 0.5f));
            step.doneColor = new Color(Plain.r, Plain.g, Plain.b, 0.32f);
            step.doneMarker = new Color(Yellow.r, Yellow.g, Yellow.b, 0.25f);

            // Mini: a dot, further right, smaller.
            var mini = Row("Mini", templates, font, 26f, new Color(Plain.r, Plain.g, Plain.b, 0.72f), textLeft: 84f, textTop: 0f, spaceAfter: 10f);
            mini.marker = Marker(mini.transform, "Dot", dot, new Color(Plain.r, Plain.g, Plain.b, 0.55f), new Vector2(66f, -18f), new Vector2(0.5f, 0.5f));
            mini.doneColor = new Color(Plain.r, Plain.g, Plain.b, 0.28f);
            mini.doneMarker = new Color(Plain.r, Plain.g, Plain.b, 0.2f);

            var view = page.gameObject.GetComponent<ObjectivesView>();
            if (view == null) view = page.gameObject.AddComponent<ObjectivesView>();
            var so = new SerializedObject(view);
            so.FindProperty("objectives").objectReferenceValue = objectives;
            so.FindProperty("list").objectReferenceValue = list;
            so.FindProperty("mainTemplate").objectReferenceValue = main;
            so.FindProperty("stepTemplate").objectReferenceValue = step;
            so.FindProperty("miniTemplate").objectReferenceValue = mini;
            so.FindProperty("dashes").objectReferenceValue = dashes;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // ---- Helpers ----

        private static ObjectiveRow Row(string name, Transform parent, TMP_FontAsset font, float size, Color color,
                                        float textLeft, float textTop, float spaceAfter)
        {
            var r = Rect(name, parent);
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(0f, textTop + size * 1.3f);
            var text = Text("Text", r, font, size, color);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            var tr = text.rectTransform;
            tr.anchorMin = new Vector2(0f, 1f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.offsetMin = new Vector2(textLeft, -textTop - size * 1.3f);
            tr.offsetMax = new Vector2(0f, -textTop);
            text.text = name == "Main" ? "Find out who sent the envelope, and why." : "A step.";
            var row = r.gameObject.AddComponent<ObjectiveRow>();
            row.text = text;
            row.spaceAfter = spaceAfter;
            return row;
        }

        private static Image Marker(Transform parent, string name, Sprite sprite, Color color, Vector2 at, Vector2 pivot)
        {
            var image = Img(name, parent, sprite, color);
            Corner(image.rectTransform, pivot, at, new Vector2(sprite.rect.width, sprite.rect.height) * 4f);
            return image;
        }

        // Anchored to the parent's top-left corner.
        private static void Corner(RectTransform r, Vector2 pivot, Vector2 at, Vector2 size)
        {
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = pivot;
            r.anchoredPosition = at;
            r.sizeDelta = size;
        }

        private static float PixelScale(Sprite s) => 100f / (s.pixelsPerUnit * 4f);   // pixel art at 4x

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image Img(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) { text.font = font; text.fontSharedMaterial = font.material; }
            text.fontSize = size;
            text.fontStyle = FontStyles.Normal;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Sprite Load(string path)
        {
            if (AssetImporter.GetAtPath(path) == null) AssetDatabase.ImportAsset(path);
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
