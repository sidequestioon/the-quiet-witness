using System;
using System.Collections;
using System.Collections.Generic;
using QuietWitness.Core;
using UnityEngine;

namespace QuietWitness.World
{
    // A picture that shows only at some times of day: the evening sky, lit windows, a neon sign.
    // One row per lighting preset with how visible it is (0 = hidden). A preset without a row hides it.
    // Fades when time moves on; F2 in Play mode (editor) previews every preset.
    [RequireComponent(typeof(SpriteRenderer))]
    public class FadeByPreset : MonoBehaviour
    {
        [Serializable]
        public class Row
        {
            public LightingPreset preset;
            [Range(0f, 1f)] public float alpha = 1f;
        }

        [SerializeField] private List<Row> rows = new List<Row>();
        [SerializeField] private float fadeDuration = 1.5f;

        private SpriteRenderer sprite;
        private Coroutine fade;

        private void Awake() => sprite = GetComponent<SpriteRenderer>();

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SegmentChanged += OnSegmentChanged;
                if (TimeManager.Instance.Current != null) Apply(TimeManager.Instance.Current.Lighting, true);
            }
#if UNITY_EDITOR
            GlobalLightController.PreviewChanged += OnPreview;
#endif
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.SegmentChanged -= OnSegmentChanged;
#if UNITY_EDITOR
            GlobalLightController.PreviewChanged -= OnPreview;
#endif
        }

#if UNITY_EDITOR
        private void OnPreview(LightingPreset preset) => Apply(preset, true);
#endif

        private void OnSegmentChanged(TimeSegment segment)
        {
            if (segment != null) Apply(segment.Lighting, false);
        }

        private void Apply(LightingPreset preset, bool instant)
        {
            if (preset == null) return;
            var row = rows.Find(r => r.preset == preset);
            float alpha = row != null ? row.alpha : 0f;
            if (fade != null) StopCoroutine(fade);
            if (instant || !isActiveAndEnabled) Set(alpha);
            else fade = StartCoroutine(Fade(alpha));
        }

        private void Set(float alpha)
        {
            var c = sprite.color;
            c.a = alpha;
            sprite.color = c;
            sprite.enabled = alpha > 0.001f;
        }

        private IEnumerator Fade(float to)
        {
            float from = sprite.enabled ? sprite.color.a : 0f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / fadeDuration)
            {
                Set(Mathf.Lerp(from, to, t));
                yield return null;
            }
            Set(to);
        }
    }
}
