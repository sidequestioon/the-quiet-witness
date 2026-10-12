using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuietWitness.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // Puts a drawn background into its scene instead of the grey boxes.
    // For every scene that has Art/Locations/<Scene>/<Scene>.png:
    //   - a "Background" sprite behind everything: its bottom row FeetAboveBottom px below the hero's
    //     feet line (the top of the grey Floor), centered on x = 0, on the pixel grid;
    //   - the camera and the hero stay inside the picture;
    //   - hotspots that ended up outside the picture are pulled inside its edges;
    //   - every grey placeholder picture is hidden, NPCs and exits too (their hotspots keep working);
    //   - looping animations from Art/Locations/<Scene>/Anim/<Name>/ are placed on top of the picture
    //     (frames <Name>_00.png, _01.png ... plus anim.json: { "x": 152, "y": 25, "frameMs": 100 },
    //     x/y = the frame's top-left corner in background pixels).
    // Run again after every new export of the background.
    // Menu: Tools > The Quiet Witness > Setup > Location backgrounds
    public static class LocationBackgroundSetup
    {
        private const string LocationsFolder = ProjectRoot + "/Art/Locations";
        private static readonly string[] Scenes = { "Office", "Diner", "PoliceStation" };
        // Pixels between the picture's bottom row and the hero's feet (the camera shows 8 below the feet).
        private const int FeetAboveBottom = 10;
        // How far before the picture's edge the hero stops, in world units.
        private const float EdgeInset = 0.5f;
        private const int SortingOrder = -100;
        // A 16:9 screen shows 135-144 px above the bottom of the view; lower pictures leave a black band on top.
        private const int MinHeight = 145;
        private const string AnimPrefix = "Anim_";

        [MenuItem("Tools/The Quiet Witness/Setup/Location backgrounds")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Location backgrounds", "Stop Play mode first, then run Setup again.", "OK");
                return;
            }

            int placed = 0;
            foreach (var sceneName in Scenes)
            {
                string path = $"{LocationsFolder}/{sceneName}/{sceneName}.png";
                if (!File.Exists(path)) continue;

                AssetDatabase.ImportAsset(path);
                // One picture = one sprite. Unity's 2D default ("Multiple") keeps old cut-out rects
                // when the picture changes size, and then no sprite is made at all.
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    Debug.LogWarning($"[Setup] {path} is not imported as a sprite.");
                    continue;
                }

                if (!OpenScene(Scene(sceneName))) return;
                if (PlaceBackground(sceneName, sprite))
                {
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    SaveOpenScene();
                    placed++;
                }
            }

            if (placed == 0)
                EditorUtility.DisplayDialog("Location backgrounds",
                    $"No backgrounds found. Expected e.g. {LocationsFolder}/Office/Office.png", "OK");
            else
                Debug.Log($"[Setup] Location backgrounds done: {placed} scene(s). Press Play.");
        }

        private static bool PlaceBackground(string sceneName, Sprite sprite)
        {
            var floor = GameObject.Find("Floor");
            if (floor == null || !floor.TryGetComponent<Renderer>(out var floorRenderer))
            {
                Debug.LogWarning($"[Setup] {sceneName}: no Floor, don't know where the feet line is.");
                return false;
            }

            float ppu = sprite.pixelsPerUnit;
            // A hidden renderer reports empty bounds, so show the floor for a moment to measure it
            // (it is hidden from the first run on).
            floorRenderer.enabled = true;
            int feetPx = Mathf.RoundToInt(floorRenderer.bounds.max.y * ppu);
            floorRenderer.enabled = false;

            var background = GameObject.Find("Background");
            if (background == null) background = new GameObject("Background");
            var renderer = background.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = SortingOrder;
            renderer.enabled = true;

            // Left and bottom edges land exactly on the pixel grid, whatever the pivot.
            int widthPx = Mathf.RoundToInt(sprite.rect.width);
            int leftPx = -widthPx / 2;
            int bottomPx = feetPx - FeetAboveBottom;
            Bounds local = sprite.bounds;
            background.transform.SetParent(null);
            background.transform.SetPositionAndRotation(
                new Vector3(leftPx / ppu - local.min.x, bottomPx / ppu - local.min.y, 0f), Quaternion.identity);
            background.transform.localScale = Vector3.one;

            float left = leftPx / ppu;
            float right = (leftPx + widthPx) / ppu;

            // Wherever the camera shows past the picture (taller screens, odd window sizes) it is black,
            // like a letterbox, instead of the grey-box blue.
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }
            if (sprite.rect.height < MinHeight)
                Debug.LogWarning($"[Setup] {sceneName}: the background is {sprite.rect.height} px tall. " +
                                 $"Draw at least {MinHeight} px (ceiling included) so the camera never shows past its top.");

            var follow = Object.FindFirstObjectByType<CameraFollow>();
            if (follow != null)
            {
                var so = new SerializedObject(follow);
                so.FindProperty("roomLeft").floatValue = left;
                so.FindProperty("roomRight").floatValue = right;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var mover = Object.FindFirstObjectByType<PlayerMover>();
            if (mover != null)
            {
                var so = new SerializedObject(mover);
                so.FindProperty("minX").floatValue = left + EdgeInset;
                so.FindProperty("maxX").floatValue = right - EdgeInset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (var spot in Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            {
                var t = spot.transform;
                var p = t.position;
                float x = Mathf.Clamp(p.x, left + EdgeInset, right - EdgeInset);
                if (!Mathf.Approximately(x, p.x)) t.position = new Vector3(x, p.y, p.z);

                if (spot.TryGetComponent<SpriteRenderer>(out var grey)) grey.enabled = false;
            }

            // Test markers and other grey boxes that are not hotspots: hidden too.
            foreach (var grey in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (grey == renderer || grey.transform.IsChildOf(background.transform) ||
                    grey.GetComponentInParent<PlayerMover>() != null) continue;
                grey.enabled = false;
            }

            int heightPx = Mathf.RoundToInt(sprite.rect.height);
            PlaceAnimations(sceneName, renderer, leftPx, bottomPx + heightPx, ppu);
            return true;
        }

        [System.Serializable]
        private class AnimInfo
        {
            public int x;
            public int y;
            public int frameMs = 100;
        }

        // A ceiling fan, fish in a tank... Each folder under Anim/ becomes one looping sprite
        // in front of the background, at the pixel given in its anim.json.
        // The still picture under it must be erased from the background or fully covered by the frames.
        private static void PlaceAnimations(string sceneName, SpriteRenderer background, int leftPx, int topPx, float ppu)
        {
            var parent = background.transform;
            for (int i = parent.childCount - 1; i >= 0; i--)
                if (parent.GetChild(i).name.StartsWith(AnimPrefix)) Object.DestroyImmediate(parent.GetChild(i).gameObject);

            string root = $"{LocationsFolder}/{sceneName}/Anim";
            if (!Directory.Exists(root)) return;

            int order = SortingOrder + 1;
            foreach (var dir in Directory.GetDirectories(root).OrderBy(d => d, System.StringComparer.Ordinal))
            {
                string folder = dir.Replace('\\', '/');
                string name = Path.GetFileName(folder);
                string jsonPath = folder + "/anim.json";
                if (!File.Exists(jsonPath))
                {
                    Debug.LogWarning($"[Setup] {folder}: no anim.json with the position, skipped.");
                    continue;
                }
                var info = JsonUtility.FromJson<AnimInfo>(File.ReadAllText(jsonPath));

                var frames = new List<Sprite>();
                foreach (var path in Directory.GetFiles(folder, "*.png").Select(f => f.Replace('\\', '/'))
                             .OrderBy(f => f, System.StringComparer.Ordinal))
                {
                    AssetDatabase.ImportAsset(path);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    // Top-left pivot: the frame's corner sits exactly on the pixel from anim.json.
                    // (Unity's 2D default is "Multiple": then the sprite is a cut-out with a centre pivot.)
                    if (settings.spriteMode != (int)SpriteImportMode.Single ||
                        settings.spriteAlignment != (int)SpriteAlignment.TopLeft)
                    {
                        settings.spriteMode = (int)SpriteImportMode.Single;
                        settings.spriteAlignment = (int)SpriteAlignment.TopLeft;
                        settings.spritePivot = new Vector2(0f, 1f);
                        importer.SetTextureSettings(settings);
                        importer.SaveAndReimport();
                    }
                    var frame = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (frame != null) frames.Add(frame);
                }
                if (frames.Count == 0) continue;

                var go = new GameObject(AnimPrefix + name);
                go.transform.SetParent(parent, false);
                // Measured from the sprite itself, so the corner lands right whatever its pivot is.
                Bounds local = frames[0].bounds;
                go.transform.position = new Vector3((leftPx + info.x) / ppu - local.min.x,
                                                    (topPx - info.y) / ppu - local.max.y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = frames[0];
                sr.sortingLayerID = background.sortingLayerID;
                sr.sortingOrder = order++;

                var flipbook = go.AddComponent<SpriteFlipbook>();
                var so = new SerializedObject(flipbook);
                SetObjectList(so, "frames", frames.ToArray());
                so.FindProperty("frameTime").floatValue = info.frameMs / 1000f;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[Setup] {sceneName}: animation {name}, {frames.Count} frames at ({info.x}, {info.y}).");
            }
        }
    }
}
