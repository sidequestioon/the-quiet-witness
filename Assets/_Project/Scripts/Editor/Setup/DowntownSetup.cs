using System.IO;
using System.Linq;
using QuietWitness.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // Downtown, the street at night. For now only the Riviera between two dark neighbours, to see how it looks:
    //  - Downtown.unity, made once as a copy of the office with only the basics kept (like the darkroom);
    //  - its picture Art/Locations/Downtown/Downtown.png, placed by Location backgrounds. It is drawn already lit,
    //    so it is shown exactly as drawn (unlit); only the hero takes the street's lights;
    //  - the rain: Art/Locations/Downtown/Anim/Rain (4 frames), in front of everything;
    //  - the camera pulled back: it shows 800x450 pixels of the picture, the hero small against the buildings,
    //    like Backbone. The picture is drawn at that size first, then stretched to fill the screen
    //    (Pixel Perfect Camera: Upscale Render Texture + Stretch Fill), so any size works, not only x2 or x3;
    //  - lights for the hero: under the marquee, from the lobby, the two lamps on the pilasters, the globe, the pink neon;
    //  - a filter of its own, StreetGrade.asset: the office's muted colours plus a glow on the bulbs and the neon.
    // The scene, its lights and the filter are made once: what you move or tune by hand is kept.
    // Menu: Tools > The Quiet Witness > Setup > Downtown: the Riviera
    public static class DowntownSetup
    {
        private const string SceneName = "Downtown";
        private const string Picture = ProjectRoot + "/Art/Locations/Downtown/Downtown.png";
        private const string GradeFile = ProjectRoot + "/Art/Locations/Downtown/StreetGrade.asset";
        private const string UnlitMaterial = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        private static readonly string[] Keep =
            { "Main Camera", "Global Light 2D", "Floor", "Investigator", "Systems", "InteractionPrompt", "Background", "Color Grade", "Sounds" };

        private const int ViewWidth = 800, ViewHeight = 450;   // pixels of the picture the camera shows
        private const int ViewBottomY = 760;                    // the lowest row of the picture the camera shows
        private const int HeroStartX = 560;                     // where the hero stands at first (px of the picture)
        private const float Brightness = 0.7f;                  // the hero, away from the lights, at night outside
        private const int RainOrder = 500;                      // in front of the hero

        private readonly struct Lamp
        {
            public readonly string Name; public readonly int X, Y; public readonly float Radius, Intensity, Haze; public readonly Color Color;
            public Lamp(string name, int x, int y, float radius, Color color, float intensity, float haze)
            { Name = name; X = x; Y = y; Radius = radius; Color = color; Intensity = intensity; Haze = haze; }
        }

        private static readonly Color Bulbs = new Color(1f, 0.8f, 0.55f);
        private static readonly Lamp[] Lamps =
        {
            new Lamp("Marquee_1", 380, 520, 200, Bulbs, 0.8f, 0.03f),
            new Lamp("Marquee_2", 510, 520, 200, Bulbs, 0.8f, 0.03f),
            new Lamp("Marquee_3", 640, 520, 200, Bulbs, 0.9f, 0.04f),
            new Lamp("Marquee_4", 770, 520, 200, Bulbs, 0.8f, 0.03f),
            new Lamp("Marquee_5", 900, 520, 200, Bulbs, 0.8f, 0.03f),
            new Lamp("Lobby", 640, 640, 150, new Color(1f, 0.82f, 0.58f), 0.9f, 0.05f),
            new Lamp("Pilaster_Left", 341, 308, 110, new Color(1f, 0.84f, 0.6f), 0.7f, 0.05f),
            new Lamp("Pilaster_Right", 939, 308, 110, new Color(1f, 0.84f, 0.6f), 0.7f, 0.05f),
            new Lamp("Globe", 708, 284, 160, new Color(1f, 0.95f, 0.85f), 0.6f, 0.06f),
            new Lamp("Neon_Riviera", 640, 290, 240, new Color(1f, 0.4f, 0.55f), 0.35f, 0.03f),
        };

        [MenuItem("Tools/The Quiet Witness/Setup/Downtown: the Riviera")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Downtown", "Stop Play mode first.", "OK");
                return;
            }
            if (!File.Exists(Picture))
            {
                EditorUtility.DisplayDialog("Downtown", $"Put the picture here first:\n{Picture}", "OK");
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

            // 2. The picture and the rain (in every scene that has one).
            LocationBackgroundSetup.Run();

            // 3. The street.
            if (!OpenScene(scenePath)) return;
            var background = GameObject.Find("Background");
            if (background == null || !background.TryGetComponent<SpriteRenderer>(out var bg) || bg.sprite == null)
            {
                Debug.LogError("[Setup] Downtown: no Background after Location backgrounds.");
                return;
            }
            float ppu = bg.sprite.pixelsPerUnit;
            Bounds b = bg.bounds;
            Vector2 Px(float x, float y) => new Vector2(b.min.x + x / ppu, b.max.y - y / ppu);

            // Drawn already lit: shown as drawn. The rain too, and in front of the hero.
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterial);
            if (unlit != null) bg.sharedMaterial = unlit;
            foreach (Transform child in background.transform)
            {
                if (!child.TryGetComponent<SpriteRenderer>(out var sr)) continue;
                if (unlit != null) sr.sharedMaterial = unlit;
                if (child.name == "Anim_Rain") sr.sortingOrder = RainOrder;
            }

            CameraView(Px);
            Hero(Px);
            Lights(Px, ppu);
            Grade();

            var global = Object.FindFirstObjectByType<GlobalLightController>();
            if (global != null && fresh)
            {
                var so = new SerializedObject(global);
                so.FindProperty("brightness").floatValue = Brightness;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SaveOpenScene();
            Debug.Log("[Setup] Downtown done. Open Scenes/Downtown and press Play.");
        }

        // Pulled back: the camera shows ViewWidth x ViewHeight pixels of the picture, its bottom just below the hero's feet.
        private static void CameraView(System.Func<float, float, Vector2> px)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;
            var ppc = cam.GetComponent<PixelPerfectCamera>();
            if (ppc != null)
            {
                var so = new SerializedObject(ppc);
                so.FindProperty("m_RefResolutionX").intValue = ViewWidth;
                so.FindProperty("m_RefResolutionY").intValue = ViewHeight;
                // Without these the camera only zooms by whole steps (x2, x3) and a size in between changes nothing.
                void Choose(string field, int value)
                {
                    var p = so.FindProperty(field);
                    if (p != null) p.intValue = value;
                    else Debug.LogWarning($"[Setup] Pixel Perfect Camera has no {field}: set it in the Inspector.");
                }
                Choose("m_GridSnapping", 2);   // Upscale Render Texture
                Choose("m_CropFrame", 4);      // Stretch Fill
                Choose("m_FilterMode", 0);     // Retro AA: even pixels when stretched
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            cam.orthographicSize = ViewHeight / 2f / PixelArtImporter.PixelsPerUnit;
            var follow = cam.GetComponent<CameraFollow>();
            if (follow != null)
            {
                var so = new SerializedObject(follow);
                so.FindProperty("viewBottom").floatValue = px(0, ViewBottomY).y;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void Hero(System.Func<float, float, Vector2> px)
        {
            var mover = Object.FindFirstObjectByType<PlayerMover>();
            if (mover == null) return;
            var t = mover.transform;
            t.position = new Vector3(px(HeroStartX, 0).x, t.position.y, t.position.z);
        }

        // Real 2D lights, for the hero only (the picture is unlit). Made once each.
        private static void Lights(System.Func<float, float, Vector2> px, float ppu)
        {
            var parentGo = GameObject.Find("Lights");
            if (parentGo == null) parentGo = new GameObject("Lights");
            int[] layers = SortingLayer.layers.Select(l => l.id).ToArray();
            foreach (var lamp in Lamps)
            {
                if (parentGo.transform.Find(lamp.Name) != null) continue;
                var go = new GameObject(lamp.Name);
                go.transform.SetParent(parentGo.transform, false);
                go.transform.position = px(lamp.X, lamp.Y);
                var light = go.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.targetSortingLayers = layers;
                light.shadowsEnabled = false;
                light.pointLightInnerRadius = 0f;
                light.pointLightOuterRadius = lamp.Radius / ppu;
                light.falloffIntensity = 0.6f;
                light.color = lamp.Color;
                light.intensity = lamp.Intensity;
                light.volumeIntensity = lamp.Haze;
            }
        }

        // The street's filter: muted, a little warm, dark corners, and the bulbs and neon glow (Bloom).
        private static void Grade()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GradeFile);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, GradeFile);

                var colors = profile.Add<ColorAdjustments>();
                colors.saturation.Override(-18f);
                colors.colorFilter.Override(new Color(1f, 0.96f, 0.86f));
                colors.contrast.Override(8f);

                var balance = profile.Add<WhiteBalance>();
                balance.temperature.Override(6f);

                var vignette = profile.Add<Vignette>();
                vignette.intensity.Override(0.3f);
                vignette.smoothness.Override(0.5f);

                var bloom = profile.Add<Bloom>();
                bloom.threshold.Override(0.82f);
                bloom.intensity.Override(0.55f);
                bloom.scatter.Override(0.65f);

                foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Setup] Created {GradeFile}");
            }

            var go = GameObject.Find("Color Grade");
            if (go == null) go = new GameObject("Color Grade");
            if (!go.TryGetComponent<Volume>(out var volume)) volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            // A fresh copy of the office still points at the office's filter: the street gets its own.
            if (volume.sharedProfile == null || volume.sharedProfile.name == "OfficeGrade") volume.sharedProfile = profile;

            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                EditorUtility.SetDirty(data);
            }
        }
    }
}
