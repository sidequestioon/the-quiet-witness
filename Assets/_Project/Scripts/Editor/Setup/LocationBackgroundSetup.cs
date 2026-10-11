using System.IO;
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
    //   - the grey placeholder pictures are hidden (their hotspots keep working); NPCs and exits stay visible.
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

                bool keepVisible = spot.name.StartsWith("Npc_") || spot.name == "Exit";
                if (!keepVisible && spot.TryGetComponent<SpriteRenderer>(out var grey)) grey.enabled = false;
            }

            // Test markers and other grey boxes that are not hotspots: hidden too.
            foreach (var grey in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (grey == renderer || grey.GetComponentInParent<PlayerMover>() != null) continue;
                var owner = grey.gameObject.name;
                if (owner.StartsWith("Npc_") || owner == "Exit") continue;
                grey.enabled = false;
            }

            return true;
        }
    }
}
