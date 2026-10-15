using QuietWitness.Map;
using UnityEditor;
using UnityEngine;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // Dots on the photos and clippings pinned around the map on the cork board, like the district dots:
    // click one to read what the hero thinks about it (no going anywhere). Later some of them (the newspaper)
    // will open full-screen to read.
    // The texts are in the Notes list below (or on each Note_ object's Map District after the run).
    // Safe to run again: the Note_ dots are made anew. Setup > Town map also makes them.
    // Menu: Tools > The Quiet Witness > Setup > Map: dots on the board's photos
    public static class BoardNotesSetup
    {
        private const string Art = ProjectRoot + "/Art/UI/Map";
        private const string Prefix = "Note_";

        // What is pinned up: title, the hero's words, where the dot is (board pixels from the top-left).
        public static readonly (string title, string text, int x, int y)[] Notes =
        {
            ("THE RIVIERA", "The Riviera, back when every seat was sold. Now it's the last picture house in town.", 37, 33),
            ("WHO?", "Somebody I couldn't put a name to. Still can't. The question mark stays until I do.", 83, 40),
            ("PLATE?", "A car that kept showing up where it shouldn't. The plate is a blur, no matter how long I stare.", 123, 80),
            ("CLASS OF '74", "Police academy, class of '74. Half of them made sergeant. I made enemies.", 54, 89),
            ("DETECTIVE FIRED", "March '85. I told the truth on the stand, and the department asked for my badge.", 72, 180),
            ("CITY HALL SAYS NO", "The council voted the Riviera down. Old cinemas don't pay taxes like parking lots do.", 147, 164),
            ("CALL MARTY", "Marty knows a guy for everything. The guy usually turns out to be Marty.", 130, 186),
            ("GOLDEN DRAGON", "They deliver until two. Half my dinners this year came in a paper box.", 33, 228),
            ("GREETINGS FROM MIAMI", "Somebody's always leaving Millbrook for the sun. Nobody ever writes twice.", 343, 34),
            ("OCTOBER 1987", "I cross the days off so I know they're over. Friday the ninth is circled. Rent's due.", 430, 20),
            ("LAKE '79", "Fishing on the lake, summer of '79. We didn't catch a thing. Best weekend I ever had.", 418, 135),
            ("ME AND SAL", "Me and Sal, back when we shared a patrol car and bad coffee. He still calls on my birthday.", 345, 207),
        };

        [MenuItem("Tools/The Quiet Witness/Setup/Map: dots on the board's photos")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Map", "Stop Play mode first.", "OK");
                return;
            }
            var dot = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Dot.png");
            var dotHover = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Dot_Hover.png");
            var dotHere = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Dot_Here.png");
            var ring = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Ring_Lit.png");
            if (dot == null)
            {
                EditorUtility.DisplayDialog("Map", "Not found: " + Art + "/Dot.png", "OK");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                var map = root.GetComponentInChildren<TownMap>(true);
                var board = map != null ? new SerializedObject(map).FindProperty("board").objectReferenceValue as RectTransform : null;
                if (board == null)
                {
                    EditorUtility.DisplayDialog("Map", "No town map on Systems: run Setup > Town map first.", "OK");
                    return;
                }
                Build(board, dot, dotHover, dotHere, ring);
                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] {Notes.Length} dots on the board's photos. Open the cork board in the office and click one.");
        }

        // Removes the old Note_ dots on the board and makes them again from the Notes list.
        public static void Build(RectTransform board, Sprite dot, Sprite dotHover, Sprite dotHere, Sprite ring)
        {
            for (int i = board.childCount - 1; i >= 0; i--)
                if (board.GetChild(i).name.StartsWith(Prefix)) Object.DestroyImmediate(board.GetChild(i).gameObject);

            Vector2 size = board.rect.size;
            foreach (var n in Notes)
            {
                var at = new Vector2(n.x - size.x / 2f, size.y / 2f - n.y);
                TownMapSetup.District(board, n.title, n.text, at, null, dot, dotHover, dotHere, ring, null, null);
                // District_ -> Note_ (so the town map's districts and these stay apart)
                var made = board.GetChild(board.childCount - 1);
                made.name = Prefix + n.title.Replace(' ', '_').Replace("'", "").Replace("?", "");
            }
            // under the torn-off map and the things over it (Setup > Map: tear off), like the districts
            var torn = board.Find("TornMap");
            if (torn != null)
            {
                var notes = new System.Collections.Generic.List<Transform>();
                foreach (Transform c in board)
                    if (c.name.StartsWith(Prefix)) notes.Add(c);
                foreach (var c in notes)
                    if (c.GetSiblingIndex() > torn.GetSiblingIndex()) c.SetSiblingIndex(torn.GetSiblingIndex());
            }
        }
    }
}
