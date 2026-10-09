using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuietWitness.Core
{
    // "Unlocked when ALL of these are true."
    [Serializable]
    public class Condition
    {
        [SerializeField] private List<ClueData> requiredClues = new List<ClueData>();
        [Tooltip("Extra flags, e.g. talked:client")]
        [SerializeField] private List<string> requiredFlags = new List<string>();

        public bool IsMet(GameState state)
        {
            if (state == null) return false;

            foreach (var clue in requiredClues)
                if (clue != null && !state.HasClue(clue)) return false;

            foreach (var flag in requiredFlags)
                if (!string.IsNullOrEmpty(flag) && !state.HasFlag(flag)) return false;

            return true;
        }
    }
}