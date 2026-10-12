using System.IO;
using System.Linq;
using QuietWitness.Map;
using QuietWitness.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // The darkroom behind the office's left door, as its own scene:
    //  - Darkroom.unity, made once as a copy of the office with only the basics kept
    //    (camera, global light, floor, hero, systems, prompt, colour filter); in Build Profiles;
    //  - its picture Art/Locations/Darkroom/Darkroom.png placed by Location backgrounds;
    //  - doors both ways: the office's left door <-> the darkroom's right door, the hero comes in by the door;
    //  - a few things to look at, a dark room (Brightness 0.3) and the red safelights:
    //    the one over the sink glows and flickers a little, the one by the enlarger and the white tube are off.
    // Hotspots and lights are only created once: what you change by hand is kept when this runs again.
    // Menu: Tools > The Quiet Witness > Setup > Darkroom
    public static class DarkroomSetup
    {
        private const string SceneName = "Darkroom";
        private const string Picture = ProjectRoot + "/Art/Locations/Darkroom/Darkroom.png";
        private const string LocationsFolder = ProjectRoot + "/Cases/Locations";
        private static readonly string[] Keep =
            { "Main Camera", "Global Light 2D", "Floor", "Investigator", "Systems", "InteractionPrompt", "Background", "Color Grade" };
        private const int FeetAboveBottom = 10;   // same as Location backgrounds
        private const int ReachPadding = 4;
        private const float ReachHeight = 2.6f;
        private const float RoomBrightness = 0.3f;

        private struct Spot
        {
            public string Name, Label; public int Left, Right, IconX, IconY; public ActionKind Kind; public string[] Lines;
            public Spot(string name, string label, int left, int right, int iconX, int iconY, ActionKind kind, params string[] lines)
            { Name = name; Label = label; Left = left; Right = right; IconX = iconX; IconY = iconY; Kind = kind; Lines = lines; }
        }

        private static readonly Spot[] Looks =
        {
            new Spot("Look_Enlarger", "Enlarger", 21, 56, 38, 82, ActionKind.Look,
                     "My old enlarger. Still the best way to see what a negative is hiding."),
            new Spot("Look_Prints", "Drying prints", 58, 150, 104, 88, ActionKind.Look,
                     "Prints from old cases. Half of them nobody ever paid for."),
            new Spot("Look_Sink", "Sink", 295, 355, 325, 104, ActionKind.Look,
                     "Developer, stop bath, fixer. Smells like every night I couldn't sleep."),
            new Spot("Look_Chemicals", "Chemicals", 152, 216, 184, 98, ActionKind.Look,
                     "Mix them wrong and a photo forgets everything it saw. Mix them right and it remembers too much."),
            new Spot("Look_FilmDryer", "Film dryer", 357, 377, 367, 98, ActionKind.Look,
                     "Negatives drying in the dark. Every frame is a second somebody thought nobody saw."),
        };

        [MenuItem("Tools/The Quiet Witness/Setup/Darkroom")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Darkroom", "Stop Play mode first.", "OK");
                return;
            }
            if (!File.Exists(Picture))
            {
                EditorUtility.DisplayDialog("Darkroom", $"Put the picture here first:\n{Picture}", "OK");
                return;
            }

            // 1. The scene: a copy of the office, cleaned.
            string scenePath = Scene(SceneName);
            bool fresh = !File.Exists(scenePath);
            if (fresh && !AssetDatabase.CopyAsset(Scene("Office"), scenePath))
            {
                Debug.LogError("[Setup] Could not copy the office scene.");
                return;
            }
            var build = EditorBuildSettings.scenes.ToList();
            if (!build.Any(s => s.path == scenePath))
            {
                build.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = build.ToArray();
            }
            if (fresh)
            {
                if (!OpenScene(scenePath)) return;
                foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                    if (!Keep.Contains(root.name)) Object.DestroyImmediate(root);
                SaveOpenScene();
            }

            // 2. Where you can travel to (not on the town map: only through the door).
            var darkroom = CreateAsset<LocationData>($"{LocationsFolder}/Darkroom.asset", so =>
            {
                so.FindProperty("id").stringValue = "darkroom";
                so.FindProperty("displayName").stringValue = "Darkroom";
                so.FindProperty("sceneName").stringValue = SceneName;
            });
            var office = LoadAsset<LocationData>($"{LocationsFolder}/Office.asset");
            if (office == null) return;

            // 3. Pictures in all scenes (the darkroom's included).
            LocationBackgroundSetup.Run();

            // 4. The darkroom.
            if (!OpenScene(scenePath)) return;
            if (!Grid(out var px, out float feet, out float ppu)) return;
            var hotspots = Parent("Hotspots");
            foreach (var spot in Looks) Hotspot(hotspots, spot, px, feet);
            var door = Door(hotspots, new Spot("Door_Office", "Office", 381, 400, 414, 100, ActionKind.Leave), px, feet);
            // Opening other scenes unloads assets nothing points to yet: load them again right before use.
            office = AssetDatabase.LoadAssetAtPath<LocationData>($"{LocationsFolder}/Office.asset");
            WireDoor(door, office, "from_darkroom");
            Spawn("from_office", px(394, 0).x, feet);
            DarkroomLights(px, ppu);
            var global = Object.FindFirstObjectByType<GlobalLightController>();
            if (global != null && fresh)
            {
                var so = new SerializedObject(global);
                so.FindProperty("brightness").floatValue = RoomBrightness;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SaveOpenScene();

            // 5. The office's left door.
            if (!OpenScene(Scene("Office"))) return;
            if (!Grid(out px, out feet, out ppu)) return;
            var officeDoor = Door(Parent("Hotspots"), new Spot("Door_Darkroom", "Darkroom", 22, 41, 12, 98, ActionKind.Leave), px, feet);
            darkroom = AssetDatabase.LoadAssetAtPath<LocationData>($"{LocationsFolder}/Darkroom.asset");
            WireDoor(officeDoor, darkroom, "from_office");
            Spawn("from_darkroom", px(30, 0).x, feet);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SaveOpenScene();

            Debug.Log("[Setup] Darkroom done. Play the office and go through the left door.");
        }

        // Background corner and the feet line, in world units.
        private static bool Grid(out System.Func<float, float, Vector2> px, out float feet, out float ppu)
        {
            px = null; feet = 0f; ppu = 36f;
            var background = GameObject.Find("Background");
            if (background == null || !background.TryGetComponent<SpriteRenderer>(out var bg) || bg.sprite == null)
            {
                Debug.LogError("[Setup] No Background in the scene.");
                return false;
            }
            float p = bg.sprite.pixelsPerUnit;
            Bounds b = bg.bounds;
            px = (x, y) => new Vector2(b.min.x + x / p, b.max.y - y / p);
            feet = b.min.y + FeetAboveBottom / p;
            ppu = p;
            return true;
        }

        private static Transform Parent(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) go = new GameObject(name);
            return go.transform;
        }

        // An invisible reach zone in front of a drawn thing, with its icon and action (made once).
        private static GameObject Hotspot(Transform parent, Spot spot, System.Func<float, float, Vector2> px, float feet)
        {
            var existing = parent.Find(spot.Name);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(spot.Name);
            go.transform.SetParent(parent, false);
            float x0 = px(spot.Left - ReachPadding, 0).x, x1 = px(spot.Right + 1 + ReachPadding, 0).x;
            var position = new Vector3((x0 + x1) / 2f, feet - 0.2f + ReachHeight / 2f, 0f);
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(x1 - x0, ReachHeight);
            var interactable = go.AddComponent<Interactable>();
            SetField(interactable, "prompt", spot.Label);

            var actions = go.AddComponent<HotspotActions>();
            var so = new SerializedObject(actions);
            so.FindProperty("promptOffset").vector2Value = px(spot.IconX, spot.IconY) - (Vector2)position;
            var items = so.FindProperty("actions");
            items.arraySize = 1;
            var first = items.GetArrayElementAtIndex(0);
            first.FindPropertyRelative("kind").enumValueIndex = (int)spot.Kind;
            first.FindPropertyRelative("label").stringValue = spot.Label;
            // His line lives in Hero Lines (made once, then never overwritten).
            if (spot.Lines != null && spot.Lines.Length > 0)
                first.FindPropertyRelative("line").stringValue =
                    HeroLinesSetup.Ensure(HeroLinesSetup.IdFor(SceneName, spot.Name, 0), string.Join(" ", spot.Lines));
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        // Doors are made again on every run (nothing to tune by hand in them): the reach zone sits where
        // the hero stops by the door, the icon on the door itself.
        private static GameObject Door(Transform parent, Spot spot, System.Func<float, float, Vector2> px, float feet)
        {
            var old = parent.Find(spot.Name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            return Hotspot(parent, spot, px, feet);
        }

        // The door's action travels to the other room; the hero appears at the spawn point there.
        private static void WireDoor(GameObject go, LocationData destination, string arriveAt)
        {
            if (destination == null) Debug.LogError($"[Setup] {go.name}: no destination location, the door won't work.");
            var travel = go.GetComponent<LocationTravel>();
            if (travel == null) travel = go.AddComponent<LocationTravel>();
            var so = new SerializedObject(travel);
            so.FindProperty("destination").objectReferenceValue = destination;
            so.FindProperty("arriveAt").stringValue = arriveAt;
            so.ApplyModifiedPropertiesWithoutUndo();

            var actions = go.GetComponent<HotspotActions>();
            var field = typeof(HotspotActions).GetField("actions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var list = (System.Collections.Generic.List<HotspotAction>)field.GetValue(actions);
            if (list.Count == 0) return;
            var chosen = list[0].onChosen ??= new UnityEngine.Events.UnityEvent();
            for (int i = chosen.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(chosen, i);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(chosen, travel.Travel);
            EditorUtility.SetDirty(actions);
        }

        private static void Spawn(string id, float x, float feet)
        {
            string name = "Spawn_" + id;
            var go = GameObject.Find(name);
            if (go != null) return;
            go = new GameObject(name);
            go.transform.position = new Vector3(x, feet, 0f);
            var spawn = go.AddComponent<SpawnPoint>();
            SetField(spawn, "id", id);
        }

        private static void DarkroomLights(System.Func<float, float, Vector2> px, float ppu)
        {
            var parent = Parent("Lights");
            int[] layers = SortingLayer.layers.Select(l => l.id).ToArray();

            // Over the sink: on, breathing a little.
            if (parent.Find("Safelight_Sink") == null)
            {
                var light = RedLight(parent, "Safelight_Sink", px(325, 68), ppu, layers);
                var flicker = light.gameObject.AddComponent<FlickerLight>();
                var so = new SerializedObject(flicker);
                so.FindProperty("glow").objectReferenceValue = light;
                so.FindProperty("glowIntensity").floatValue = 1.2f;
                so.FindProperty("depth").floatValue = 0.35f;
                so.FindProperty("steady").vector2Value = new Vector2(1f, 3f);
                so.FindProperty("sputter").vector2Value = new Vector2(0.3f, 1.2f);
                so.FindProperty("flash").vector2Value = new Vector2(0.08f, 0.25f);
                so.FindProperty("blackoutChance").floatValue = 0f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            // By the enlarger: there, but switched off (tick it to use it).
            if (parent.Find("Safelight_Enlarger") == null)
                RedLight(parent, "Safelight_Enlarger", px(93, 68), ppu, layers).gameObject.SetActive(false);
            // The small tube over the enlarger: a pool of light on the enlarger and the table.
            if (parent.Find("Lamp_Enlarger") == null)
            {
                var lamp = RedLight(parent, "Lamp_Enlarger", px(50, 70), ppu, layers);
                lamp.color = new Color(1f, 0.94f, 0.82f);
                lamp.pointLightOuterRadius = 75f / ppu;
                lamp.intensity = 0.9f;
                lamp.falloffIntensity = 0.55f;
                lamp.volumeIntensity = 0.06f;
                lamp.transform.rotation = Quaternion.Euler(0f, 0f, 180f);   // shines down
                lamp.pointLightOuterAngle = 160f;
                lamp.pointLightInnerAngle = 90f;
            }
            // The little red caged lamp on the wall to the right of the enlarger.
            if (parent.Find("Bulkhead_Red") == null)
            {
                var bulkhead = RedLight(parent, "Bulkhead_Red", px(108, 100), ppu, layers);
                bulkhead.pointLightOuterRadius = 45f / ppu;
                bulkhead.intensity = 0.8f;
                bulkhead.volumeIntensity = 0.06f;
            }
            // The white tube on the ceiling: off while the darkroom is in use (tick it to light the room).
            if (parent.Find("WhiteLight_Tube") == null)
            {
                var tube = RedLight(parent, "WhiteLight_Tube", px(207, 49), ppu, layers);
                tube.color = new Color(0.92f, 0.95f, 1f);
                tube.pointLightOuterRadius = 200f / ppu;
                tube.intensity = 0.9f;
                tube.volumeIntensity = 0.05f;
                tube.transform.rotation = Quaternion.Euler(0f, 0f, 180f);   // shines down
                tube.pointLightOuterAngle = 170f;
                tube.pointLightInnerAngle = 100f;
                tube.gameObject.SetActive(false);
            }
        }

        private static Light2D RedLight(Transform parent, string name, Vector2 at, float ppu, int[] layers)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            var light = go.AddComponent<Light2D>();
            light.targetSortingLayers = layers;
            light.lightType = Light2D.LightType.Point;
            light.pointLightInnerRadius = 0f;
            light.pointLightOuterRadius = 120f / ppu;
            light.falloffIntensity = 0.5f;
            light.color = new Color(1f, 0.12f, 0.08f);
            light.intensity = 1.2f;
            light.volumeIntensityEnabled = true;
            light.volumeIntensity = 0.1f;
            return light;
        }
    }
}
