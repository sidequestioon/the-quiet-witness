using System.Collections.Generic;
using QuietWitness.Core;
using UnityEngine;

namespace QuietWitness.World
{
    // Until a flag is set, every hotspot in the scene is switched off (no icons, nothing to press, no doors)
    // except the ones in Except. E.g. the prologue: only the envelope until it has been opened.
    // When the flag is set, the hotspots come back.
    public class HotspotLock : MonoBehaviour
    {
        [Tooltip("Hotspots come back once this flag is set.")]
        [SerializeField] private string untilFlag = "prologue:envelope_opened";
        [Tooltip("Hotspots that stay on (e.g. Take_Envelope).")]
        [SerializeField] private List<GameObject> except = new List<GameObject>();

        private readonly List<GameObject> locked = new List<GameObject>();
        private GameState state;

        private void Start()
        {
            state = GameState.Instance;
            if (state == null || string.IsNullOrWhiteSpace(untilFlag) || state.HasFlag(untilFlag.Trim())) return;

            foreach (var hotspot in FindObjectsByType<HotspotActions>(FindObjectsSortMode.None))
            {
                var go = hotspot.gameObject;
                if (except.Contains(go) || !go.activeSelf) continue;
                go.SetActive(false);
                locked.Add(go);
            }
            state.FlagSet += OnFlagSet;
        }

        private void OnDestroy()
        {
            if (state != null) state.FlagSet -= OnFlagSet;
        }

        private void OnFlagSet(string flag)
        {
            if (flag != untilFlag.Trim()) return;
            foreach (var go in locked)
                if (go != null) go.SetActive(true);
            locked.Clear();
            state.FlagSet -= OnFlagSet;
        }
    }
}
