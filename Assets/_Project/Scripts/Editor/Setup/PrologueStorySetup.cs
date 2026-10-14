using System.Collections.Generic;
using System.Linq;
using QuietWitness.Core;
using QuietWitness.Cutscenes;
using QuietWitness.World;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static QuietWitness.EditorTools.SetupUtil;
using static QuietWitness.EditorTools.UiBuild;

namespace QuietWitness.EditorTools
{
    // The rest of the prologue:
    //  - office: the locked drawer opens a code lock (the code is his old badge number); the badge inside
    //  - darkroom: an old photo on the wall gives the number; the enlarger develops the film frames
    //  - when both are done he says "Riviera..." (Story Beats on Systems) and the map lets him go
    //  - the cork board map says "not yet" until then
    // Adds his lines to Hero Lines, two new pieces of evidence, the goals; builds the two close-ups on Systems.
    // Lines, goals, evidence and hotspot steps are made once and never overwritten; the close-up screens are
    // rebuilt on every run. Menu: Tools > The Quiet Witness > Setup > Prologue story
    public static class PrologueStorySetup
    {
        private const string Ui = ProjectRoot + "/Art/UI";
        private const string FontAsset = ProjectRoot + "/Art/Fonts/Jost-Medium SDF.asset";
        private const string BlurProfile = Ui + "/Inspect/InspectBlur.asset";
        private const string LabelMaterial = Ui + "/Inspect/PromptLabel.mat";
        private const string Evidence = ProjectRoot + "/Cases/Evidence";
        private const string ObjectivesPath = ProjectRoot + "/Cases/Objectives.asset";
        private const string CloseUpsName = "CloseUps";

        public const string DrawerOpened = "office:drawer_opened";
        public const string FilmEnlarged = "darkroom:film_enlarged";
        public const string Ready = "prologue:ready";
        private const string EnvelopeOpened = "prologue:envelope_opened";
        private const string FilmClue = "clue:envelope_filmclip";

        private static readonly (string id, string text)[] Lines =
        {
            ("office.drawer_lock", "Four numbers. I'd have picked something I couldn't forget. Didn't help."),
            ("office.drawer_badge", "My badge. Eleven years, one shield. Officially it isn't mine anymore. In a dark projection booth, nobody checks."),
            ("office.drawer_empty", "Empty now. Just dust and an old habit."),
            ("office.map_not_yet", "Not yet. I'm not walking into that cinema with empty pockets and no idea who I'm looking for."),
            ("darkroom.look_photo", "Me, fresh out of the academy. Badge one, one, seven, four. Some numbers you don't forget."),
            ("darkroom.enlarger_film", "Two frames, a few seconds of somebody's night. Let's see whose."),
            ("darkroom.film_print", "Not a frame from any movie. Somebody pointed a camera at the booth window. Same man, twice, staring right back at the lens."),
            ("darkroom.enlarger_done", "I've seen his face. Now I need a name to go with it."),
            ("prologue.ready", "Riviera. Last show's at eleven thirty. Time to go."),
        };

        [MenuItem("Tools/The Quiet Witness/Setup/Prologue story")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Prologue story", "Stop Play mode first.", "OK");
                return;
            }

