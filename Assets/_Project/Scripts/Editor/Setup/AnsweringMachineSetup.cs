using QuietWitness.Core;
using QuietWitness.Map;
using QuietWitness.UI;
using QuietWitness.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static QuietWitness.EditorTools.SetupUtil;
using static QuietWitness.EditorTools.UiBuild;

namespace QuietWitness.EditorTools
{
    // The answering machine in the office has a message from Sal (the side story): its red light blinks;
    // Look / Listen; the message plays line by line at the bottom of the screen; then a side goal in the case file.
    // Makes the subtitles on Systems, the light and the Answering Machine in the Office scene.
    // The message is filled in only while the list is empty, so lines changed in Unity are kept.
    // Menu: Tools > The Quiet Witness > Setup > Office: answering machine
    public static class AnsweringMachineSetup
    {
        private const string Props = ProjectRoot + "/Art/Locations/Office/Props";
        private const string FontAsset = ProjectRoot + "/Art/Fonts/Jost-Medium SDF.asset";
        private const string Heard = "office:sal_heard";

        private static readonly (string speaker, string text)[] Message =
        {
            ("ANSWERING MACHINE", "You have... one... new message."),
            ("SAL", "Hey, it's Sal. Yeah, I know. Long time."),
            ("SAL", "Listen, I need a favor. Nothing big. Probably."),
            ("SAL", "My sister's kid, Danny, got himself into something down at the Docks."),
            ("SAL", "I can't go asking around. Not with the badge on. You know how it is."),
            ("SAL", "Meet me at the Blue Note. Tomorrow, after nine. First round's on me."),
            ("ANSWERING MACHINE", "End of messages."),
        };

        [MenuItem("Tools/The Quiet Witness/Setup/Office: answering machine")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Answering machine", "Stop Play mode first.", "OK");
                return;
            }
            var on = UiBuild.LoadSprite(Props + "/Machine_Led_On.png");
            var off = UiBuild.LoadSprite(Props + "/Machine_Led_Off.png");
            if (on == null || off == null) return;

            HeroLinesSetup.Ensure("office.machine_blink", "The red light is blinking. One new message. That's one more than usual.");
            HeroLinesSetup.Ensure("office.machine_heard", "Sal's message. The Blue Note, tomorrow, after nine.");
            HeroLinesSetup.Ensure("office.machine_after", "Sal never asks for anything. That's what worries me.");

            var objectives = AssetDatabase.LoadAssetAtPath<ObjectiveList>(ProjectRoot + "/Cases/Objectives.asset");
            if (objectives != null) ObjectivesLookSetup.FillGoals(objectives);

            BuildSubtitles();

            if (!OpenScene(Scene("Office"))) return;
            var hotspot = GameObject.Find("Look_AnsweringMachine");
            if (hotspot == null)
            {
                Debug.LogWarning("[Setup] Look_AnsweringMachine not found in the Office scene.");
                return;
            }

            // the little red light on the machine (picture pixels 76, 102-103)
            var led = GameObject.Find("Machine_Led");
            var background = GameObject.Find("Background");
            if (led == null && background != null && background.TryGetComponent<SpriteRenderer>(out var bg) && bg.sprite != null)
            {
                led = new GameObject("Machine_Led");
                led.transform.SetParent(hotspot.transform, false);
                float ppu = bg.sprite.pixelsPerUnit;
                Bounds b = bg.bounds;
                led.transform.position = new Vector3(b.min.x + 76.5f / ppu, b.max.y - 103f / ppu, 0f);
                var sr = led.AddComponent<SpriteRenderer>();
                sr.sortingOrder = -98;
            }
            var ledRenderer = led != null ? led.GetComponent<SpriteRenderer>() : null;
            if (ledRenderer != null) ledRenderer.sprite = off;

            if (!hotspot.TryGetComponent<AnsweringMachine>(out var machine)) machine = hotspot.AddComponent<AnsweringMachine>();
            var so = new SerializedObject(machine);
            var list = so.FindProperty("message");
            if (list.arraySize == 0)
            {
                list.arraySize = Message.Length;
                for (int i = 0; i < Message.Length; i++)
                {
                    var e = list.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("speaker").stringValue = Message[i].speaker;
                    e.FindPropertyRelative("text").stringValue = Message[i].text;
                }
            }
            so.FindProperty("heardFlag").stringValue = Heard;
            so.FindProperty("led").objectReferenceValue = ledRenderer;
            so.FindProperty("ledOn").objectReferenceValue = on;
            so.FindProperty("ledOff").objectReferenceValue = off;
            so.FindProperty("beep").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectRoot + "/Audio/SFX/Darkroom_Beep.wav");
            so.ApplyModifiedPropertiesWithoutUndo();

            // Look: the light blinks / Sal's message. Listen: plays it (any time).
            PrologueStorySetup.LookAndUse("Look_AnsweringMachine",
                new[]
                {
                    PrologueStorySetup.Step("heard it", needs: new[] { Heard }, line: "office.machine_heard"),
                    PrologueStorySetup.Step("a new message", line: "office.machine_blink"),
                },
                new[] { PrologueStorySetup.Step("play the message", then: machine.Play) },
                null, null, "Listen");

            EditorUtility.SetDirty(machine);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hotspot.scene);
            SaveOpenScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Answering machine done: walk up to it in the office, Look and Listen.");
        }

        // The subtitles at the bottom of the screen (on Systems), the plate like the map's info plate.
        private static void BuildSubtitles()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAsset);
            var root = PrefabUtility.LoadPrefabContents(SystemsPrefab);
            try
            {
                var old = root.transform.Find("VoiceSubtitles");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var canvas = UiBuild.Canvas(root.transform, "VoiceSubtitles", 61);
                var screen = Rect("Screen", canvas.transform);
                Stretch(screen);
                var group = screen.gameObject.AddComponent<CanvasGroup>();

                var plate = Img("Plate", screen, null, new Color(0.08f, 0.07f, 0.09f, 0.95f));
                Place(plate.rectTransform, new Vector2(0f, 150f), new Vector2(600f, 110f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
                var map = root.GetComponentInChildren<TownMap>(true);
                var info = map != null ? new SerializedObject(map).FindProperty("info").objectReferenceValue as RectTransform : null;
                var infoImage = info != null ? info.GetComponent<Image>() : null;
                if (infoImage != null)
                {
                    plate.sprite = infoImage.sprite;
                    plate.type = infoImage.type;
                    plate.pixelsPerUnitMultiplier = infoImage.pixelsPerUnitMultiplier;
                    plate.color = infoImage.color;
                }

                var speaker = Text("Speaker", plate.rectTransform, font, 20f, Yellow, TextAlignmentOptions.Center, "SAL");
                speaker.characterSpacing = 4f;
                Place(speaker.rectTransform, new Vector2(0f, 30f), new Vector2(500f, 28f));
                var text = Text("Text", plate.rectTransform, font, 26f, Plain, TextAlignmentOptions.Center, "...");
                text.textWrappingMode = TextWrappingModes.Normal;
                Place(text.rectTransform, new Vector2(0f, -10f), new Vector2(500f, 40f));

                var subs = canvas.AddComponent<VoiceSubtitles>();
                var so = new SerializedObject(subs);
                so.FindProperty("group").objectReferenceValue = group;
                so.FindProperty("plate").objectReferenceValue = plate.rectTransform;
                so.FindProperty("speaker").objectReferenceValue = speaker;
                so.FindProperty("text").objectReferenceValue = text;
                so.ApplyModifiedPropertiesWithoutUndo();
                group.alpha = 0f;

                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
