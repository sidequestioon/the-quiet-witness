using System;
using System.Collections;
using System.Collections.Generic;
using QuietWitness.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace QuietWitness.World
{
    // A light whose colour and strength follow the time of day: light from the window,
    // a street lamp outside. One row per lighting preset (Morning, Day, Evening, Night);
    // a preset without a row, or with Intensity 0, turns the light off.
    // F2 in Play mode (editor) previews every preset, like the global light.
    [RequireComponent(typeof(Light2D))]
    public class LightByPreset : MonoBehaviour
    {
        [Serializable]
        public class Mood
        {
            public LightingPreset preset;
            public Color color = Color.white;
            [Range(0f, 3f)] public float intensity = 1f;
        }

        [SerializeField] private List<Mood> moods = new List<Mood>();
        [SerializeField] private float fadeDuration = 1.5f;

        private Light2D light2D;
        private Coroutine fade;

        private void Awake() => light2D = GetComponent<Light2D>();

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SegmentChanged += OnSegmentChanged;
                OnSegmentChanged(TimeManager.Instance.Current, instant: true);
            }
#if UNITY_EDITOR
            GlobalLightController.PreviewChanged += OnPreview;
#endif
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.SegmentChanged -= OnSegmentChanged;
#if UNITY_EDITOR
            GlobalLightController.PreviewChanged -= OnPreview;
#endif
        }

#if UNITY_EDITOR
        private void OnPreview(LightingPreset preset) => Apply(preset, instant: true);
#endif

        private void OnSegmentChanged(TimeSegment segment) => OnSegmentChanged(segment, instant: false);

        private void OnSegmentChanged(TimeSegment segment, bool instant)
        {
            if (segment != null) Apply(segment.Lighting, instant);
        }

        private void Apply(LightingPreset preset, bool instant)
        {
            if (preset == null) return;
            var mood = moods.Find(m => m.preset == preset);
            Color color = mood != null ? mood.color : light2D.color;
            float intensity = mood != null ? mood.intensity : 0f;

            if (fade != null) StopCoroutine(fade);
            if (instant || !isActiveAndEnabled) Set(color, intensity);
            else fade = StartCoroutine(Fade(color, intensity));
        }

        private void Set(Color color, float intensity)
        {
            light2D.color = color;
            light2D.intensity = intensity;
            light2D.enabled = intensity > 0.001f;
        }

        private IEnumerator Fade(Color toColor, float toIntensity)
        {
            Color fromColor = light2D.color;
            float fromIntensity = light2D.enabled ? light2D.intensity : 0f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / fadeDuration)
            {
                Set(Color.Lerp(fromColor, toColor, t), Mathf.Lerp(fromIntensity, toIntensity, t));
                yield return null;
            }
            Set(toColor, toIntensity);
        }
    }
}
