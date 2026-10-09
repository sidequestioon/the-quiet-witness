using System.Collections.Generic;
using QuietWitness.Core;
using UnityEngine;

namespace QuietWitness.Board
{
    // What the player figures out by connecting two cards on the board.
    // A card is a clue or another conclusion, so conclusions can build on each other.
    // Reaching it sets "conclusion:<id>".
    [CreateAssetMenu(fileName = "Conclusion", menuName = "The Quiet Witness/Conclusion")]
    public class ConclusionData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string title;
        [TextArea(3, 8)]
        [SerializeField] private string description;

        [Header("Connect these two cards (two in total)")]
        [SerializeField] private List<ClueData> fromClues = new List<ClueData>();
        [SerializeField] private List<ConclusionData> fromConclusions = new List<ConclusionData>();

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public string Flag => "conclusion:" + id;

        // Flags of the two cards that make this conclusion.
        public List<string> InputFlags()
        {
            var flags = new List<string>();
            foreach (var clue in fromClues) if (clue != null) flags.Add(clue.Flag);
            foreach (var conclusion in fromConclusions) if (conclusion != null) flags.Add(conclusion.Flag);
            return flags;
        }

        public bool Matches(string flagA, string flagB)
        {
            var inputs = InputFlags();
            if (inputs.Count != 2) return false;
            return (inputs[0] == flagA && inputs[1] == flagB) || (inputs[0] == flagB && inputs[1] == flagA);
        }

        private void OnValidate()
        {
            int count = InputFlags().Count;
            if (count != 0 && count != 2)
                Debug.LogWarning($"[Board] Conclusion '{name}' needs exactly two cards, has {count}.", this);
        }
    }
}
