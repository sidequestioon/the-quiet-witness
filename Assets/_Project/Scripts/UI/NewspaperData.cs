using System;
using System.Collections.Generic;
using QuietWitness.Core;
using UnityEngine;

namespace QuietWitness.UI
{
    // One edition of the town paper. It opens by itself when its time segment starts.
    // The headline reacts to what the player did; small articles can hide clues.
    [CreateAssetMenu(fileName = "Newspaper", menuName = "The Quiet Witness/Newspaper")]
    public class NewspaperData : ScriptableObject
    {
        [Serializable]
        public class Headline
        {
            [Tooltip("Used when this is met. Empty = always (put the default last).")]
            public Condition when;
            public string headline;
            [TextArea(2, 6)] public string lead;
        }

        [Serializable]
        public class Article
        {
            public string title;
            [TextArea(2, 6)] public string body;
            [Tooltip("The article is printed only when this is met. Empty = always.")]
            public Condition when;
            [Tooltip("Optional: clicking the article circles it and gives this clue.")]
            public ClueData clue;
        }

        [SerializeField] private string id;
        [SerializeField] private string masthead = "THE MILLBROOK COURIER";
        [SerializeField] private string dateLine = "Evening Edition";
        [Tooltip("The edition opens when this time segment starts.")]
        [SerializeField] private TimeSegment segment;
        [Tooltip("The first one whose condition is met is printed.")]
        [SerializeField] private List<Headline> headlines = new List<Headline>();
        [SerializeField] private List<Article> articles = new List<Article>();

        public string Id => id;
        public string Masthead => masthead;
        public string DateLine => dateLine;
        public TimeSegment Segment => segment;
        public IReadOnlyList<Article> Articles => articles;
        public string ReadFlag => "paper:" + id;

        public Headline PickHeadline(GameState state)
        {
            foreach (var h in headlines)
                if (h != null && (h.when == null || h.when.IsMet(state))) return h;
            return null;
        }
    }
}
