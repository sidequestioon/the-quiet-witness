using System.Linq;
using QuietWitness.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // The rainy town outside the office window, in layers that slide at different speeds (parallax):
    // sky (one per time of day) - far roofs - the houses across the street - wires and a street lamp -
    // rain - drops on the glass. Lit windows, the street lamp and the HOTEL neon show in the evening and
    // at night (Fade By Preset); the neon flickers. The glass in Office.png must stay transparent.
    // Made again on every run from the pictures in Art/Locations/Office/Window/ (nothing to tune by hand
    // except the speeds: Parallax Layer > Factor).
    // Run after Location backgrounds. Menu: Tools > The Quiet Witness > Setup > Office window view
    public static class OfficeWindowSetup
    {
        private const string Folder = ProjectRoot + "/Art/Locations/Office/Window";
        private const string UnlitMaterial = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        private const string RootName = "WindowView";
        private const int LayerX = 223, LayerY = 36;    // top-left of the 170x80 layers, px of Office.png
        private const int GlassX = 284, GlassY = 39;    // top-left of the glass (drops)
        private static readonly string[] Times = { "Morning", "Day", "Evening", "Night" };

        private struct Layer
        {
            public string File; public int Order; public float Factor; public bool Unlit; public float[] Alpha;
            public Layer(string file, int order, float factor, bool unlit, float[] alpha = null)
            { File = file; Order = order; Factor = factor; Unlit = unlit; Alpha = alpha; }
        }

        // Alpha per time of day: Morning, Day, Evening, Night (null = always).
        private static readonly Layer[] Layers =
        {
            new Layer("City_Sky_Morning", -115, 0.175f, true, new[] { 1f, 0f, 0f, 0f }),
            new Layer("City_Sky_Day",     -114, 0.175f, true, new[] { 0f, 1f, 0f, 0f }),
            new Layer("City_Sky_Evening", -113, 0.175f, true, new[] { 0f, 0f, 1f, 0f }),
            new Layer("City_Sky_Night",   -112, 0.175f, true, new[] { 0f, 0f, 0f, 1f }),
            new Layer("City_Far",         -111, 0.125f, false),
            new Layer("City_Far_Lights",  -110, 0.125f, true, new[] { 0.3f, 0f, 1f, 1f }),
            new Layer("City_Mid",         -109, 0.075f, false),
            new Layer("City_Mid_Lights",  -108, 0.075f, true, new[] { 0.2f, 0f, 1f, 1f }),
            new Layer("City_Mid_Neon",    -107, 0.075f, true, new[] { 0f, 0f, 1f, 1f }),
            new Layer("City_Near",        -106, 0.03f, false),
            new Layer("City_Near_Lights", -105, 0.03f, true, new[] { 0f, 0f, 1f, 1f }),
        };

        [MenuItem("Tools/The Quiet Witness/Setup/Office window view")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Office window view", "Stop Play mode first.", "OK");
                return;
            }
            if (!OpenScene(Scene("Office"))) return;
            var background = GameObject.Find("Background");
            if (background == null || !background.TryGetComponent<SpriteRenderer>(out var bg) || bg.sprite == null)
            {
                EditorUtility.DisplayDialog("Office window view", "Run Setup > Location backgrounds first.", "OK");
                return;
            }
            float ppu = bg.sprite.pixelsPerUnit;
            Bounds b = bg.bounds;
            Vector2 Px(float x, float y) => new Vector2(b.min.x + x / ppu, b.max.y - y / ppu);
            float anchor = b.center.x;

            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterial);
            var presets = Times.Select(t => FindAll<LightingPreset>().FirstOrDefault(p => p.name == t)).ToArray();

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;

            foreach (var layer in Layers)
            {
                var sprite = Load($"{Folder}/{layer.File}.png", ppu);
                if (sprite == null) continue;
                var go = Add(root, layer.File, sprite, layer.Order, Px(LayerX, LayerY), layer.Unlit ? unlit : null);
                Parallax(go, layer.Factor, anchor, ppu);
                if (layer.Alpha != null) Fade(go, presets, layer.Alpha);

                if (layer.File == "City_Mid_Neon")   // a faulty neon: flickers now and then
                {
                    var flicker = go.AddComponent<FlickerLight>();
                    var so = new SerializedObject(flicker);
                    so.FindProperty("target").objectReferenceValue = go.GetComponent<SpriteRenderer>();
                    so.FindProperty("onSprite").objectReferenceValue = sprite;
                    so.FindProperty("dimSprite").objectReferenceValue = Load($"{Folder}/City_Mid_Neon_Dim.png", ppu);
                    so.FindProperty("offSprite").objectReferenceValue = Load($"{Folder}/City_Mid_Neon_Off.png", ppu);
                    so.FindProperty("steady").vector2Value = new Vector2(3f, 8f);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Rain: four frames, in front of the town.
            var frames = Enumerable.Range(0, 4).Select(i => Load($"{Folder}/City_Rain_{i:00}.png", ppu)).Where(s => s != null).ToArray();
            if (frames.Length > 0)
            {
                var rain = Add(root, "City_Rain", frames[0], -104, Px(LayerX, LayerY), unlit);
                Parallax(rain, 0.03f, anchor, ppu);
                var flipbook = rain.AddComponent<SpriteFlipbook>();
                var so = new SerializedObject(flipbook);
                SetObjectList(so, "frames", frames);
                so.FindProperty("frameTime").floatValue = 0.08f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            // Drops on the glass: they don't move.
            var drops = Load($"{Folder}/Window_Droplets.png", ppu);
            if (drops != null) Parallax(Add(root, "Window_Droplets", drops, -103, Px(GlassX, GlassY), null), 0f, anchor, ppu);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SaveOpenScene();
            Debug.Log("[Setup] Office window view done. Press Play and walk past the window (F2: times of day).");
        }

        private static GameObject Add(Transform root, string name, Sprite sprite, int order, Vector2 topLeft, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = topLeft;
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;                       // behind the Background (-100): seen only through the glass
            if (material != null) r.sharedMaterial = material;
            return go;
        }

        private static void Parallax(GameObject go, float factor, float anchor, float ppu)
        {
            var p = go.AddComponent<ParallaxLayer>();
            var so = new SerializedObject(p);
            so.FindProperty("factor").floatValue = factor;
            so.FindProperty("anchorX").floatValue = anchor;
            so.FindProperty("pixelsPerUnit").floatValue = ppu;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Fade(GameObject go, LightingPreset[] presets, float[] alpha)
        {
            var f = go.AddComponent<FadeByPreset>();
            var so = new SerializedObject(f);
            var rows = so.FindProperty("rows");
            rows.arraySize = presets.Length;
            for (int i = 0; i < presets.Length; i++)
            {
                var row = rows.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("preset").objectReferenceValue = presets[i];
                row.FindPropertyRelative("alpha").floatValue = alpha[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            // In the editor it shows the evening (the story starts on Friday evening).
            var r = go.GetComponent<SpriteRenderer>();
            r.color = new Color(1f, 1f, 1f, alpha[2]);
            r.enabled = alpha[2] > 0f;
        }

        // 1 texel = 1 pixel of the room, pivot in the top-left corner.
        private static Sprite Load(string path, float ppu)
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
