using System.Collections.Generic;
using QuietWitness.Core;
using UnityEngine;

namespace QuietWitness.Archive
{
    // One record: a person's dossier, a phone book line, an old case, a clipping.
    // Reading it for the first time sets "archive:<id>" and hands out its clues and flags.
    // To put an address on the map: add "archive:<id>" to that Location's unlock condition.
    [CreateAssetMenu(fileName = "ArchiveEntry", menuName = "The Quiet Witness/Archive Entry")]
    public class ArchiveEntry : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string title;
        [Tooltip("Job, address or case number. Shown under the title.")]
        [SerializeField] private string subtitle;
        [Tooltip("Optional photo or document scan.")]
        [SerializeField] private Sprite photo;
        [TextArea(4, 12)]
        [SerializeField] private string body;

        [Header("Search")]
        [Tooltip("Words the player must type to find this record, e.g. 'Hale', 'Elm Street'. " +
                 "Case and extra spaces don't matter. The title always works too.")]
        [SerializeField] private List<string> keywords = new List<string>();
        [Tooltip("The record can't be found until this is met. Empty = always.")]
        [SerializeField] private Condition availableWhen;

        [Header("Reward on first read")]
        [SerializeField] private List<ClueData> grantsClues = new List<ClueData>();
        [Tooltip("Extra flags, e.g. 'knows:judge_car'.")]
        [SerializeField] private List<string> grantsFlags = new List<string>();

        public string Id => id;
        public string Title => title;
        public string Subtitle => subtitle;
        public Sprite Photo => photo;
        public string Body => body;
        public string ReadFlag => "archive:" + id;

        public bool IsAvailable(GameState state) => availableWhen == null || availableWhen.IsMet(state);

        public bool IsRead(GameState state) => state != null && state.HasFlag(ReadFlag);

        // Exact match with a keyword or the title. No partial matches:
        // the player has to actually know what to look for.
        public bool Matches(string query)
        {
            string q = Normalize(query);
            if (q.Length == 0) return false;
            if (Normalize(title) == q) return true;
            foreach (var keyword in keywords)
                if (Normalize(keyword) == q) return true;
            return false;
        }

        // Returns true the first time the record is read.
        public bool MarkRead(GameState state)
        {
            if (state == null || !state.SetFlag(ReadFlag)) return false;
            foreach (var clue in grantsClues) state.FindClue(clue);
            foreach (var flag in grantsFlags) state.SetFlag(flag);
            return true;
        }

        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var parts = text.Trim().ToLowerInvariant()
                .Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }
    }
}
