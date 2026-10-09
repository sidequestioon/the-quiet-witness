using System;
using System.Collections.Generic;
using QuietWitness.Board;
using QuietWitness.Core;
using QuietWitness.UI;
using UnityEngine;

namespace QuietWitness.Court
{
    // The trial: the prosecution makes claims, the player answers each one with evidence.
    // Every right answer adds a point; the score (and flags) pick the verdict.
    [CreateAssetMenu(fileName = "CourtCase", menuName = "The Quiet Witness/Court Case")]
    public class CourtCase : ScriptableObject
    {
        [Serializable]
        public class Point
        {
            [Tooltip("What the prosecution claims.")]
            [TextArea(2, 4)] public string claim;
            [Tooltip("Evidence that breaks the claim (any one of them).")]
            public List<ClueData> answeredByClues = new List<ClueData>();
            public List<ConclusionData> answeredByConclusions = new List<ConclusionData>();
            [Tooltip("What happens when the player presents the right evidence.")]
            [TextArea(2, 5)] public string sustained;
            [Tooltip("What happens on wrong evidence or when the player lets it pass.")]
            [TextArea(2, 5)] public string overruled;

            public bool IsAnsweredBy(string cardFlag)
            {
                foreach (var clue in answeredByClues)
                    if (clue != null && clue.Flag == cardFlag) return true;
                foreach (var conclusion in answeredByConclusions)
                    if (conclusion != null && conclusion.Flag == cardFlag) return true;
                return false;
            }
        }

        [Serializable]
        public class Verdict
        {
            public string id;
            public string title;
            [TextArea(3, 8)] public string text;
            [Tooltip("Needs at least this many points.")]
            public int minScore;
            [Tooltip("Extra flags (choices made before the trial). Empty = none.")]
            public Condition when;
            [Tooltip("Optional: Tuesday's newspaper, opened after the verdict.")]
            public NewspaperData newspaper;

            public string Flag => "ending:" + id;
        }

        [SerializeField] private string id;
        [SerializeField] private string title = "The People v. ...";
        [TextArea(3, 8)]
        [SerializeField] private string intro;
        [SerializeField] private List<Point> points = new List<Point>();
        [Tooltip("Checked top to bottom: the first one that fits is the verdict. Put the best first.")]
        [SerializeField] private List<Verdict> verdicts = new List<Verdict>();

        public string Id => id;
        public string Title => title;
        public string Intro => intro;
        public IReadOnlyList<Point> Points => points;

        public Verdict PickVerdict(int score, GameState state)
        {
            foreach (var v in verdicts)
                if (v != null && score >= v.minScore && (v.when == null || v.when.IsMet(state))) return v;
            return verdicts.Count > 0 ? verdicts[verdicts.Count - 1] : null;
        }
    }
}
