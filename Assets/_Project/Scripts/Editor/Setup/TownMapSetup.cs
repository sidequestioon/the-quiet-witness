using System.Collections.Generic;
using System.Linq;
using QuietWitness.Cutscenes;
using QuietWitness.Map;
using QuietWitness.World;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static QuietWitness.EditorTools.SetupUtil;
using static QuietWitness.EditorTools.UiBuild;

namespace QuietWitness.EditorTools
{
    // The town map on Systems: the cork board from the office (Art/UI/Map/Map_Board.png, 480x270) in the middle
    // of the screen over the blurred, darkened room, with a dot for each district: hover to read about it,
    // click to go there (Old Town = the office, Downtown = the Riviera once the prologue is done).
    // The cork board in the office now always opens it.
    // Rebuilt on every run (district texts and dots you changed are reset - change them in the
    // Districts list below or after the run); the Riviera location and his map lines are made once.
    // Menu: Tools > The Quiet Witness > Setup > Town map
    public static class TownMapSetup
    {
        private const string Art = ProjectRoot + "/Art/UI/Map";
        private const string FontAsset = ProjectRoot + "/Art/Fonts/Jost-Medium SDF.asset";
        private const string BlurProfile = ProjectRoot + "/Art/UI/Inspect/InspectBlur.asset";
        private const string Locations = ProjectRoot + "/Cases/Locations";
        private const string RootName = "TownMap";
        private const int Scale = 3;

        // Districts: name, text, where the dot is (board pixels from the top-left), location file or null.
        private static readonly (string title, string text, int x, int y, string location)[] Districts =
        {
            ("OLD TOWN", "Brick houses older than the city charter. My office is here, above a laundromat that closed in '82.", 191, 93, "Office"),
            ("DOWNTOWN", "Neon, jukeboxes and trouble after midnight. The Riviera is on Main Street.", 221, 117, "Riviera"),
            ("THE MARKET", "Fish in the morning, fistfights at night. Everybody here owes somebody.", 190, 142, null),
            ("WESTGATE", "Lawns, station wagons and neighbors who see everything and say nothing.", 166, 104, null),
            ("MILLBROOK PARK", "Forty acres of trees in the middle of town. After dark, nobody walks through it alone.", 193, 166, null),
            ("COLLEGE HILL", "Students, cheap coffee and big ideas. Half of them will end up working for Uptown.", 168, 178, null),
            ("SOUTHSIDE", "The trains wake you at four. The cops come by less often than the trains.", 219, 191, null),
            ("RIVERSIDE", "Mill workers' houses along the water. Most of the mills are gone. The people stayed.", 268, 175, null),
            ("UPTOWN", "The courthouse, the banks and City Hall. Money wears a tie here.", 275, 113, null),
            ("HILLCREST", "Big houses behind tall hedges. People here don't call the police. They call their lawyers.", 315, 125, null),
            ("THE DOCKS", "Warehouses, cranes and men who don't remember faces.", 306, 87, null),
            ("IRONWORKS", "The old foundry closed in '81. Now it's rust, rail cars and the road out of town.", 304, 187, null),
        };