            // Pictures
            var drawer = LoadSprite(Ui + "/Lock/Drawer_Front.png");
            var digits = Enumerable.Range(0, 10).Select(i => LoadSprite($"{Ui}/Lock/Digit_{i}.png")).ToArray();
            var arrowUp = LoadSprite(Ui + "/Lock/Arrow_Up.png");
            var arrowDown = LoadSprite(Ui + "/Lock/Arrow_Down.png");
            var selector = LoadSprite(Ui + "/Lock/Selector.png");
            var badge = LoadSprite(Ui + "/Inspect/Item_Badge.png");
            var tray = LoadSprite(Ui + "/Develop/Tray.png");
            var print = LoadSprite(Ui + "/Develop/Print_Frames.png");
            var hand = LoadSprite(Ui + "/Prompt/Prompt_Use_Story.png");
            var leave = LoadSprite(Ui + "/Prompt/Prompt_Leave_Story.png");
            var plate = AssetDatabase.LoadAssetAtPath<Sprite>(Ui + "/Inspect/Caption_Plate.png");
            if (new[] { drawer, arrowUp, arrowDown, selector, badge, tray, print, hand, leave }.Any(s => s == null) ||
                digits.Any(s => s == null)) return;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAsset);
            var labelMaterial = AssetDatabase.LoadAssetAtPath<Material>(LabelMaterial);
            var blur = AssetDatabase.LoadAssetAtPath<VolumeProfile>(BlurProfile);
            var lockOpen = AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectRoot + "/Audio/SFX/Lock_Open.wav");
            var tick = AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectRoot + "/Audio/SFX/Clock_Tick.wav");

            // 1. His lines (never overwritten)
            foreach (var (id, text) in Lines) HeroLinesSetup.Ensure(id, text);

            // 2. Evidence
            var badgeClue = CreateAsset<ClueData>($"{Evidence}/Police_Badge.asset", so =>
            {
                so.FindProperty("id").stringValue = "police_badge";
                so.FindProperty("title").stringValue = "Old Badge";
                so.FindProperty("description").stringValue = LineText("office.drawer_badge");
                so.FindProperty("icon").objectReferenceValue = badge;
            });
            var printClue = CreateAsset<ClueData>($"{Evidence}/Enlarged_Frames.asset", so =>
            {
                so.FindProperty("id").stringValue = "enlarged_frames";
                so.FindProperty("title").stringValue = "Enlarged Frames";
                so.FindProperty("description").stringValue = LineText("darkroom.film_print");
                so.FindProperty("icon").objectReferenceValue = print;
            });

            // 3. Goals
            PatchObjectives();

            // 4. The office
            if (!OpenScene(Scene("Office"))) return;
            var opener = Opener();
            WireSteps("Look_LockedDrawer", new[]
            {
                Step("not opened yet", unless: new[] { DrawerOpened }, then: opener.OpenCodeLock),
                Step("open, empty", line: "office.drawer_empty"),
            });
            WireSteps("Look_Map", new[]
            {
                Step("ready to go", needs: new[] { Ready }, then: opener.OpenMap),
                Step("not yet", line: "office.map_not_yet"),
            });
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SaveOpenScene();

            // 5. The darkroom
            if (!OpenScene(Scene("Darkroom"))) return;
            opener = Opener();
            WireSteps("Look_Enlarger", new[]
            {
                Step("the film from the envelope", needs: new[] { FilmClue }, unless: new[] { FilmEnlarged },
                     line: "darkroom.enlarger_film", then: opener.OpenEnlarger),
                Step("already done", needs: new[] { FilmEnlarged }, line: "darkroom.enlarger_done"),
                Step("before the envelope", line: "darkroom.look_enlarger"),
            });
            AddPhotoHotspot();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SaveOpenScene();

            // 6. Systems: the close-ups and the story beat
            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                var old = root.transform.Find(CloseUpsName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var canvas = UiBuild.Canvas(root.transform, CloseUpsName, 55);
                BuildCodeLock(canvas.transform, font, labelMaterial, plate, blur, drawer, digits, arrowUp, arrowDown, selector,
                              badge, hand, badgeClue, lockOpen, tick);
                BuildDevelop(canvas.transform, font, labelMaterial, plate, blur, tray, print, leave, printClue);
                AddBeat(root);
                foreach (var save in root.GetComponentsInChildren<QuietWitness.Save.SaveSystem>(true))
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
            Debug.Log("[Setup] Prologue story done. New game: envelope > darkroom photo (badge number) > drawer > enlarger > map.");
        }

        private static string LineText(string id) => HeroLines.Find(id)?.text ??
            AssetDatabase.LoadAssetAtPath<HeroLines>(HeroLinesSetup.AssetPath)?.lines.FirstOrDefault(l => l.id == id)?.text ?? "";

        // ---- Goals ----

        private static void PatchObjectives()
        {
            var list = AssetDatabase.LoadAssetAtPath<ObjectiveList>(ObjectivesPath);
            if (list == null) { Debug.LogWarning("[Setup] No Objectives yet: run Setup > Case file first."); return; }
            var goals = list.goals;
            ObjectiveList.Goal Get(string id) => goals.FirstOrDefault(g => g.id == id);

            if (Get("badge") == null)
            {
                int at = Mathf.Max(0, goals.FindIndex(g => g.id == "who_sent") + 1);
                goals.Insert(at, new ObjectiveList.Goal
                {
                    id = "badge", main = false,
                    text = "Get my old badge out of the locked drawer. A projection booth doesn't open for a private eye.",
                    showWhen = EnvelopeOpened, doneWhen = DrawerOpened,
                });
            }
            // The film before the cinema, the cinema only once he's ready, the diner later.
            var film = Get("film");
            if (film != null)
            {
                goals.Remove(film);
                goals.Insert(goals.IndexOf(Get("badge")) + 1, film);
            }
            var riviera = Get("riviera");
            if (riviera != null && riviera.showWhen == EnvelopeOpened) riviera.showWhen = Ready;
            if (riviera != null)
            {
                goals.Remove(riviera);
                goals.Insert(goals.IndexOf(film ?? Get("badge")) + 1, riviera);
            }
            var diner = Get("diner");
            if (diner != null && diner.showWhen == EnvelopeOpened) diner.showWhen = "visited:riviera";
            EditorUtility.SetDirty(list);
        }

        // ---- Hotspots ----

        private static CloseUpOpener Opener()
        {
            var go = GameObject.Find("CloseUpOpener") ?? new GameObject("CloseUpOpener");
            return go.TryGetComponent<CloseUpOpener>(out var o) ? o : go.AddComponent<CloseUpOpener>();
        }

        private struct StepSpec
        {
            public string note, line;
            public string[] needs, unless;
            public UnityAction then;
        }

        private static StepSpec Step(string note, string[] needs = null, string[] unless = null, string line = null, UnityAction then = null) =>
            new StepSpec { note = note, needs = needs, unless = unless, line = line, then = then };

        // The hotspot's action does nothing itself any more: it runs Hotspot Steps (made once).
        private static void WireSteps(string hotspotName, StepSpec[] specs)
        {
            var go = GameObject.Find(hotspotName);
            if (go == null || !go.TryGetComponent<HotspotActions>(out var actions))
            {
                Debug.LogWarning($"[Setup] Hotspot {hotspotName} not found in {EditorSceneManager.GetActiveScene().name}.");
                return;
            }
            bool fresh = !go.TryGetComponent<HotspotSteps>(out var steps);
            if (fresh) steps = go.AddComponent<HotspotSteps>();
            if (fresh || steps.steps.Count == 0)
            {
                steps.steps.Clear();
                foreach (var spec in specs)
                {
                    var step = new HotspotSteps.Step
                    {
                        note = spec.note,
                        line = spec.line ?? string.Empty,
                        needs = spec.needs?.ToList() ?? new List<string>(),
                        unless = spec.unless?.ToList() ?? new List<string>(),
                    };
                    if (spec.then != null) UnityEventTools.AddPersistentListener(step.then, spec.then);
                    steps.steps.Add(step);
                }
                EditorUtility.SetDirty(steps);
            }

            var so = new SerializedObject(actions);
            var first = so.FindProperty("actions").GetArrayElementAtIndex(0);
            first.FindPropertyRelative("line").stringValue = string.Empty;   // the steps say it now
            first.FindPropertyRelative("lines").arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            var field = typeof(HotspotActions).GetField("actions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var list = (List<HotspotAction>)field.GetValue(actions);
            var chosen = list[0].onChosen ??= new UnityEvent();
            for (int i = chosen.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(chosen, i);
            UnityEventTools.AddPersistentListener(chosen, steps.Run);
            EditorUtility.SetDirty(actions);
        }

        // The framed photo on the darkroom wall, between the prints and the chemicals.
        private static void AddPhotoHotspot()
        {
            const string name = "Look_OldPhoto";
            var parent = GameObject.Find("Hotspots") ?? new GameObject("Hotspots");
            if (parent.transform.Find(name) != null) return;
            var background = GameObject.Find("Background");
            if (background == null || !background.TryGetComponent<SpriteRenderer>(out var bg) || bg.sprite == null) return;
            float ppu = bg.sprite.pixelsPerUnit;
            Bounds b = bg.bounds;
            Vector2 Px(float x, float y) => new Vector2(b.min.x + x / ppu, b.max.y - y / ppu);
            float feet = b.min.y + 10f / ppu;
            const float reach = 2.6f;

            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            float x0 = Px(154, 0).x, x1 = Px(178, 0).x;
            var position = new Vector3((x0 + x1) / 2f, feet - 0.2f + reach / 2f, 0f);
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(x1 - x0, reach);
            SetField(go.AddComponent<Interactable>(), "prompt", "Old photo");
            var actions = go.AddComponent<HotspotActions>();
            var so = new SerializedObject(actions);
            so.FindProperty("promptOffset").vector2Value = Px(165, 49) - (Vector2)position;
            var items = so.FindProperty("actions");
            items.arraySize = 1;
            var first = items.GetArrayElementAtIndex(0);
            first.FindPropertyRelative("kind").enumValueIndex = (int)ActionKind.Look;
            first.FindPropertyRelative("label").stringValue = "Old photo";
            first.FindPropertyRelative("line").stringValue = "darkroom.look_photo";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- Systems ----

        private static void AddBeat(GameObject root)
        {
            var beats = root.GetComponentInChildren<StoryBeats>(true);
            if (beats == null)
            {
                var go = new GameObject("StoryBeats");
                go.transform.SetParent(root.transform, false);
                beats = go.AddComponent<StoryBeats>();
            }
            if (beats.beats.Any(b => b.setFlag == Ready)) return;
            beats.beats.Add(new StoryBeats.Beat
            {
                note = "badge and film done: off to the Riviera",
                needs = new List<string> { DrawerOpened, FilmEnlarged },
                line = "prologue.ready",
                setFlag = Ready,
            });
            EditorUtility.SetDirty(beats);
        }

        private static void BuildScreen(Transform parent, string name, VolumeProfile blur, out GameObject go, out CanvasGroup group, out Image dim)
        {
            var rect = Rect(name, parent);
            Stretch(rect);
            go = rect.gameObject;
            group = go.AddComponent<CanvasGroup>();
            dim = Img("Dim", rect, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Stretch(dim.rectTransform);
        }

        private static void Common(SerializedObject so, CanvasGroup group, Image dim, VolumeProfile blur,
                                   (RectTransform root, Image icon, TextMeshProUGUI label) prompt, Sprite icon,
                                   (RectTransform plate, TextMeshProUGUI text) caption)
        {
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("dim").objectReferenceValue = dim;
            so.FindProperty("blurProfile").objectReferenceValue = blur;
            so.FindProperty("prompt").objectReferenceValue = prompt.root;
            so.FindProperty("promptIcon").objectReferenceValue = prompt.icon;
            so.FindProperty("promptLabel").objectReferenceValue = prompt.label;
            so.FindProperty("handIcon").objectReferenceValue = icon;
            so.FindProperty("closeIcon").objectReferenceValue = icon;
            so.FindProperty("caption").objectReferenceValue = caption.plate;
            so.FindProperty("captionText").objectReferenceValue = caption.text;
        }

        private static void BuildCodeLock(Transform canvas, TMP_FontAsset font, Material labelMaterial, Sprite plate, VolumeProfile blur,
                                          Sprite drawer, Sprite[] digits, Sprite up, Sprite down, Sprite selector, Sprite badge,
                                          Sprite hand, ClueData clue, AudioClip open, AudioClip tick)
        {
            BuildScreen(canvas, "CodeLock", blur, out var go, out var group, out var dim);
            const float s = 5f;                                 // the drawer at 5x
            var lockImage = Pixel("Lock", go.transform, drawer, s, new Vector2(0f, 80f));
            var lockRoot = lockImage.rectTransform;
            Vector2 Local(float px, float py) => new Vector2((px - drawer.rect.width / 2f) * s, (drawer.rect.height / 2f - py) * s);

            var wheels = new Image[4];
            var ups = new Button[4];
            var downs = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                float cx = 38f + i * 17f;
                wheels[i] = Pixel($"Wheel_{i + 1}", lockRoot, digits[0], s, Local(cx, 42.5f), raycast: true);
                var wb = wheels[i].gameObject.AddComponent<Button>();
                wb.transition = Selectable.Transition.None;
                ups[i] = Arrow(lockRoot, $"Up_{i + 1}", up, s, Local(cx, 29.5f));
                downs[i] = Arrow(lockRoot, $"Down_{i + 1}", down, s, Local(cx, 55.5f));
            }
            var frame = Pixel("Selector", lockRoot, selector, s, Local(38f, 42.5f));

            var item = Pixel("Badge", go.transform, badge, 6f, new Vector2(0f, 80f));
            item.gameObject.SetActive(false);
            var caption = Caption(go.transform, font, plate, new Vector2(0f, -150f));
            var prompt = UiBuild.Prompt(go.transform, font, labelMaterial, hand, new Vector2(0f, -400f));
            var hint = Text("Hint", go.transform, font, 24f, new Color(Plain.r, Plain.g, Plain.b, 0.45f), TextAlignmentOptions.BottomRight,
                            "A / D  choose      W / S  turn      Esc  leave");
            Place(hint.rectTransform, new Vector2(-110f, 60f), new Vector2(900f, 40f), new Vector2(1f, 0f), new Vector2(1f, 0f));

            var view = go.AddComponent<CodeLockView>();
            var so = new SerializedObject(view);
            Common(so, group, dim, blur, prompt, hand, caption);
            so.FindProperty("lockRoot").objectReferenceValue = lockRoot;
            SetObjectList(so, "wheels", wheels);
            SetObjectList(so, "digitSprites", digits);
            SetObjectList(so, "upButtons", ups);
            SetObjectList(so, "downButtons", downs);
            so.FindProperty("selector").objectReferenceValue = frame.rectTransform;
            so.FindProperty("item").objectReferenceValue = item;
            so.FindProperty("clue").objectReferenceValue = clue;
            so.FindProperty("turnSound").objectReferenceValue = tick;
            so.FindProperty("openSound").objectReferenceValue = open;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button Arrow(Transform parent, string name, Sprite sprite, float scale, Vector2 at)
        {
            var image = Pixel(name, parent, sprite, scale, at, raycast: true);
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.85f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
            return button;
        }

        private static void BuildDevelop(Transform canvas, TMP_FontAsset font, Material labelMaterial, Sprite plate, VolumeProfile blur,
                                         Sprite tray, Sprite print, Sprite leave, ClueData clue)
        {
            BuildScreen(canvas, "Enlarger", blur, out var go, out var group, out var dim);
            var trayImage = Pixel("Tray", go.transform, tray, 5f, new Vector2(0f, 60f));
            var printImage = Pixel("Print", trayImage.rectTransform, print, 5f, Vector2.zero);
            var light = Img("Safelight", go.transform, null, new Color(0.75f, 0.08f, 0.05f, 0.22f));
            Stretch(light.rectTransform);
            var caption = Caption(go.transform, font, plate, new Vector2(0f, -250f));
            var prompt = UiBuild.Prompt(go.transform, font, labelMaterial, leave, new Vector2(0f, -430f));

            var view = go.AddComponent<DevelopView>();
            var so = new SerializedObject(view);
            Common(so, group, dim, blur, prompt, leave, caption);
            so.FindProperty("tray").objectReferenceValue = trayImage.rectTransform;
            so.FindProperty("print").objectReferenceValue = printImage;
            so.FindProperty("clue").objectReferenceValue = clue;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
