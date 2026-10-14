using System.Linq;
using QuietWitness.Core;
using QuietWitness.Cutscenes;
using QuietWitness.Save;
using QuietWitness.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // The case file: the icon at the top of the screen, and the screen it opens -
    // Objectives (main goal, steps, mini steps) and Inventory (the things found, turned left and right).
    // Also makes the five things from the envelope into evidence (Cases/Evidence) and links them
    // to the envelope close-up, and makes the Objectives list (Cases/Objectives.asset).
    // Lives on Systems, so it is in every scene. Running it again rebuilds the screen
    // (the Objectives list and the evidence files are kept as you edited them).
    // Run after Envelope inspect. Menu: Tools > The Quiet Witness > Setup > Case file
    public static class CaseFileSetup
    {
        private const string Art = ProjectRoot + "/Art/UI/CaseFile";
        private const string FontAsset = ProjectRoot + "/Art/Fonts/Jost-Medium SDF.asset";
        private const string BlurProfile = ProjectRoot + "/Art/UI/Inspect/InspectBlur.asset";
        private const string ObjectivesPath = ProjectRoot + "/Cases/Objectives.asset";
        private const string EvidenceFolder = ProjectRoot + "/Cases/Evidence";
        private const string RootName = "CaseFile";

        private static readonly Color Plain = new Color(0.96f, 0.94f, 0.91f, 1f);
        private static readonly Color Yellow = new Color(0.94f, 0.85f, 0.64f, 1f);

        // The things from the envelope: object in the close-up, file, id, name in the case file.
        private static readonly (string item, string file, string id, string title)[] Evidence =
        {
            ("Item_Drawing",   "Envelope_Drawing",   "envelope_drawing",   "Pencil Drawing"),
            ("Item_Ticket",    "Envelope_Ticket",    "envelope_ticket",    "Cinema Ticket"),
            ("Item_Money",     "Envelope_Money",     "envelope_money",     "Forty Dollars"),
            ("Item_Matchbook", "Envelope_Matchbook", "envelope_matchbook", "Diner Matchbook"),
            ("Item_FilmClip",  "Envelope_FilmClip",  "envelope_filmclip",  "Film Clip"),
        };

        // First goals: the same as Setup > Case file: objectives look.
        private static (string id, bool main, bool mini, string text, string show, string done)[] Goals => ObjectivesLookSetup.Goals;

        [MenuItem("Tools/The Quiet Witness/Setup/Case file")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Case file", "Stop Play mode first.", "OK");
                return;
            }

            var icon = Load($"{Art}/Icon_CaseFile.png");
            var iconHover = Load($"{Art}/Icon_CaseFile_Hover.png");
            var arrowLeft = Load($"{Art}/Arrow_Left.png");
            var arrowRight = Load($"{Art}/Arrow_Right.png");
            var badge = Load($"{Art}/Badge.png");
            var marker = Load($"{Art}/Marker.png");
            if (new[] { icon, iconHover, arrowLeft, arrowRight, badge, marker }.Any(s => s == null)) return;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAsset);
            var blur = AssetDatabase.LoadAssetAtPath<VolumeProfile>(BlurProfile);
            if (blur == null) Debug.LogWarning("[Setup] No InspectBlur profile: run Setup > Envelope inspect first (the case file will only darken).");

            var objectives = CreateAsset<ObjectiveList>(ObjectivesPath, FillGoals);
            if (!MakeEvidence()) return;

            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                var old = root.transform.Find(RootName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
                Build(root.transform, font, blur, objectives, icon, iconHover, arrowLeft, arrowRight, badge, marker);

                // The old J journal and its "New clue" notices give way to the case file.
                foreach (var journal in root.GetComponentsInChildren<JournalPanel>(true)) journal.enabled = false;
                foreach (var toast in root.GetComponentsInChildren<JournalToast>(true))
                {
                    var tso = new SerializedObject(toast);
                    tso.FindProperty("announceClues").boolValue = false;
                    tso.ApplyModifiedPropertiesWithoutUndo();
                }
                // Saves must know the new evidence.
                foreach (var save in root.GetComponentsInChildren<SaveSystem>(true))
                {
                    var sso = new SerializedObject(save);
                    SetObjectList(sso, "clues", FindAll<ClueData>().Where(c => c != null).ToArray());
                    sso.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Case file done. Open the envelope in the office, then the icon at the top-right (or Tab / J).");
        }

        // ---- Data ----

        private static void FillGoals(SerializedObject so)
        {
            var list = so.FindProperty("goals");
            list.arraySize = Goals.Length;
            for (int i = 0; i < Goals.Length; i++)
            {
                var g = list.GetArrayElementAtIndex(i);
                g.FindPropertyRelative("id").stringValue = Goals[i].id;
                g.FindPropertyRelative("main").boolValue = Goals[i].main;
                g.FindPropertyRelative("mini").boolValue = Goals[i].mini;
                g.FindPropertyRelative("text").stringValue = Goals[i].text;
                g.FindPropertyRelative("showWhen").stringValue = Goals[i].show;
                g.FindPropertyRelative("doneWhen").stringValue = Goals[i].done;
            }
        }

        // One clue file per thing in the envelope; its words are taken from the close-up the first time.
        private static bool MakeEvidence()
        {
            if (!OpenScene(Scene("Office"))) return false;
            var inspect = Object.FindFirstObjectByType<EnvelopeInspect>(FindObjectsInactive.Include);
            if (inspect == null)
            {
                EditorUtility.DisplayDialog("Case file", "No envelope close-up in the office: run Setup > Envelope inspect first.", "OK");
                return false;
            }
            if (!AssetDatabase.IsValidFolder(EvidenceFolder))
                AssetDatabase.CreateFolder(ProjectRoot + "/Cases", "Evidence");

            var items = inspect.GetComponentsInChildren<InspectItem>(true);
            foreach (var e in Evidence)
            {
                var item = items.FirstOrDefault(i => i.name == e.item);
                if (item == null) { Debug.LogWarning($"[Setup] {e.item} not found in the envelope close-up."); continue; }
                var picture = item.GetComponent<Image>();
                string words = item.lines != null ? string.Join(" ", item.lines) : string.Empty;
                var clue = CreateAsset<ClueData>($"{EvidenceFolder}/{e.file}.asset", so =>
                {
                    so.FindProperty("id").stringValue = e.id;
                    so.FindProperty("title").stringValue = e.title;
                    so.FindProperty("description").stringValue = words;
                    so.FindProperty("icon").objectReferenceValue = picture != null ? picture.sprite : null;
                    so.FindProperty("isKey").boolValue = false;
                });
                var iso = new SerializedObject(item);
                iso.FindProperty("clue").objectReferenceValue = clue;
                iso.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SaveOpenScene();
            return true;
        }

        // ---- The screen ----

        private static void Build(Transform parent, TMP_FontAsset font, VolumeProfile blur, ObjectiveList objectives,
                                  Sprite icon, Sprite iconHover, Sprite arrowLeft, Sprite arrowRight, Sprite badge, Sprite marker)
        {
            var go = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;          // over the envelope close-up (50), under fades and dialogue (100)
            canvas.pixelPerfect = true;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            // Screen
            var window = Rect("Window", go.transform);
            Stretch(window);
            var group = window.gameObject.AddComponent<CanvasGroup>();
            var dim = Img("Dim", window, null, Color.black);
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;          // no clicks into the room behind

            var title = Text("Title", window, font, 64f, Plain, TextAlignmentOptions.TopLeft);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, -70f), new Vector2(1000f, 90f));
            title.characterSpacing = 6f;
            title.text = "OBJECTIVES";
            var rule = Img("Rule", window, null, new Color(1f, 1f, 1f, 0.12f));
            rule.rectTransform.anchorMin = new Vector2(0f, 1f);
            rule.rectTransform.anchorMax = new Vector2(1f, 1f);
            rule.rectTransform.pivot = new Vector2(0.5f, 1f);
            rule.rectTransform.offsetMin = new Vector2(110f, -172f);
            rule.rectTransform.offsetMax = new Vector2(-110f, -170f);

            // Menu on the left
            var menu = Rect("Menu", window);
            Place(menu, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 60f), new Vector2(400f, 200f));
            var objectivesTab = Tab("Tab_Objectives", menu, font, "OBJECTIVES", 40f, marker, badge);
            var inventoryTab = Tab("Tab_Inventory", menu, font, "INVENTORY", -40f, marker, badge);

            // Objectives page
            var objectivesPage = Rect("ObjectivesPage", window);
            Stretch(objectivesPage);
            var objectivesView = ObjectivesLookSetup.BuildPage(objectivesPage, font, objectives);

            // Inventory page
            var inventoryPage = Rect("InventoryPage", window);
            Stretch(inventoryPage);
            var row = Rect("Row", inventoryPage);
            Place(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, 100f), Vector2.zero);
            var template = Img("ItemTemplate", row, null, Color.white);
            template.preserveAspect = true;
            template.raycastTarget = true;
            var templateButton = template.gameObject.AddComponent<Button>();
            templateButton.transition = Selectable.Transition.None;
            var counter = Text("Counter", inventoryPage, font, 26f, new Color(Plain.r, Plain.g, Plain.b, 0.45f), TextAlignmentOptions.Center);
            Place(counter.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, 330f), new Vector2(300f, 40f));
            counter.text = "1 / 5";
            var itemName = Text("Name", inventoryPage, font, 40f, Plain, TextAlignmentOptions.Center);
            Place(itemName.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, -140f), new Vector2(560f, 60f));
            itemName.characterSpacing = 3f;
            itemName.text = "Pencil Drawing";
            var prev = Arrow("Prev", inventoryPage, arrowLeft, new Vector2(60f - 330f, -140f));
            var next = Arrow("Next", inventoryPage, arrowRight, new Vector2(60f + 330f, -140f));
            var itemText = Text("Text", inventoryPage, font, 30f, new Color(Plain.r, Plain.g, Plain.b, 0.85f), TextAlignmentOptions.Top);
            Place(itemText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(60f, -190f), new Vector2(900f, 300f));
            itemText.lineSpacing = 10f;
            itemText.text = "A projection booth, drawn in pencil by a steady hand.";
            var empty = Text("Empty", inventoryPage, font, 32f, new Color(Plain.r, Plain.g, Plain.b, 0.5f), TextAlignmentOptions.Center);
            Place(empty.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, 0f), new Vector2(900f, 60f));
            empty.text = "Nothing here yet.";
            empty.gameObject.SetActive(false);
            inventoryPage.gameObject.SetActive(false);

            var hints = Text("Hints", window, font, 24f, new Color(Plain.r, Plain.g, Plain.b, 0.45f), TextAlignmentOptions.BottomRight);
            Place(hints.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-110f, 60f), new Vector2(900f, 40f));
            hints.text = "Esc  Close";

            // The icon at the top-right
            var hudRect = Rect("Hud", go.transform);
            Place(hudRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -36f), new Vector2(icon.rect.width, icon.rect.height) * 5f);
            var hud = hudRect.gameObject.AddComponent<CanvasGroup>();
            var hudIcon = Img("Icon", hudRect, icon, Color.white);
            Stretch(hudIcon.rectTransform);
            hudIcon.raycastTarget = true;
            var hudButton = hudIcon.gameObject.AddComponent<Button>();
            hudButton.transition = Selectable.Transition.None;
            var hudBadge = Img("Badge", hudIcon.rectTransform, badge, Color.white);
            Place(hudBadge.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-6f, -6f), new Vector2(badge.rect.width, badge.rect.height) * 5f);
            hudBadge.gameObject.SetActive(false);

            // Wire it all to the panel.
            var panel = go.AddComponent<CaseFilePanel>();
            var so = new SerializedObject(panel);
            void Set(string field, Object value) => so.FindProperty(field).objectReferenceValue = value;
            Set("hud", hud);
            Set("hudButton", hudButton);
            Set("hudIcon", hudIcon);
            Set("hudSprite", icon);
            Set("hudHover", iconHover);
            Set("hudBadge", hudBadge.gameObject);
            Set("window", group);
            Set("dim", dim);
            Set("blurProfile", blur);
            Set("title", title);
            Set("hints", hints);
            Set("objectivesTab", objectivesTab);
            Set("inventoryTab", inventoryTab);
            Set("objectives", objectives);
            Set("objectivesPage", objectivesPage.gameObject);
            Set("objectivesView", objectivesView);
            Set("inventoryPage", inventoryPage.gameObject);
            Set("itemTemplate", template);
            Set("itemName", itemName);
            Set("itemText", itemText);
            Set("counter", counter);
            Set("prevButton", prev);
            Set("nextButton", next);
            Set("emptyInventory", empty);
            Set("openSound", AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectRoot + "/Audio/SFX/Map_Open.wav"));
            Set("closeSound", AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectRoot + "/Audio/SFX/Map_Close.wav"));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static CaseFileTab Tab(string name, RectTransform parent, TMP_FontAsset font, string text, float y, Sprite marker, Sprite badge)
        {
            var rect = Rect(name, parent);
            Place(rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, y), new Vector2(360f, 64f));
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);         // invisible, catches the mouse
            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var label = Text("Label", rect, font, 36f, Plain, TextAlignmentOptions.Left);
            Stretch(label.rectTransform);
            label.characterSpacing = 4f;
            label.text = text;
            var pointer = Img("Marker", rect, marker, Color.white);
            Place(pointer.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-30f, 0f), new Vector2(marker.rect.width, marker.rect.height) * 4f);
            float width = label.GetPreferredValues(text).x;
            var dot = Img("Badge", rect, badge, Color.white);
            Place(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(width + 22f, 10f), new Vector2(badge.rect.width, badge.rect.height) * 4f);
            dot.gameObject.SetActive(false);

            var tab = rect.gameObject.AddComponent<CaseFileTab>();
            tab.button = button;
            tab.label = label;
            tab.marker = pointer;
            tab.badge = dot.gameObject;
            return tab;
        }

        private static Button Arrow(string name, RectTransform parent, Sprite sprite, Vector2 at)
        {
            var image = Img(name, parent, sprite, new Color(1f, 1f, 1f, 0.8f));
            Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), at, new Vector2(sprite.rect.width, sprite.rect.height) * 4f);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.7f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(1f, 1f, 1f, 0.5f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
            return button;
        }

        // ---- Little helpers ----

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 at, Vector2 size)
        {
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = pivot;
            r.anchoredPosition = at;
            r.sizeDelta = size;
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

        private static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.raycastTarget = false;
            return text;
        }

        private static Sprite Load(string path)
        {
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
