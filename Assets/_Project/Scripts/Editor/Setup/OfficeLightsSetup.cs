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
    // Real 2D lights for the office, placed on the lamps and the window of Office.png:
    //  - the desk lamp, the table lamp and the floor lamp: warm, on in the evening and at night (LampSwitch);
    //  - the two ceiling lamps: same, but created switched off (tick the object to use them);
    //  - a soft glow from the aquarium, always on;
    //  - the window: a glow around it and a patch of light with the window bars and the blinds
    //    on the sofa and the rug. Its colour changes with the time of day (LightByPreset).
    //  - the desk lamp and the table lamp stay on all day; the room is a bit darker than outside
    //    (Global Light > Brightness 0.8);
    //  - a colour "filter" (Color Grade, OfficeGrade.asset): muted, yellowish colours, dark corners;
    //  - the "DARKROOM IN USE" sign drawn in the picture flickers: a copy of its visible pixels
    //    (On / Dim / Off) lies exactly over it, with a glow (FlickerLight).
    // Lights are only created once: what you move or retune by hand is kept when this runs again.
    // To get the defaults back, delete the "Lights" object and run it again.
    // Run after Location backgrounds. Menu: Tools > The Quiet Witness > Setup > Office lights
    public static class OfficeLightsSetup
    {
        private const string StripesFile = ProjectRoot + "/Art/Locations/Office/Light/WindowStripes.png";
        private const string UnlitMaterial = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        private const string ParentName = "Lights";
        private const string GradeFile = ProjectRoot + "/Art/Locations/Office/Light/OfficeGrade.asset";
        private const string GradeName = "Color Grade";
        private const float RoomBrightness = 0.8f;
        private static readonly string[] AlwaysOn = { "Lamp_Desk", "Lamp_Table" };
        private const string PropsFolder = ProjectRoot + "/Art/Locations/Office/Props";
        private const string SignName = "Sign_DarkroomInUse";
        private const int SignX = 98, SignY = 27;         // its top-left corner, px of Office.png (on the drawn sign)
        private const int OldSignX = 8, OldSignY = 22;    // where an earlier version put it
        private static readonly string[] MoodOrder = { "Morning", "Day", "Evening", "Night" };

        private enum Kind { Lamp, Ceiling, Always, Window }

        private sealed class Spec
        {
            public string Name;
            public int X, Y;                 // px of Office.png (x right, y down)
            public Kind Kind;
            public float Radius;             // px
            public float Angle = 360f;       // less than 360: a cone pointing down
            public Color Color = Color.white;
            public float Intensity = 1f;
            public float Falloff = 0.5f;
            public float Haze;               // the light's glow in the air
            public bool Stripes;             // shaped by WindowStripes.png instead of a circle
            public Color[] MoodColors;       // Morning, Day, Evening, Night
            public float[] MoodIntensities;
        }

        private static readonly Color Warm = new Color(1f, 0.79f, 0.5f);
        private static readonly Color Cool = new Color(0.85f, 0.92f, 1f);

        private static readonly Spec[] Specs =
        {
            new Spec { Name = "Lamp_Desk",  X = 148, Y = 91, Kind = Kind.Lamp, Radius = 75, Angle = 160, Color = Warm, Intensity = 1f, Haze = 0.06f },
            new Spec { Name = "Lamp_Table", X = 97,  Y = 84, Kind = Kind.Lamp, Radius = 80, Color = Warm, Intensity = 0.9f, Haze = 0.08f },
            new Spec { Name = "Lamp_Floor", X = 259, Y = 82, Kind = Kind.Lamp, Radius = 90, Color = Warm, Intensity = 0.9f, Haze = 0.08f },
            new Spec { Name = "Ceiling_Left",  X = 77,  Y = 48, Kind = Kind.Ceiling, Radius = 110, Angle = 170, Color = Cool, Intensity = 0.5f, Haze = 0.04f },
            new Spec { Name = "Ceiling_Right", X = 325, Y = 50, Kind = Kind.Ceiling, Radius = 110, Angle = 170, Color = Cool, Intensity = 0.5f, Haze = 0.04f },
            new Spec { Name = "Aquarium_Glow", X = 35, Y = 101, Kind = Kind.Always, Radius = 26, Color = new Color(0.45f, 0.85f, 1f), Intensity = 0.6f, Falloff = 0.4f },
            new Spec
            {
                Name = "Window_Glow", X = 308, Y = 87, Kind = Kind.Window, Radius = 70, Haze = 0.06f,
                MoodColors = new[] { new Color(1f, 0.9f, 0.72f), new Color(1f, 0.97f, 0.9f), new Color(0.95f, 0.55f, 0.6f), new Color(0.45f, 0.6f, 1f) },
                MoodIntensities = new[] { 0.45f, 0.25f, 0.5f, 0.7f },
            },
            new Spec
            {
                Name = "Window_Stripes", X = 262, Y = 116, Kind = Kind.Window, Stripes = true,
                MoodColors = new[] { new Color(1f, 0.88f, 0.65f), new Color(1f, 0.95f, 0.85f), new Color(0.95f, 0.6f, 0.65f), new Color(0.45f, 0.6f, 1f) },
                MoodIntensities = new[] { 0.6f, 0.35f, 0.6f, 0.9f },
            },
        };

        [MenuItem("Tools/The Quiet Witness/Setup/Office lights")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Office lights", "Stop Play mode first.", "OK");
                return;
            }

            if (!OpenScene(Scene("Office"))) return;
            var background = GameObject.Find("Background");
            if (background == null || !background.TryGetComponent<SpriteRenderer>(out var bg) || bg.sprite == null)
            {
                EditorUtility.DisplayDialog("Office lights", "Run Setup > Location backgrounds first.", "OK");
                return;
            }
            float ppu = bg.sprite.pixelsPerUnit;
            Bounds b = bg.bounds;
            Vector2 Px(float x, float y) => new Vector2(b.min.x + x / ppu, b.max.y - y / ppu);

            var stripes = LoadStripes(ppu);
            var presets = MoodOrder.Select(FindPreset).ToArray();
            int[] allLayers = SortingLayer.layers.Select(l => l.id).ToArray();

            var parent = GameObject.Find(ParentName);
            if (parent == null) parent = new GameObject(ParentName);

            int created = 0;
            foreach (var spec in Specs)
            {
                if (parent.transform.Find(spec.Name) != null) continue;   // keep hand-tuned lights
                created++;

                var go = new GameObject(spec.Name);
                go.transform.SetParent(parent.transform, false);
                go.transform.position = Px(spec.X, spec.Y);
                if (spec.Angle < 360f) go.transform.rotation = Quaternion.Euler(0f, 0f, 180f);   // cone points down

                var light = go.AddComponent<Light2D>();
                light.targetSortingLayers = allLayers;
                light.shadowsEnabled = false;
                if (spec.Stripes)
                {
                    light.lightType = Light2D.LightType.Sprite;
                    light.lightCookieSprite = stripes;
                }
                else
                {
                    light.lightType = Light2D.LightType.Point;
                    light.pointLightInnerRadius = 0f;
                    light.pointLightOuterRadius = spec.Radius / ppu;
                    light.pointLightOuterAngle = spec.Angle;
                    light.pointLightInnerAngle = spec.Angle < 360f ? spec.Angle * 0.5f : 360f;
                    light.falloffIntensity = spec.Falloff;
                }
                light.color = spec.Color;
                light.intensity = spec.Intensity;
                light.volumeIntensity = spec.Haze;   // the haze is on whenever this is above 0 (Unity 6)

                switch (spec.Kind)
                {
                    case Kind.Lamp:
                        go.AddComponent<LampSwitch>();
                        break;
                    case Kind.Ceiling:
                        go.AddComponent<LampSwitch>();
                        go.SetActive(false);
                        break;
                    case Kind.Window:
                        var byPreset = go.AddComponent<LightByPreset>();
                        var so = new SerializedObject(byPreset);
                        var moods = so.FindProperty("moods");
                        moods.arraySize = MoodOrder.Length;
                        for (int i = 0; i < MoodOrder.Length; i++)
                        {
                            var row = moods.GetArrayElementAtIndex(i);
                            row.FindPropertyRelative("preset").objectReferenceValue = presets[i];
                            row.FindPropertyRelative("color").colorValue = spec.MoodColors[i];
                            row.FindPropertyRelative("intensity").floatValue = spec.MoodIntensities[i];
                        }
                        so.ApplyModifiedPropertiesWithoutUndo();
                        // In the editor it shows the evening (the story starts on Friday evening).
                        light.color = spec.MoodColors[2];
                        light.intensity = spec.MoodIntensities[2];
                        break;
                }
            }

            // The desk lamp and the table lamp burn all day: the office is never really bright.
            foreach (var name in AlwaysOn)
            {
                var lamp = parent.transform.Find(name);
                if (lamp == null || !lamp.TryGetComponent<LampSwitch>(out var lampSwitch)) continue;
                var so = new SerializedObject(lampSwitch);
                so.FindProperty("alwaysOn").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // A bit darker than outside, whatever the time of day.
            var global = Object.FindFirstObjectByType<GlobalLightController>();
            if (global != null)
            {
                var so = new SerializedObject(global);
                var brightness = so.FindProperty("brightness");
                if (Mathf.Approximately(brightness.floatValue, 1f)) brightness.floatValue = RoomBrightness;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AddColorGrade();
            AddSign(Px, ppu, allLayers);

            // The grey-box lamp from the first prototype: switched off, not deleted.
            var oldLamp = GameObject.Find("Lamp_Bottle");
            if (oldLamp != null) oldLamp.SetActive(false);

            // The hero's speech plate stays black in any light.
            var material = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterial);
            var speech = Object.FindFirstObjectByType<HeroSpeech>();
            if (speech != null && material != null)
            {
                var so = new SerializedObject(speech);
                so.FindProperty("plateMaterial").objectReferenceValue = material;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (global == null)
                Debug.LogWarning("[Setup] The office has no Global Light 2D with GlobalLightController: the room won't get darker in the evening.");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SaveOpenScene();
            Debug.Log($"[Setup] Office lights done: {created} new light(s). Press Play, then F2 to see the room in every light.");
        }

        // The office "filter": muted, slightly yellow colours and darker corners, like an old photo.
        // Made once as OfficeGrade.asset; what you change in it is kept.
        private static void AddColorGrade()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GradeFile);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, GradeFile);

                var colors = profile.Add<ColorAdjustments>();
                colors.saturation.Override(-25f);                           // muted
                colors.colorFilter.Override(new Color(1f, 0.95f, 0.82f));   // yellowish
                colors.contrast.Override(8f);
                colors.postExposure.Override(-0.1f);                        // a touch darker

                var balance = profile.Add<WhiteBalance>();
                balance.temperature.Override(10f);                          // warmer

                var vignette = profile.Add<Vignette>();
                vignette.intensity.Override(0.28f);                         // dark corners
                vignette.smoothness.Override(0.5f);

                foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Setup] Created {GradeFile}");
            }

            var go = GameObject.Find(GradeName);
            if (go == null) go = new GameObject(GradeName);
            if (!go.TryGetComponent<Volume>(out var volume)) volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            if (volume.sharedProfile == null) volume.sharedProfile = profile;

            // The camera has to draw post-processing, or the filter is not seen.
            var camera = Object.FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                var data = camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                EditorUtility.SetDirty(data);
            }
        }

        private static LightingPreset FindPreset(string name)
        {
            var preset = FindAll<LightingPreset>().FirstOrDefault(p => p.name == name);
            if (preset == null) Debug.LogWarning($"[Setup] Lighting preset '{name}' not found: the window light will be off then.");
            return preset;
        }

        // The sign over the darkroom door: lit sprite + a small warm-red glow, both flicker.
        // Made once: where you move it, and how you tune the flicker, is kept.
        private static void AddSign(System.Func<float, float, Vector2> px, float ppu, int[] layers)
        {
            var existing = GameObject.Find(SignName);
            if (existing != null)
            {
                // Still where the first version put it: move it onto the sign drawn in the picture.
                if (((Vector2)existing.transform.position - px(OldSignX, OldSignY)).sqrMagnitude < 0.0001f)
                    existing.transform.position = px(SignX, SignY);
                return;
            }
            var on = LoadSprite($"{PropsFolder}/Sign_DarkroomInUse_On.png", ppu);
            var dim = LoadSprite($"{PropsFolder}/Sign_DarkroomInUse_Dim.png", ppu);
            var off = LoadSprite($"{PropsFolder}/Sign_DarkroomInUse_Off.png", ppu);
            if (on == null) return;

            var go = new GameObject(SignName);
            go.transform.position = px(SignX, SignY);
            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = on;
            sprite.sortingOrder = -98;                      // over the background and its animations

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(go.transform, false);
            glowGo.transform.localPosition = new Vector3(on.rect.width / 2f / ppu, -on.rect.height / 2f / ppu, 0f);
            var glow = glowGo.AddComponent<Light2D>();
            glow.targetSortingLayers = layers;
            glow.lightType = Light2D.LightType.Point;
            glow.pointLightInnerRadius = 0f;
            glow.pointLightOuterRadius = 40f / ppu;
            glow.falloffIntensity = 0.6f;
            glow.color = new Color(1f, 0.45f, 0.35f);
            glow.intensity = 0.8f;
            glow.volumeIntensity = 0.08f;

            var flicker = go.AddComponent<FlickerLight>();
            var so = new SerializedObject(flicker);
            so.FindProperty("target").objectReferenceValue = sprite;
            so.FindProperty("onSprite").objectReferenceValue = on;
            so.FindProperty("dimSprite").objectReferenceValue = dim;
            so.FindProperty("offSprite").objectReferenceValue = off;
            so.FindProperty("glow").objectReferenceValue = glow;
            so.FindProperty("glowIntensity").floatValue = 0.8f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // The window patch: 1 texel = 1 pixel of the room, its top-left corner on the light's position.
        private static Sprite LoadStripes(float ppu)
        {
            var sprite = LoadSprite(StripesFile, ppu);
            if (sprite == null) Debug.LogWarning($"[Setup] Not found: {StripesFile}. The window light will have no shape.");
            return sprite;
        }

        // A pixel-art sprite: 1 texel = 1 pixel of the room, pivot in its top-left corner.
        private static Sprite LoadSprite(string path, float ppu)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[Setup] Not found: {path}");
                return null;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.TopLeft;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
