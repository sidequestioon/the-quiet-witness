using System;
using System.Collections;
using System.Collections.Generic;
using QuietWitness.Core;
using UnityEngine;
using UnityEngine.Events;

namespace QuietWitness.World
{
    // What a hotspot does can change as the story goes on: a list of steps, and the first step
    // that fits the flags right now is used. E.g. the locked drawer:
    //   1. not opened yet  -> "Four numbers..." then the code lock opens
    //   2. (anything else) -> "Empty now."
    // Each step: the flags it needs, the flags that rule it out, a line from Hero Lines,
    // a flag to set, and what happens after he has said the line (Then).
    // Hooked up as Hotspot Actions > On Chosen -> Hotspot Steps.Run (and the action's own Line empty).
    public class HotspotSteps : MonoBehaviour
    {
        [Serializable]
        public class Step
        {
            [Tooltip("Just a note for you, e.g. \"before the drawer is open\".")]
            public string note;
            [Tooltip("All of these flags must be set. Empty = no need.")]
            public List<string> needs = new List<string>();
            [Tooltip("None of these flags may be set.")]
            public List<string> unless = new List<string>();
            [Tooltip("What he says (from Hero Lines). Empty = nothing.")]
            [HeroLine]
            public string line;
            [Tooltip("Flag set when this step is used, e.g. darkroom:photo_seen. Empty = none.")]
            public string setFlag;
            [Tooltip("What happens after he has said the line: open a close-up, the map...")]
            public UnityEvent then = new UnityEvent();

            public bool Fits()
            {
                var state = GameState.Instance;
                foreach (var f in needs)
                    if (!string.IsNullOrWhiteSpace(f) && (state == null || !state.HasFlag(f.Trim()))) return false;
                foreach (var f in unless)
                    if (!string.IsNullOrWhiteSpace(f) && state != null && state.HasFlag(f.Trim())) return false;
                return true;
            }
        }

        [Tooltip("Checked from the top: the first step that fits is used.")]
        public List<Step> steps = new List<Step>();

        public void Run()
        {
            foreach (var step in steps)
            {
                if (!step.Fits()) continue;
                StartCoroutine(Play(step));
                return;
            }
        }

        private IEnumerator Play(Step step)
        {
            if (!string.IsNullOrWhiteSpace(step.setFlag)) GameState.Instance?.SetFlag(step.setFlag.Trim());
            var say = new HotspotAction { line = step.line };
            if (say.HasSpeech && HeroSpeech.Say(say))
            {
                yield return null;
                while (HeroSpeech.IsSpeaking) yield return null;
            }
            step.then?.Invoke();
        }
    }
}
