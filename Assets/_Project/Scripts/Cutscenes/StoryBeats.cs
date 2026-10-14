using System;
using System.Collections.Generic;
using QuietWitness.Core;
using QuietWitness.Map;
using QuietWitness.UI;
using QuietWitness.World;
using UnityEngine;

namespace QuietWitness.Cutscenes
{
    // Moments that happen by themselves once the story gets there, e.g. when the film is enlarged
    // and the badge is out of the drawer: he says "Riviera. Eleven thirty." and the map shows the way.
    // Each beat plays once (its Set Flag marks it done), as soon as the hero is free again
    // (no close-up, no speech, no travel). Lives on Systems, so it works in every room.
    public class StoryBeats : MonoBehaviour
    {
        [Serializable]
        public class Beat
        {
            [Tooltip("Just a note for you.")]
            public string note;
            [Tooltip("All of these flags must be set.")]
            public List<string> needs = new List<string>();
            [Tooltip("None of these flags may be set.")]
            public List<string> unless = new List<string>();
            [Tooltip("What he says (from Hero Lines).")]
            [HeroLine]
            public string line;
            [Tooltip("Set when the beat plays; also means \"done\", so it never plays twice. Required.")]
            public string setFlag;
        }

        public static StoryBeats Instance { get; private set; }

        [Tooltip("Seconds the hero must be free before a beat plays (so it doesn't cut into a close-up closing).")]
        [SerializeField] private float pause = 0.5f;
        public List<Beat> beats = new List<Beat>();

        private float freeFor;

        private void Awake()
        {
            if (Instance != null && Instance != this) return;   // duplicate Systems, removed by its root
            Instance = this;
        }

        private void Update()
        {
            if (Instance != this) return;
            bool busy = InputBlocker.IsBlocked || HeroSpeech.IsSpeaking || CaseFilePanel.IsOpen ||
                        (SceneLoader.Instance != null && SceneLoader.Instance.IsTravelling);
            freeFor = busy ? 0f : freeFor + Time.deltaTime;
            if (freeFor < pause) return;

            var state = GameState.Instance;
            if (state == null) return;
            foreach (var beat in beats)
            {
                if (string.IsNullOrWhiteSpace(beat.setFlag) || state.HasFlag(beat.setFlag.Trim())) continue;
                if (!Fits(beat, state)) continue;
                state.SetFlag(beat.setFlag.Trim());
                HeroSpeech.Say(new HotspotAction { line = beat.line });
                freeFor = 0f;
                return;
            }
        }

        private static bool Fits(Beat beat, GameState state)
        {
            foreach (var f in beat.needs)
                if (!string.IsNullOrWhiteSpace(f) && !state.HasFlag(f.Trim())) return false;
            foreach (var f in beat.unless)
                if (!string.IsNullOrWhiteSpace(f) && state.HasFlag(f.Trim())) return false;
            return true;
        }
    }
}
