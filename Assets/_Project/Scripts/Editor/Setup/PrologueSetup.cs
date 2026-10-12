using System.Linq;
using QuietWitness.Cutscenes;
using QuietWitness.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // The opening of the game in the office: the hero lying on the sofa and smoking (47 frames),
    // the knock, the envelope sliding in under the right door (10 frames) and the envelope on the
    // floor you can take (yellow hand, story). The order of things is in Prologue Intro.
    // Made again on every run (timings and lines you changed in Prologue Intro are lost then).
    // Run after Location backgrounds. Menu: Tools > The Quiet Witness > Setup > Prologue
    public static class PrologueSetup
    {
        private const string LyingFolder = ProjectRoot + "/Art/Characters/Hero/Lying";
        private const string EnvelopeFolder = ProjectRoot + "/Art/Locations/Office/Prologue";
        private const string Knock = ProjectRoot + "/Audio/SFX/Door_Knock.wav";
        private const string RootName = "Prologue";
        private const string HotspotName = "Take_Envelope";
        private const int LyingX = 268, LyingY = 99;        // top-left of the lying hero, px of Office.png
        private const int EnvelopeX = 391, EnvelopeY = 150; // top-left of the envelope frames
        private const int GetUpX = 300;                      // where he stands after getting up
        private const int FeetAboveBottom = 10;
        private const float ReachHeight = 2.6f;

        [MenuItem("Tools/The Quiet Witness/Setup/Prologue")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Prologue", "Stop Play mode first.", "OK");
                return;
            }
            if (!OpenScene(Scene("Office"))) return;
            var background = GameObject.Find("Background");
            if (background == null || !background.TryGetComponent<SpriteRenderer>(out var bg) || bg.sprite == null)
            {
                EditorUtility.DisplayDialog("Prologue", "Run Setup > Location backgrounds first.", "OK");
                return;
            }
            float ppu = bg.sprite.pixelsPerUnit;
            Bounds b = bg.bounds;
            Vector2 Px(float x, float y) => new Vector2(b.min.x + x / ppu, b.max.y - y / ppu);
            float feet = b.min.y + FeetAboveBottom / ppu;

            var lying = Frames(LyingFolder, "Hero_Lying_");
            var slide = Frames(EnvelopeFolder, "Envelope_Slide_");
            if (lying.Length == 0 || slide.Length == 0) return;

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName);

            var lyingHero = Flipbook(root, "Hero_Lying", lying, 0.15f, true, true, Px(LyingX, LyingY));
            var envelope = Flipbook(root, "Envelope", slide, 0.12f, false, false, Px(EnvelopeX, EnvelopeY));
            var spot = new GameObject("GetUpSpot");
            spot.transform.SetParent(root.transform, false);
            spot.transform.position = new Vector3(Px(GetUpX, 0).x, feet, 0f);

            // The envelope on the floor: Take (yellow, story).
            var hotspots = GameObject.Find("Hotspots");
            if (hotspots == null) hotspots = new GameObject("Hotspots");
            var oldSpot = hotspots.transform.Find(HotspotName);
            if (oldSpot != null) Object.DestroyImmediate(oldSpot.gameObject);
            var hotspot = new GameObject(HotspotName);
            hotspot.transform.SetParent(hotspots.transform, false);
            float x0 = Px(380, 0).x, x1 = Px(412, 0).x;
            var position = new Vector3((x0 + x1) / 2f, feet - 0.2f + ReachHeight / 2f, 0f);
            hotspot.transform.position = position;
            var box = hotspot.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(x1 - x0, ReachHeight);
            SetField(hotspot.AddComponent<Interactable>(), "prompt", "Envelope");
            var actions = hotspot.AddComponent<HotspotActions>();
            var so = new SerializedObject(actions);
            so.FindProperty("promptOffset").vector2Value = Px(403, 146) - (Vector2)position;
            so.FindProperty("story").boolValue = true;
            var items = so.FindProperty("actions");
            items.arraySize = 1;
            items.GetArrayElementAtIndex(0).FindPropertyRelative("kind").enumValueIndex = (int)ActionKind.Take;
            items.GetArrayElementAtIndex(0).FindPropertyRelative("label").stringValue = "Envelope";
            var lines = items.GetArrayElementAtIndex(0).FindPropertyRelative("lines");
            lines.arraySize = 1;
            lines.GetArrayElementAtIndex(0).stringValue =
                "No stamp, no name. Somebody climbed three floors in the rain to slip this under my door.";
            so.ApplyModifiedPropertiesWithoutUndo();

            var intro = root.AddComponent<PrologueIntro>();
            var iso = new SerializedObject(intro);
            iso.FindProperty("lyingHero").objectReferenceValue = lyingHero;
            iso.FindProperty("getUpSpot").objectReferenceValue = spot.transform;
            iso.FindProperty("envelopeSlide").objectReferenceValue = envelope.GetComponent<SpriteFlipbook>();
            iso.FindProperty("envelopeHotspot").objectReferenceValue = hotspot;
            iso.FindProperty("knock").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Knock);
            iso.ApplyModifiedPropertiesWithoutUndo();

            // Take -> PrologueIntro.PickUpEnvelope
            var field = typeof(HotspotActions).GetField("actions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var list = (System.Collections.Generic.List<HotspotAction>)field.GetValue(actions);
            var chosen = list[0].onChosen ??= new UnityEngine.Events.UnityEvent();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(chosen, intro.PickUpEnvelope);
            EditorUtility.SetDirty(actions);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SaveOpenScene();
            Debug.Log("[Setup] Prologue done. Start a new game (no save) in the office: he lies on the sofa, then the knock.");
        }

        private static GameObject Flipbook(GameObject root, string name, Sprite[] frames, float frameTime, bool loop, bool playOnAwake, Vector2 topLeft)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.position = topLeft;
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = frames[0];
            r.sortingOrder = -98;           // over the background, under the walking hero (10)
            var flipbook = go.AddComponent<SpriteFlipbook>();
            var so = new SerializedObject(flipbook);
            SetObjectList(so, "frames", frames);
            so.FindProperty("frameTime").floatValue = frameTime;
            so.FindProperty("loop").boolValue = loop;
            so.FindProperty("playOnAwake").boolValue = playOnAwake;
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        // All PNGs in a folder that start with the prefix, in name order: Single sprites, pivot top-left.
        private static Sprite[] Frames(string folder, string prefix)
        {
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(p => System.IO.Path.GetFileName(p).StartsWith(prefix))
                .OrderBy(p => p, System.StringComparer.Ordinal).ToArray();
            if (paths.Length == 0) Debug.LogError($"[Setup] No {prefix}*.png in {folder}");
            foreach (var path in paths)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteMode != (int)SpriteImportMode.Single || settings.spriteAlignment != (int)SpriteAlignment.TopLeft)
                {
                    settings.spriteMode = (int)SpriteImportMode.Single;
                    settings.spriteAlignment = (int)SpriteAlignment.TopLeft;
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                }
            }
            return paths.Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path)).Where(s => s != null).ToArray();
        }
    }
}
