using QuietWitness.Map;
using QuietWitness.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static QuietWitness.EditorTools.SetupUtil;
using static QuietWitness.EditorTools.UiBuild;

namespace QuietWitness.EditorTools
{
    // The newspaper on the cork board (the City Hall clipping) opens to read: two pages of The Gazette over the
    // darkened screen, arrows to turn the pages. Adds the paper reader to Systems and gives the clipping's dot
    // its pages (Art/UI/Newspaper). Safe to run again.
    // Menu: Tools > The Quiet Witness > Setup > Map: the Gazette to read
    public static class GazetteSetup
    {
        private const string Art = ProjectRoot + "/Art/UI/Newspaper";
        private const string FontAsset = ProjectRoot + "/Art/Fonts/Jost-Medium SDF.asset";
        private const string RootName = "PaperReader";
        public const string NoteName = "Note_CITY_HALL_SAYS_NO";

        public static Sprite[] Pages()
        {
            var a = LoadSprite(Art + "/Gazette_Page1.png");
            var b = LoadSprite(Art + "/Gazette_Page2.png");
            return a != null && b != null ? new[] { a, b } : null;
        }

        [MenuItem("Tools/The Quiet Witness/Setup/Map: the Gazette to read")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Gazette", "Stop Play mode first.", "OK");
                return;
            }
            var pages = Pages();
            var left = LoadSprite(Art + "/Arrow_Left.png");
            var right = LoadSprite(Art + "/Arrow_Right.png");
            var dot = LoadSprite(Art + "/Page_Dot.png");
            if (pages == null || left == null || right == null || dot == null) return;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAsset);

            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                var old = root.transform.Find(RootName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var canvas = UiBuild.Canvas(root.transform, RootName, 62);   // over the town map (58)

                var screen = Rect("Screen", canvas.transform);
                Stretch(screen);
                var group = screen.gameObject.AddComponent<CanvasGroup>();
                var dim = Img("Dim", screen, null, new Color(0f, 0f, 0f, 0.8f), raycast: true);
                Stretch(dim.rectTransform);

                Vector2 at = new Vector2(0f, 14f);
                var shadow = Img("Shadow", screen, pages[0], new Color(0f, 0f, 0f, 0.45f));
                Place(shadow.rectTransform, at + new Vector2(10f, -10f), pages[0].rect.size * 2f);
                var page = Img("Page", screen, pages[0], Color.white, raycast: true);
                Place(page.rectTransform, at, pages[0].rect.size * 2f);

                var prev = Pixel("Prev", screen, left, 4f, at + Vector2.left * 300f);
                var next = Pixel("Next", screen, right, 4f, at + Vector2.right * 300f);
                var dots = Rect("Pages", screen);
                Place(dots, at + Vector2.down * 500f, new Vector2(200f, 20f));
                var dotTemplate = Pixel("Dot", dots, dot, 4f, Vector2.zero);
                dotTemplate.gameObject.SetActive(false);

                var keys = Text("Keys", screen, font, 22f, new Color(Plain.r, Plain.g, Plain.b, 0.5f), TextAlignmentOptions.TopRight, "Esc  Close");
                Place(keys.rectTransform, new Vector2(-60f, -24f), new Vector2(400f, 36f), new Vector2(1f, 1f), new Vector2(1f, 1f));

                var reader = canvas.AddComponent<PaperReader>();
                var so = new SerializedObject(reader);
                so.FindProperty("group").objectReferenceValue = group;
                so.FindProperty("page").objectReferenceValue = page;
                so.FindProperty("pageShadow").objectReferenceValue = shadow;
                so.FindProperty("pageScale").intValue = 2;
                so.FindProperty("prevArrow").objectReferenceValue = prev;
                so.FindProperty("nextArrow").objectReferenceValue = next;
                so.FindProperty("pageDots").objectReferenceValue = dots;
                so.FindProperty("dotTemplate").objectReferenceValue = dotTemplate;
                so.ApplyModifiedPropertiesWithoutUndo();
                group.alpha = 0f;
                group.blocksRaycasts = false;

                // the clipping's dot on the board opens the paper
                var map = root.GetComponentInChildren<TownMap>(true);
                var board = map != null ? new SerializedObject(map).FindProperty("board").objectReferenceValue as RectTransform : null;
                var note = board != null ? board.Find(NoteName) : null;
                if (note == null || !SetPages(note.GetComponent<MapDistrict>(), pages))
                    Debug.LogWarning("[Setup] No City Hall dot on the board: run Setup > Map: dots on the board's photos, then this again.");

                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] The Gazette: open the cork board in the office and click the City Hall clipping.");
        }

        public static bool SetPages(MapDistrict district, Sprite[] pages)
        {
            if (district == null || pages == null) return false;
            var so = new SerializedObject(district);
            var list = so.FindProperty("readPages");
            list.arraySize = pages.Length;
            for (int i = 0; i < pages.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = pages[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }
    }
}
