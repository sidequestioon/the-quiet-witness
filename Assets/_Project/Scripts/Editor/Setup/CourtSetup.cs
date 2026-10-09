using QuietWitness.Board;
using QuietWitness.Core;
using QuietWitness.Court;
using QuietWitness.UI;
using QuietWitness.World;
using UnityEditor;
using UnityEngine;
using static QuietWitness.EditorTools.SetupUtil;

namespace QuietWitness.EditorTools
{
    // One click for the courtroom task: a test trial with three verdicts,
    // Tuesday's newspaper, the court UI in Systems and a courtroom door in the police station.
    // Menu: Tools > The Quiet Witness > Setup > Court
    public static class CourtSetup
    {
        private const string Prologue = ProjectRoot + "/Cases/Prologue";

        [MenuItem("Tools/The Quiet Witness/Setup/Court")]
        private static void Run()
        {
            var bottle = LoadAsset<ClueData>(Prologue + "/BottleNoBlood.asset");
            var report = LoadAsset<ClueData>(Prologue + "/CoronerReport.asset");
            var notWeapon = LoadAsset<ConclusionData>(Prologue + "/BottleNotWeapon.asset");

            // 1. Tuesday's paper: the headline follows the verdict.
            var tuesday = CreateAsset<NewspaperData>(Prologue + "/Paper_Tuesday.asset", so =>
            {
                so.FindProperty("id").stringValue = "tue_verdict";
                so.FindProperty("dateLine").stringValue = "Tuesday · Morning Edition";

                var headlines = so.FindProperty("headlines");
                headlines.arraySize = 3;
                var free = headlines.GetArrayElementAtIndex(0);
                SetFlags(free.FindPropertyRelative("when"), "ending:not_guilty");
                SetString(free, "headline", "Not guilty");
                SetString(free, "lead", "The jury took less than an hour. The defense raised doubts the police never looked at.");
                var hung = headlines.GetArrayElementAtIndex(1);
                SetFlags(hung.FindPropertyRelative("when"), "ending:hung_jury");
                SetString(hung, "headline", "Hung jury in park case");
                SetString(hung, "lead", "The jury could not agree. A new trial is expected.");
                var guilty = headlines.GetArrayElementAtIndex(2);
                SetString(guilty, "headline", "Guilty");
                SetString(guilty, "lead", "The court found the defendant guilty. The town moves on.");
                so.FindProperty("articles").arraySize = 0;
            });

            // 2. The test trial.
            var trial = CreateAsset<CourtCase>(Prologue + "/TestTrial.asset", so =>
            {
                so.FindProperty("id").stringValue = "test_trial";
                so.FindProperty("title").stringValue = "The People v. the drifter (test)";
                so.FindProperty("intro").stringValue = "Monday, nine a.m. The courtroom is full. The prosecution looks sure of itself.";

                var points = so.FindProperty("points");
                points.arraySize = 2;
                var p1 = points.GetArrayElementAtIndex(0);
                SetString(p1, "claim", "The defendant struck the victim with the bottle.");
                var p1c = p1.FindPropertyRelative("answeredByConclusions");
                p1c.arraySize = 1;
                p1c.GetArrayElementAtIndex(0).objectReferenceValue = notWeapon;
                p1.FindPropertyRelative("answeredByClues").arraySize = 0;
                SetString(p1, "sustained", "No blood on the bottle, yet a blow to the head. The jury looks at the bottle differently now.");
                SetString(p1, "overruled", "The bottle stays on the evidence table. The jury nods along.");

                var p2 = points.GetArrayElementAtIndex(1);
                SetString(p2, "claim", "She simply froze in the cold. There was no one else.");
                var p2c = p2.FindPropertyRelative("answeredByClues");
                p2c.arraySize = 1;
                p2c.GetArrayElementAtIndex(0).objectReferenceValue = report;
                p2.FindPropertyRelative("answeredByConclusions").arraySize = 0;
                SetString(p2, "sustained", "The coroner's report speaks of a blow to the head. Someone else was there.");
                SetString(p2, "overruled", "The word \"cold\" hangs in the air.");

                var verdicts = so.FindProperty("verdicts");
                verdicts.arraySize = 3;
                SetVerdict(verdicts.GetArrayElementAtIndex(0), "not_guilty", "Not guilty", 2,
                    "The defendant walks out of the courtroom a free man.", tuesday);
                SetVerdict(verdicts.GetArrayElementAtIndex(1), "hung_jury", "Hung jury", 1,
                    "The jury cannot agree. It isn't over yet.", tuesday);
                SetVerdict(verdicts.GetArrayElementAtIndex(2), "guilty", "Guilty", 0,
                    "The gavel falls. The town gets the ending it expected.", tuesday);
            });
            AssetDatabase.SaveAssets();

            // 3. UI in Systems; the newspaper panel learns about Tuesday's paper.
            EditSystemsCanvas(canvas =>
            {
                if (canvas.GetComponentInChildren<CourtPanel>(true) == null) CourtUIBuilder.BuildAll(canvas);

                var paper = canvas.GetComponentInChildren<NewspaperPanel>(true);
                if (paper != null)
                {
                    var so = new SerializedObject(paper);
                    SetObjectList(so, "editions", FindAll<NewspaperData>());
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            });

            // 4. Courtroom door in the police station (test access for v0.1).
            if (!OpenScene(Scene("PoliceStation"))) return;
            // Opening a scene unloads assets nobody holds yet, so load the trial again.
            trial = LoadAsset<CourtCase>(Prologue + "/TestTrial.asset");
            var door = CloneInScene("Exit", "CourtDoor", new Vector3(3f, -1.25f, 0f), new Color(0.7f, 0.55f, 0.2f));
            if (door != null)
            {
                RemoveComponent<QuietWitness.Map.MapOpener>(door);
                var opener = door.AddComponent<CourtOpener>();
                var openerSo = new SerializedObject(opener);
                openerSo.FindProperty("courtCase").objectReferenceValue = trial;
                openerSo.ApplyModifiedPropertiesWithoutUndo();

                var interactable = door.GetComponent<Interactable>();
                SetField(interactable, "prompt", "Enter the courtroom");
                SetOnlyListener(interactable, "onInteract", opener.Begin);
                SaveOpenScene();
            }

            Debug.Log("[Setup] Court done. Play from Office, reach Saturday, go to the police station.");
        }

        private static void SetVerdict(SerializedProperty v, string id, string title, int minScore,
                                       string text, NewspaperData paper)
        {
            SetString(v, "id", id);
            SetString(v, "title", title);
            SetString(v, "text", text);
            v.FindPropertyRelative("minScore").intValue = minScore;
            SetFlags(v.FindPropertyRelative("when"));
            v.FindPropertyRelative("newspaper").objectReferenceValue = paper;
        }
    }
}
