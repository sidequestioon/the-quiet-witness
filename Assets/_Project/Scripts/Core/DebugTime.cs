using UnityEngine;

namespace QuietWitness.Core
{
    // Temporary test helper for the time system.
    public class DebugTime : MonoBehaviour
    {
        private void Start()
        {
            if (TimeManager.Instance.gameObject != gameObject) return;

            TimeManager.Instance.ReadyToAdvance += s =>
                Debug.Log($"Ready to move on from {s.DisplayName}. {s.Warning}");
            TimeManager.Instance.SegmentChanged += s =>
                Debug.Log($"Now: {s.DisplayName}");
            Debug.Log($"Start: {TimeManager.Instance.Current.DisplayName}");
        }

        [ContextMenu("Advance time")]
        private void Advance() => TimeManager.Instance.Advance();
    }
}