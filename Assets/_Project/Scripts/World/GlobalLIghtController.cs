using System.Collections;
using QuietWitness.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace QuietWitness.World
{
    // Recolors the scene's Global Light 2D when time moves on.
    [RequireComponent(typeof(Light2D))]
    public class GlobalLightController : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 1.5f;

        private Light2D globalLight;
        private Coroutine fade;

        private void Awake() => globalLight = GetComponent<Light2D>();

        private void Start()
        {
            TimeManager.Instance.SegmentChanged += OnSegmentChanged;
            Apply(TimeManager.Instance.Current, instant: true);
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.SegmentChanged -= OnSegmentChanged;
        }

        private void OnSegmentChanged(TimeSegment segment) => Apply(segment, instant: false);

        private void Apply(TimeSegment segment, bool instant)
        {
            if (segment == null || segment.Lighting == null) return;
            var preset = segment.Lighting;

            if (fade != null) StopCoroutine(fade);

            if (instant)
            {
                globalLight.color = preset.GlobalColor;
                globalLight.intensity = preset.GlobalIntensity;
            }
            else
            {
                fade = StartCoroutine(Fade(preset.GlobalColor, preset.GlobalIntensity));
            }
        }

        private IEnumerator Fade(Color toColor, float toIntensity)
        {
            Color fromColor = globalLight.color;
            float fromIntensity = globalLight.intensity;

            for (float t = 0f; t < 1f; t += Time.deltaTime / fadeDuration)
            {
                globalLight.color = Color.Lerp(fromColor, toColor, t);
                globalLight.intensity = Mathf.Lerp(fromIntensity, toIntensity, t);
                yield return null; // wait one frame
            }

            globalLight.color = toColor;
            globalLight.intensity = toIntensity;
        }
    }
}