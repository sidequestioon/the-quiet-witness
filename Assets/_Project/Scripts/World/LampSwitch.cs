using QuietWitness.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace QuietWitness.World
{
    // A lamp or neon sign: on when the current preset says lamps are on.
    [RequireComponent(typeof(Light2D))]
    public class LampSwitch : MonoBehaviour
    {
        private Light2D lamp;

        private void Awake() => lamp = GetComponent<Light2D>();

        private void Start()
        {
            TimeManager.Instance.SegmentChanged += OnSegmentChanged;
            OnSegmentChanged(TimeManager.Instance.Current);
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.SegmentChanged -= OnSegmentChanged;
        }

        private void OnSegmentChanged(TimeSegment segment)
        {
            if (segment == null || segment.Lighting == null) return;
            lamp.enabled = segment.Lighting.LampsOn;
        }
    }
}