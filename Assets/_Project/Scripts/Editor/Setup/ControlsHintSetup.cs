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
    // The controls window: shown once in the prologue, when the hero gets up after the knock.
    // Built on Systems; the plate looks like the map's info plate. Texts are in Rows below (or on the objects
    // under ControlsHint > Screen > Window after the run). Safe to run again.
    // Menu: Tools > The Quiet Witness > Setup > Prologue: controls window
    public static class ControlsHintSetup
    {
        private const string Art = ProjectRoot + "/Art/UI/Controls";
        private const string FontAsset = ProjectRoot + "/Art/Fonts/Jost-Medium SDF.asset";
        private const string RootName = "ControlsHint";

        private static readonly (string keys, string text)[] Rows =
        {
            ("Keys_Walk", "Walk"),
            ("Keys_Use", "Look, take, use"),
            ("Keys_Choose", "Pick an action"),
            ("Keys_CaseFile", "Case file"),
            ("Keys_Close", "Close, go back"),
        };

        [MenuItem("Tools/The Quiet Witness/Setup/Prologue: controls window")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Controls", "Stop Play mode first.", "OK");
                return;
            }
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAsset);
            var keys = new Sprite[Rows.Length];
            for (int i = 0; i < Rows.Length; i++)
            {
                keys[i] = LoadSprite($"{Art}/{Rows[i].keys}.png");
                if (keys[i] == null) return;
            }

            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                var old = root.transform.Find(RootName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var canvas = UiBuild.Canvas(root.transform, RootName, 63);

                var screen = Rect("Screen", canvas.transform);
                Stretch(screen);
                var group = screen.gameObject.AddComponent<CanvasGroup>();
                var dim = Img("Dim", screen, null, new Color(0f, 0f, 0f, 0.45f), raycast: true);
                Stretch(dim.rectTransform);

                // the plate: like the map's info plate
                var window = Img("Window", screen, null, new Color(0.08f, 0.07f, 0.09f, 0.95f), raycast: true);
                Place(window.rectTransform, Vector2.zero, new Vector2(640f, 472f));
                var map = root.GetComponentInChildren<TownMap>(true);
                var info = map != null ? new SerializedObject(map).FindProperty("info").objectReferenceValue as RectTransform : null;
                var infoImage = info != null ? info.GetComponent<Image>() : null;
                if (infoImage != null)
                {
                    window.sprite = infoImage.sprite;
                    window.type = infoImage.type;
                    window.pixelsPerUnitMultiplier = infoImage.pixelsPerUnitMultiplier;
                    window.color = infoImage.color;
                }
                var w = window.rectTransform;

                var title = Text("Title", w, font, 30f, Yellow, TextAlignmentOptions.Center, "CONTROLS");
                title.characterSpacing = 4f;
                Place(title.rectTransform, new Vector2(0f, 192f), new Vector2(560f, 44f));

                for (int i = 0; i < Rows.Length; i++)
                {
                    float y = 124f - i * 62f;
                    var row = Rect("Row_" + Rows[i].text.Split(',')[0].Replace(' ', '_'), w);
                    Place(row, new Vector2(0f, y), new Vector2(600f, 56f));
                    var pic = Img("Keys", row, keys[i], Color.white);
                    Place(pic.rectTransform, new Vector2(-20f, 0f), keys[i].rect.size * 4f, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f));
                    var label = Text("Text", row, font, 26f, Plain, TextAlignmentOptions.Left, Rows[i].text);
                    Place(label.rectTransform, new Vector2(20f, 0f), new Vector2(280f, 44f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f));
                }

                var cont = Text("Continue", w, font, 22f, new Color(Plain.r, Plain.g, Plain.b, 0.5f), TextAlignmentOptions.Center, "E  Continue");
                Place(cont.rectTransform, new Vector2(0f, -196f), new Vector2(400f, 36f));

                var hint = canvas.AddComponent<ControlsHint>();
                var so = new SerializedObject(hint);
                so.FindProperty("group").objectReferenceValue = group;
                so.FindProperty("window").objectReferenceValue = w;
                so.ApplyModifiedPropertiesWithoutUndo();
                group.alpha = 0f;
                group.blocksRaycasts = false;

                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Controls window done: start a new game, it comes when he gets up after the knock.");
        }
    }
}
