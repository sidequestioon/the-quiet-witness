using QuietWitness.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // Things in the office you can look at, Backbone-style: an invisible reach zone in front of each
    // drawn object and, over the closest one, a black round icon with the object's name above it.
    // Each object gets a list of actions (just Look for now); lines and names you change by hand
    // in Hotspot Actions are kept when this runs again.
    // Positions are in pixels of Office.png (x right, y down), so they follow the drawing.
    // Run after Location backgrounds (it needs the Background to be placed). Safe to run again.
    // Menu: Tools > The Quiet Witness > Setup > Office hotspots
    public static class OfficeHotspotsSetup
    {
        private const string PromptFolder = ProjectRoot + "/Art/UI/Prompt";
        private const string FontFile = ProjectRoot + "/Art/Fonts/Jost-Medium.ttf";
        private const string FontAssetPath = ProjectRoot + "/Art/Fonts/Jost-Medium SDF.asset";
        private const string UnlitMaterial = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        private const string ParentName = "Hotspots";
        private const string PromptName = "InteractionPrompt";
        private const string Prefix = "Look_";
        private const int FeetAboveBottom = 10;   // same as Location backgrounds
        private const int ReachPadding = 4;       // px of reach on each side of the object
        private const float ReachHeight = 2.6f;   // world units up from just below the feet

        private readonly struct Spot
        {
            public readonly string Name, Label;
            public readonly int Left, Right;      // object's left/right edge, px
            public readonly int IconX, IconY;     // icon centre, px
            public readonly bool Story;           // yellow instead of white
            public readonly string OldLabel;      // a name set by an earlier version of this table: replaced
            public readonly ActionKind Kind;      // its one action (and icon)
            public readonly bool OpensMap;        // the action opens the town map
            public Spot(string name, string label, int left, int right, int iconX, int iconY,
                        bool story = false, string oldLabel = null, ActionKind kind = ActionKind.Look, bool opensMap = false)
            {
                Name = name; Label = label; Left = left; Right = right; IconX = iconX; IconY = iconY;
                Story = story; OldLabel = oldLabel; Kind = kind; OpensMap = opensMap;
            }
        }

        private static readonly Spot[] Spots =
        {
            new Spot("Bookcase",         "Bookcase",           22,  68,  45, 58),
            new Spot("Aquarium",         "Aquarium",           24,  46,  35, 84),
            new Spot("AnsweringMachine", "Answering machine",  70,  90,  80, 88),
            new Spot("Map",              "Map",                70, 132, 101, 64, story: true, oldLabel: "Cork board",
                     kind: ActionKind.Use, opensMap: true),   // hand icon; opens the map instead of a line
            new Spot("Radio",            "Radio",             106, 130, 118, 83),
            new Spot("PoliceShirt",      "Old uniform",       192, 215, 204, 70),
            new Spot("LockedDrawer",     "Locked drawer",     222, 250, 237, 108),   // 2nd drawer of the metal cabinet
            new Spot("Window",           "Window",            281, 335, 308, 66),
        };

        // What the hero says when he looks at each thing (only filled in when the list is still empty,
        // so lines you write yourself in Hotspot Actions are kept).
        private static readonly System.Collections.Generic.Dictionary<string, string[]> Lines =
            new System.Collections.Generic.Dictionary<string, string[]>
        {
            ["Bookcase"] = new[] { "Law codes, old case files and a cookbook I have never opened." },
            ["Aquarium"] = new[] { "Captain Miller. The only one in this office who never interrupts me. I should feed him. I should feed myself, too." },
            ["AnsweringMachine"] = new[] { "No blinking light. Nobody called. Again." },
            ["Radio"] = new[] { "Rain, jazz and bad news. Usually in that order." },
            ["PoliceShirt"] = new[] { "Eleven years in that uniform. They took the badge, I kept the shirt." },
            ["LockedDrawer"] = new[] { "Locked. I remember locking it. I don't remember where I put the key." },
            ["Window"] = new[] { "Rain on Millbrook. It never washes anything clean." },
        };

        // Lines an earlier version wrote: replaced by the ones above (yours are never touched).
        private static readonly System.Collections.Generic.Dictionary<string, string[]> OldLines =
            new System.Collections.Generic.Dictionary<string, string[]>
        {
            ["Aquarium"] = new[] { "Captain Miller. The only one in this office who never interrupts me.",
                                   "I should feed him. I should feed myself, too." },
            ["PoliceShirt"] = new[] { "Eleven years in that uniform.", "They took the badge. I kept the shirt." },
        };

        // Same order as ActionKind.
        private static readonly string[] PromptIcons = { "Look", "Take", "Use", "Open", "Talk", "Leave", "UseItem" };

        [MenuItem("Tools/The Quiet Witness/Setup/Office hotspots")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Office hotspots", "Stop Play mode first, then run Setup again.", "OK");
                return;
            }

            var icons = new Sprite[PromptIcons.Length];
            var storyIcons = new Sprite[PromptIcons.Length];
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i] = LoadIcon($"Prompt_{PromptIcons[i]}");
                storyIcons[i] = LoadIcon($"Prompt_{PromptIcons[i]}_Story");
                if (icons[i] == null || storyIcons[i] == null) return;
            }
            var font = LoadFont();
            var material = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterial);
            if (material == null) Debug.LogWarning($"[Setup] Not found: {UnlitMaterial}. Icons will follow the room lighting.");

            if (!OpenScene(Scene("Office"))) return;
            var background = GameObject.Find("Background");
            if (background == null || !background.TryGetComponent<SpriteRenderer>(out var bg) || bg.sprite == null)
            {
                EditorUtility.DisplayDialog("Office hotspots", "Run Setup > Location backgrounds first.", "OK");
                return;
            }

            float ppu = bg.sprite.pixelsPerUnit;
            Bounds b = bg.bounds;
            float left = b.min.x, top = b.max.y;
            float feet = b.min.y + FeetAboveBottom / ppu;
            Vector2 Px(float x, float y) => new Vector2(left + x / ppu, top - y / ppu);

            // The one prompt of the scene.
            var promptGo = GameObject.Find(PromptName);
            if (promptGo == null) promptGo = new GameObject(PromptName);
            var prompt = GetOrAdd<InteractionPrompt>(promptGo);
            var speech = GetOrAdd<HeroSpeech>(promptGo);   // the hero's lines over his head
            var speechSo = new SerializedObject(speech);
            speechSo.FindProperty("plateMaterial").objectReferenceValue = material;   // black in any light
            speechSo.ApplyModifiedPropertiesWithoutUndo();
            var pso = new SerializedObject(prompt);
            var iconList = pso.FindProperty("icons");
            iconList.arraySize = icons.Length;
            for (int i = 0; i < icons.Length; i++) iconList.GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
            var storyList = pso.FindProperty("storyIcons");
            storyList.arraySize = storyIcons.Length;
            for (int i = 0; i < storyIcons.Length; i++) storyList.GetArrayElementAtIndex(i).objectReferenceValue = storyIcons[i];
            pso.FindProperty("font").objectReferenceValue = font;
            pso.FindProperty("iconMaterial").objectReferenceValue = material;
            pso.ApplyModifiedPropertiesWithoutUndo();

            var parent = GameObject.Find(ParentName);
            if (parent == null) parent = new GameObject(ParentName);

            // Hotspots that were removed from the list above disappear from the scene too.
            for (int i = parent.transform.childCount - 1; i >= 0; i--)
            {
                var child = parent.transform.GetChild(i);
                if (!child.name.StartsWith(Prefix)) continue;
                bool listed = false;
                foreach (var spot in Spots) listed |= child.name == Prefix + spot.Name;
                if (!listed) Object.DestroyImmediate(child.gameObject);
            }

            foreach (var spot in Spots)
            {
                string name = Prefix + spot.Name;
                var t = parent.transform.Find(name);
                var go = t != null ? t.gameObject : new GameObject(name);
                go.transform.SetParent(parent.transform, false);

                float x0 = Px(spot.Left - ReachPadding, 0).x;
                float x1 = Px(spot.Right + 1 + ReachPadding, 0).x;
                var position = new Vector3((x0 + x1) / 2f, feet - 0.2f + ReachHeight / 2f, 0f);
                go.transform.position = position;

                var box = GetOrAdd<BoxCollider2D>(go);
                box.isTrigger = true;
                box.offset = Vector2.zero;
                box.size = new Vector2(x1 - x0, ReachHeight);

                var interactable = GetOrAdd<Interactable>(go);
                SetField(interactable, "prompt", spot.Label);

                // The earlier dots are replaced by the prompt.
                RemoveComponent<InteractionIcon>(go);

                var actions = GetOrAdd<HotspotActions>(go);
                var so = new SerializedObject(actions);
                // Only the first time: if you moved the icon by hand (Prompt Offset), it stays where you put it.
                var offset = so.FindProperty("promptOffset");
                if (offset.vector2Value == Vector2.zero) offset.vector2Value = Px(spot.IconX, spot.IconY) - (Vector2)position;
                var items = so.FindProperty("actions");
                if (items.arraySize == 0)
                {
                    items.arraySize = 1;
                    items.GetArrayElementAtIndex(0).FindPropertyRelative("kind").enumValueIndex = (int)spot.Kind;
                }
                if (spot.Kind != ActionKind.Look)   // e.g. the map: hand instead of eye
                    items.GetArrayElementAtIndex(0).FindPropertyRelative("kind").enumValueIndex = (int)spot.Kind;
                var first = items.GetArrayElementAtIndex(0).FindPropertyRelative("label");
                if (string.IsNullOrEmpty(first.stringValue) || first.stringValue == spot.OldLabel) first.stringValue = spot.Label;
                if (spot.Story) so.FindProperty("story").boolValue = true;
                var said = items.GetArrayElementAtIndex(0).FindPropertyRelative("lines");
                if (spot.OpensMap && said.arraySize > 0 && said.GetArrayElementAtIndex(0).stringValue.StartsWith("Millbrook. Every street"))
                    said.arraySize = 0;   // lines an earlier version put on the map
                if (OldLines.TryGetValue(spot.Name, out var old) && Same(said, old)) said.arraySize = 0;
                if (said.arraySize == 0 && Lines.TryGetValue(spot.Name, out var text))
                {
                    said.arraySize = text.Length;
                    for (int i = 0; i < text.Length; i++) said.GetArrayElementAtIndex(i).stringValue = text[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                if (spot.OpensMap) WireToMap(go, actions);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SaveOpenScene();
            Debug.Log($"[Setup] Office hotspots done: {Spots.Length} hotspots. Press Play and walk up to things.");
        }

        // The action's "On Chosen" calls MapOpener.Open (the town map) instead of saying a line.
        private static void WireToMap(GameObject go, HotspotActions actions)
        {
            var opener = GetOrAdd<QuietWitness.Map.MapOpener>(go);
            var field = typeof(HotspotActions).GetField("actions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var list = (System.Collections.Generic.List<HotspotAction>)field.GetValue(actions);
            var chosen = list[0].onChosen ??= new UnityEngine.Events.UnityEvent();
            for (int i = chosen.GetPersistentEventCount() - 1; i >= 0; i--) UnityEditor.Events.UnityEventTools.RemovePersistentListener(chosen, i);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(chosen, opener.Open);
            EditorUtility.SetDirty(actions);
        }

        private static bool Same(SerializedProperty list, string[] values)
        {
            if (list.arraySize != values.Length) return false;
            for (int i = 0; i < values.Length; i++)
                if (list.GetArrayElementAtIndex(i).stringValue != values[i]) return false;
            return true;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        // Jost (SIL Open Font License, see OFL.txt next to it) as a TextMeshPro font, made once.
        private static TMP_FontAsset LoadFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null) return existing;
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(FontFile);
            if (ttf == null)
            {
                Debug.LogWarning($"[Setup] Not found: {FontFile}. The names will use the default font.");
                return null;
            }
            var fontAsset = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            fontAsset.name = "Jost-Medium SDF";
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            fontAsset.atlasTextures[0].name = "Jost-Medium Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            fontAsset.material.name = "Jost-Medium Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] Created {FontAssetPath}");
            return fontAsset;
        }

        // Icons are single pictures with the pivot in the middle (they grow from their centre).
        private static Sprite LoadIcon(string file)
        {
            string path = $"{PromptFolder}/{file}.png";
            if (!System.IO.File.Exists(path))
            {
                EditorUtility.DisplayDialog("Office hotspots", $"Not found: {path}", "OK");
                return null;
            }
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
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
