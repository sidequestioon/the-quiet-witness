using QuietWitness.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // Leaving the office through the map for the first time: the map pulls itself off the cork board,
    // folds up like paper and drops away (drawn frames, Art/UI/Map/Fold), then he travels. Adds the map piece over the board (hidden) and the board without the map.
    // Safe to run again. Menu: Tools > The Quiet Witness > Setup > Map: tear off when leaving the office
    public static class MapTearSetup
    {
        private const string Art = ProjectRoot + "/Art/UI/Map";
        // where the map hangs on the board picture (pixels from its top-left) and its size
        private static readonly Vector2 PieceAt = new Vector2(136f, 38f);
        private static readonly Vector2 PieceSize = new Vector2(208f, 184f);

        [MenuItem("Tools/The Quiet Witness/Setup/Map: tear off when leaving the office")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Map", "Stop Play mode first.", "OK");
                return;
            }
            var empty = LoadSprite(Art + "/Map_Board_Empty.png");
            var piece = LoadSprite(Art + "/Map_Torn.png");
            var front = LoadSprite(Art + "/Map_Front.png");
            if (empty == null || piece == null || front == null) return;

            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                var map = root.GetComponentInChildren<TownMap>(true);
                var so = map != null ? new SerializedObject(map) : null;
                var board = so?.FindProperty("board").objectReferenceValue as RectTransform;
                if (board == null)
                {
                    EditorUtility.DisplayDialog("Map", "No town map on Systems: run Setup > Town map first.", "OK");
                    return;
                }
                foreach (var name in new[] { "TornMap", "FrontItems" })
                {
                    var old = board.Find(name);
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                }

                // the map piece, exactly where it hangs (board children are in board pixels from the centre)
                var go = new GameObject("TornMap", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(board, false);
                var r = (RectTransform)go.transform;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                Vector2 boardSize = board.rect.size;
                Vector2 centre = PieceAt + PieceSize / 2f;
                r.anchoredPosition = new Vector2(centre.x - boardSize.x / 2f, boardSize.y / 2f - centre.y);
                r.sizeDelta = PieceSize;
                var image = go.GetComponent<Image>();
                image.sprite = piece;
                image.raycastTarget = false;
                go.SetActive(false);

                // the photos and clippings pinned over the map's edge, on top of it: they stay behind
                var fgo = new GameObject("FrontItems", typeof(RectTransform), typeof(Image));
                fgo.transform.SetParent(board, false);
                var fr = (RectTransform)fgo.transform;
                fr.anchorMin = fr.anchorMax = fr.pivot = new Vector2(0.5f, 0.5f);
                fr.anchoredPosition = r.anchoredPosition;
                fr.sizeDelta = PieceSize;
                var fimage = fgo.GetComponent<Image>();
                fimage.sprite = front;
                fimage.raycastTarget = false;
                fgo.SetActive(false);

                so.FindProperty("frontItems").objectReferenceValue = fr;
                // the map coming off and folding up: the drawn frames and where each one sits
                string jsonPath = Art + "/Fold/frames.json";
                if (!System.IO.File.Exists(jsonPath))
                {
                    EditorUtility.DisplayDialog("Map", "Not found: " + jsonPath, "OK");
                    return;
                }
                var data = JsonUtility.FromJson<FoldData>(System.IO.File.ReadAllText(jsonPath));
                int n = data != null && data.frames != null ? data.frames.Length : 0;
                var sprites = so.FindProperty("foldFrames");
                var centres = so.FindProperty("foldCentres");
                sprites.arraySize = n;
                centres.arraySize = n;
                int pinned = 0;
                for (int i = 0; i < n; i++)
                {
                    var f = data.frames[i];
                    sprites.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite($"{Art}/Fold/MapFold_{i:00}.png");
                    centres.GetArrayElementAtIndex(i).vector2Value =
                        new Vector2(f.x + f.w / 2f - boardSize.x / 2f, boardSize.y / 2f - (f.y + f.h / 2f));
                    if (f.pinned && pinned == i) pinned = i + 1;
                }
                so.FindProperty("foldPinned").intValue = pinned;
                if (data != null && data.frameTime > 0f) so.FindProperty("foldFrameTime").floatValue = data.frameTime;
                so.FindProperty("boardImage").objectReferenceValue = board.GetComponent<Image>();
                so.FindProperty("emptyBoard").objectReferenceValue = empty;
                so.FindProperty("tornMap").objectReferenceValue = r;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Map tear-off done: leave the office through the map (Downtown) to see it.");
        }

        [System.Serializable]
        private class FoldFrame
        {
            public int x, y, w, h;
            public bool pinned;
        }

        [System.Serializable]
        private class FoldData
        {
            public float frameTime;
            public FoldFrame[] frames;
        }

        private static Sprite LoadSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) == null) AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[Setup] Not found: {path}");
                return null;
            }
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
