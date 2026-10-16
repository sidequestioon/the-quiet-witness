using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuietWitness.Core
{
    // What the hero is trying to do, for the Objectives page of the case file.
    // Each goal shows up once its flag is set and is crossed out once its done-flag is set.
    // A main goal is the big yellow line; the goals under it in this list are its steps.
    // A step marked Mini is a smaller step of the step above it (shown under it, with a dot).
    // Only the newest main goal that is not done yet is shown (with its steps).
    [CreateAssetMenu(fileName = "Objectives", menuName = "The Quiet Witness/Objectives")]
    public class ObjectiveList : ScriptableObject
    {
        [Serializable]
        public class Goal
        {
            [Tooltip("Unique id, e.g. who_sent. Don't change it later: saves use it.")]
            public string id;
            [Tooltip("Main goal: the big yellow line. The goals below it (up to the next main goal) are its steps.")]
            public bool main;
            [Tooltip("A smaller step of the step above it: shown under it, a little to the right, with a dot.")]
            public bool mini;
            [Tooltip("A side goal (a favor, an errand): listed under the main goal and its steps, not part of them.")]
            public bool side;
            [TextArea(1, 3)]
            public string text;
            [Tooltip("Shown once this flag is set, e.g. prologue:envelope_opened or visited:diner. Empty = from the start.")]
            public string showWhen;
            [Tooltip("Crossed out once this flag is set. Empty = never.")]
            public string doneWhen;

            public string SeenFlag => "seen:goal:" + id;
        }

        public List<Goal> goals = new List<Goal>();

        public static bool Has(string flag) =>
            string.IsNullOrWhiteSpace(flag) || (GameState.Instance != null && GameState.Instance.HasFlag(flag.Trim()));

        public static bool Shown(Goal goal) => goal != null && Has(goal.showWhen);
        public static bool Done(Goal goal) => goal != null && !string.IsNullOrWhiteSpace(goal.doneWhen) && Has(goal.doneWhen);

        // The main goal on screen (the newest open one, or the newest one at all) and its shown steps.
        public Goal Current(List<Goal> steps)
        {
            steps?.Clear();
            int open = -1, any = -1;
            for (int i = 0; i < goals.Count; i++)
            {
                var g = goals[i];
                if (!g.main || g.side || !Shown(g)) continue;
                any = i;
                if (!Done(g)) open = i;
            }
            int main = open >= 0 ? open : any;
            // Steps: from after the main goal up to the next main goal (or, with no main goal, from the start).
            for (int i = main + 1; i < goals.Count && !goals[i].main; i++)
                if (!goals[i].side && Shown(goals[i])) steps?.Add(goals[i]);
            return main >= 0 ? goals[main] : null;
        }

        // The side goals on screen (shown ones, in order).
        public void Sides(List<Goal> into)
        {
            into.Clear();
            foreach (var g in goals)
                if (g != null && g.side && Shown(g)) into.Add(g);
        }
    }
}