        [MenuItem("Tools/The Quiet Witness/Setup/Town map")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Town map", "Stop Play mode first.", "OK");
                return;
            }
            var boardSprite = LoadSprite(Art + "/Map_Board.png");
            var dot = LoadSprite(Art + "/Dot.png");
            var dotHover = LoadSprite(Art + "/Dot_Hover.png");
            var dotHere = LoadSprite(Art + "/Dot_Here.png");
            var ring = LoadSprite(Art + "/Ring_Lit.png");
            var plate = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectRoot + "/Art/UI/Inspect/Caption_Plate.png");
            if (new[] { boardSprite, dot, dotHover, dotHere, ring }.Any(s => s == null)) return;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAsset);
            var blur = AssetDatabase.LoadAssetAtPath<VolumeProfile>(BlurProfile);

            HeroLinesSetup.Ensure("map.default", "Millbrook. Every street has a story, and none of them want to tell it.");
            HeroLinesSetup.Ensure("map.riviera", "The Riviera. Row seven, seat twelve.");

            var riviera = CreateAsset<LocationData>($"{Locations}/Riviera.asset", so =>
            {
                so.FindProperty("id").stringValue = "riviera";
                so.FindProperty("displayName").stringValue = "The Riviera";
                so.FindProperty("sceneName").stringValue = "Riviera";
                var flags = so.FindProperty("unlockCondition").FindPropertyRelative("requiredFlags");
                flags.arraySize = 1;
                flags.GetArrayElementAtIndex(0).stringValue = PrologueStorySetup.Ready;
            });

            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                var old = root.transform.Find(RootName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var canvas = UiBuild.Canvas(root.transform, RootName, 58);

                var screen = Rect("Screen", canvas.transform);
                Stretch(screen);
                var group = screen.gameObject.AddComponent<CanvasGroup>();
                var dim = Img("Dim", screen, null, new Color(0f, 0f, 0f, 0f), raycast: true);
                Stretch(dim.rectTransform);

                // The board: its own pixels, scaled up as a whole (children are in board pixels).
                var board = Img("Board", screen, boardSprite, Color.white, raycast: true);
                Place(board.rectTransform, new Vector2(0f, 20f), new Vector2(boardSprite.rect.width, boardSprite.rect.height));
                board.rectTransform.localScale = Vector3.one * Scale;
                Vector2 Px(float x, float y) => new Vector2(x - boardSprite.rect.width / 2f, boardSprite.rect.height / 2f - y);

                foreach (var d in Districts)
                {
                    LocationData location = d.location == null ? null :
                        AssetDatabase.LoadAssetAtPath<LocationData>($"{Locations}/{d.location}.asset");
                    District(board.rectTransform, d.title, d.text, Px(d.x, d.y), location, dot, dotHover, dotHere, ring,
                             d.location == "Riviera" ? PrologueStorySetup.Ready : null,
                             d.location == "Riviera" ? "map.riviera" : null);
                }

                // The photos and clippings around the map (only to read about)
                BoardNotesSetup.Build(board.rectTransform, dot, dotHover, dotHere, ring);

                // Info box (on hover)
                var info = Img("Info", screen, plate, new Color(0.08f, 0.07f, 0.09f, 0.95f));
                if (plate != null) { info.type = Image.Type.Sliced; info.pixelsPerUnitMultiplier = 100f / (plate.pixelsPerUnit * 4f); }
                Place(info.rectTransform, Vector2.zero, new Vector2(520f, 160f));
                var layout = info.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(26, 26, 20, 20);
                layout.spacing = 8f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                var fitter = info.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var infoTitle = Text("Title", info.rectTransform, font, 30f, Yellow, TextAlignmentOptions.TopLeft, "DOWNTOWN");
                infoTitle.characterSpacing = 4f;
                var infoText = Text("Text", info.rectTransform, font, 24f, Plain, TextAlignmentOptions.TopLeft, "...");
                infoText.textWrappingMode = TextWrappingModes.Normal;
                var infoAction = Text("Action", info.rectTransform, font, 22f, Yellow, TextAlignmentOptions.TopLeft, "Click  Go there");
                info.gameObject.SetActive(false);

                // The hero's hint under the board
                var (hintPlate, hint) = Caption(screen, font, plate, new Vector2(0f, -432f));
                hintPlate.sizeDelta = new Vector2(1100f, 60f);
                hint.color = Yellow;
                hint.fontSize = 26f;
                hint.rectTransform.offsetMin = new Vector2(24f, 8f);
                hint.rectTransform.offsetMax = new Vector2(-24f, -8f);

                var keys = Text("Keys", screen, font, 22f, new Color(Plain.r, Plain.g, Plain.b, 0.5f), TextAlignmentOptions.TopRight, "Esc  Close");
                Place(keys.rectTransform, new Vector2(-60f, -24f), new Vector2(400f, 36f), new Vector2(1f, 1f), new Vector2(1f, 1f));

                var map = canvas.AddComponent<TownMap>();
                var so = new SerializedObject(map);
                so.FindProperty("group").objectReferenceValue = group;
                so.FindProperty("dim").objectReferenceValue = dim;
                so.FindProperty("blurProfile").objectReferenceValue = blur;
                so.FindProperty("board").objectReferenceValue = board.rectTransform;
                so.FindProperty("boardScale").intValue = Scale;
                so.FindProperty("info").objectReferenceValue = info.rectTransform;
                so.FindProperty("infoTitle").objectReferenceValue = infoTitle;
                so.FindProperty("infoText").objectReferenceValue = infoText;
                so.FindProperty("infoAction").objectReferenceValue = infoAction;
                so.FindProperty("hint").objectReferenceValue = hint;
                so.FindProperty("openSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectRoot + "/Audio/SFX/Map_Open.wav");
                so.FindProperty("closeSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectRoot + "/Audio/SFX/Map_Close.wav");
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            OpenFromCorkBoard();
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Town map done. Walk to the cork board in the office.");
        }

        internal static void District(RectTransform board, string title, string text, Vector2 at, LocationData location,
                                     Sprite dot, Sprite dotHover, Sprite dotHere, Sprite ring, string lightWhen, string hint)
        {
            var root = Img("District_" + title.Replace(' ', '_'), board, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Place(root.rectTransform, at, new Vector2(15f, 15f));                         // easy to hit
            var ringImage = Pixel("Ring", root.rectTransform, ring, 1f, Vector2.zero);
            ringImage.gameObject.SetActive(false);
            var dotImage = Pixel("Dot", root.rectTransform, dot, 1f, Vector2.zero);

            var district = root.gameObject.AddComponent<MapDistrict>();
            var so = new SerializedObject(district);
            so.FindProperty("title").stringValue = title;
            so.FindProperty("description").stringValue = text;
            so.FindProperty("location").objectReferenceValue = location;
            if (!string.IsNullOrEmpty(lightWhen))
            {
                var light = so.FindProperty("lightWhen");
                light.arraySize = 1;
                light.GetArrayElementAtIndex(0).stringValue = lightWhen;
            }
            so.FindProperty("hint").stringValue = hint ?? string.Empty;
            so.FindProperty("dot").objectReferenceValue = dotImage;
            so.FindProperty("ring").objectReferenceValue = ringImage;
            so.FindProperty("dotSprite").objectReferenceValue = dot;
            so.FindProperty("dotHover").objectReferenceValue = dotHover;
            so.FindProperty("dotHere").objectReferenceValue = dotHere;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // The cork board in the office opens the map any time (you can read about the districts);
        // the Riviera can only be travelled to once the prologue is done.
        private static void OpenFromCorkBoard()
        {
            if (!OpenScene(Scene("Office"))) return;
            var go = GameObject.Find("Look_Map");
            if (go == null || !go.TryGetComponent<HotspotSteps>(out var steps))
            {
                Debug.LogWarning("[Setup] Look_Map has no Hotspot Steps yet: run Setup > Prologue story first.");
                return;
            }
            if (go.GetComponents<HotspotSteps>().Length >= 2) return;   // Look and Use already (Setup > Prologue story)
            var opener = Object.FindFirstObjectByType<CloseUpOpener>();
            if (opener == null) opener = new GameObject("CloseUpOpener").AddComponent<CloseUpOpener>();
            steps.steps.Clear();
            var step = new HotspotSteps.Step { note = "open the map" };
            UnityEventTools.AddPersistentListener(step.then, opener.OpenMap);
            steps.steps.Add(step);
            EditorUtility.SetDirty(steps);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SaveOpenScene();
        }
    }
}
