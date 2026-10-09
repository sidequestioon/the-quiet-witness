using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuietWitness.Core
{
    // Single source of truth: what the player has found and unlocked.
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        // Fired the first time a flag is set.
        public event Action<string> FlagSet;

        // Fired the first time a clue is found.
        public event Action<ClueData> ClueFound;

        private readonly HashSet<string> flags = new HashSet<string>();
        private readonly List<ClueData> foundClues = new List<ClueData>();

        public IReadOnlyList<ClueData> FoundClues => foundClues;

        private void Awake()
        {
            // Only one GameState may exist.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // survives scene changes
        }

        public bool HasFlag(string flag) => flags.Contains(flag);

        // Returns true only if the flag is new.
        public bool SetFlag(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag)) return false;
            if (!flags.Add(flag)) return false; // already set
            FlagSet?.Invoke(flag);
            return true;
        }

        public bool FindClue(ClueData clue)
        {
            if (clue == null) return false;
            if (!SetFlag(clue.Flag)) return false; // already found
            foundClues.Add(clue);
            ClueFound?.Invoke(clue);
            return true;
        }

        public bool HasClue(ClueData clue) => clue != null && HasFlag(clue.Flag);
    }
}