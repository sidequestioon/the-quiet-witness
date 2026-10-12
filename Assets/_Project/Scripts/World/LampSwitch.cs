using QuietWitness.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace QuietWitness.World
{
    // A lamp or neon sign: on when the current preset says lamps are on.
    [RequireComponent(typeof(Light2D))]
    public class LampSwitch : MonoBehaviour
    {
        [Tooltip("On at any time of day, even in the morning and the afternoon (a desk lamp he never switches off).")]
        [SerializeField] private bool alwaysOn;

        private Light2D lamp;

        private void Awake() => lamp = GetComponent<Light2D>();

        private void Start()
        {
            TimeManager.Instance.SegmentChanged += OnSegmentChanged;
            OnSegmentChanged(TimeManager.Instance.Current);
#if UNITY_EDITOR
            GlobalLightController.PreviewChanged += OnPreview;
#endif
        }

#if UNITY_EDITOR
        private void OnPreview(LightingPreset preset) => lamp.enabled = alwaysOn || preset.LampsOn;
#endif

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.SegmentChanged -= OnSegmentChanged;
#if UNITY_EDITOR
            GlobalLightController.PreviewChanged -= OnPreview;
#endif
        }

        private void OnSegmentChanged(TimeSegment segment)
        {
            if (segment == null || segment.Lighting == null) return;
            lamp.enabled = alwaysOn || segment.Lighting.LampsOn;
        }
    }
}