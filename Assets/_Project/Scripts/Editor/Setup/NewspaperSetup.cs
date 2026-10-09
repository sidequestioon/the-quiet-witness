using QuietWitness.Core;
using QuietWitness.UI;
using QuietWitness.World;
using UnityEditor;
using UnityEngine;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // One click instead of the manual steps for the newspaper task:
    // test clue, two editions, newspaper + yes/no UI in Systems, "Call it a day" coat rack in Office.
    // Menu: Tools > The Quiet Witness > Setup > Newspaper
    public static class NewspaperSetup
    {
        private const string Prologue = ProjectRoot + "/Cases/Prologue";

        [MenuItem("Tools/The Quiet Witness/Setup/Newspaper")]
        private static void Run()
        {
            // 1. Data.
            var adClue = CreateAsset<ClueData>(Prologue + "/TestPaperAd.asset", so =>
            {
                so.FindProperty("id").stringValue = "paper_ad";
                so.FindProperty("title").stringValue = "Classified ad";
                so.FindProperty("description").stringValue = "Test: a clue hidden in the classifieds.";
            });

            var friday = LoadAsset<TimeSegment>(ProjectRoot + "/Cases/Time/FridayEvening.asset");
            var saturday = LoadAsset<TimeSegment>(ProjectRoot + "/Cases/Time/SaturdatMorning.asset");

            CreateAsset<NewspaperData>(Prologue + "/Paper_FridayEvening.asset", so =>
            {
                so.FindProperty("id").stringValue = "fri_evening";
                so.FindProperty("dateLine").stringValue = "Friday · Evening Edition";
                so.FindProperty("segment").objectReferenceValue = friday;

                var headlines = so.FindProperty("headlines");
                headlines.arraySize = 1;
                var h = headlines.GetArrayElementAtIndex(0);
                SetString(h, "headline", "Trial set for Monday");
                SetString(h, "lead", "The man accused of the park death will face the judge on Monday morning. Police call the case closed.");

                var articles = so.FindProperty("articles");
                articles.arraySize = 2;
                var weather = articles.GetArrayElementAtIndex(0);
                SetString(weather, "title", "Weather");
                SetString(weather, "body", "Rain through the weekend. Bring an umbrella.");
                var ad = articles.GetArrayElementAtIndex(1);
                SetString(ad, "title", "Classifieds");
                SetString(ad, "body", "Lost: grey umbrella. Found: patience. Call 555-0142.");
                ad.FindPropertyRelative("clue").objectReferenceValue = adClue;
            });

            CreateAsset<NewspaperData>(Prologue + "/Paper_SaturdayMorning.asset", so =>
            {
                so.FindProperty("id").stringValue = "sat_morning";
                so.FindProperty("dateLine").stringValue = "Saturday · Morning Edition";
                so.FindProperty("segment").objectReferenceValue = saturday;

                var headlines = so.FindProperty("headlines");
                headlines.arraySize = 2;
                var talked = headlines.GetArrayElementAtIndex(0);
                SetFlags(talked.FindPropertyRelative("when"), "talked:test_stranger");
                SetString(talked, "headline", "Lawyer asks questions downtown");
                SetString(talked, "lead", "The defense attorney was seen talking to locals last night.");
                var standard = headlines.GetArrayElementAtIndex(1);
                SetString(standard, "headline", "Two days until the trial");
                SetString(standard, "lead", "The town waits for Monday.");

                var articles = so.FindProperty("articles");
                articles.arraySize = 1;
                var weather = articles.GetArrayElementAtIndex(0);
                SetString(weather, "title", "Weather");
                SetString(weather, "body", "Morning fog, clearing by noon.");
            });
            AssetDatabase.SaveAssets();

            // 2. UI in the Systems prefab.
            EditSystemsCanvas(canvas =>
            {
                var panel = canvas.GetComponentInChildren<NewspaperPanel>(true);
                if (panel == null) panel = NewspaperUIBuilder.BuildAll(canvas).GetComponent<NewspaperPanel>();

                var so = new SerializedObject(panel);
                SetObjectList(so, "editions", FindAll<NewspaperData>());
                so.ApplyModifiedPropertiesWithoutUndo();
            });

            // 3. Coat rack in the Office.
            if (!OpenScene(Scene("Office"))) return;
            var rack = CloneInScene("Map_Wall", "CoatRack", new Vector3(6f, -0.5f, 0f), new Color(0.55f, 0.4f, 0.25f));
            if (rack != null)
            {
                RemoveComponent<QuietWitness.Map.MapOpener>(rack);
                var advancer = rack.AddComponent<TimeAdvancer>();
                var interactable = rack.GetComponent<Interactable>();
                SetField(interactable, "prompt", "Call it a day");
                SetOnlyListener(interactable, "onInteract", advancer.Use);
                SaveOpenScene();
            }

            Debug.Log("[Setup] Newspaper done. Press Play in Office to test.");
        }
    }
}
