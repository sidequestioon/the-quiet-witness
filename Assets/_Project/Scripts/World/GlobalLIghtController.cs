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

#if UNITY_EDITOR
        // Editor only: F2 cycles Day → Morning → Evening → Night, to see a location in every light.
        // The story's real time is not changed.
        public static event System.Action<LightingPreset> PreviewChanged;
        private LightingPreset[] previewPresets;
        private int previewIndex = -1;

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null || !keyboard.f2Key.wasPressedThisFrame) return;

            if (previewPresets == null)
            {
                string[] order = { "Day", "Morning", "Evening", "Night" };
                var all = new System.Collections.Generic.List<LightingPreset>();
                foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:LightingPreset"))
                    all.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<LightingPreset>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)));
                all.Sort((a, b) => Rank(a).CompareTo(Rank(b)));
                previewPresets = all.ToArray();

                int Rank(LightingPreset p)
                {
                    int i = System.Array.IndexOf(order, p.name);
                    return i < 0 ? order.Length : i;
                }
            }
            if (previewPresets.Length == 0) return;

            previewIndex = (previewIndex + 1) % previewPresets.Length;
            var preset = previewPresets[previewIndex];
            if (fade != null) StopCoroutine(fade);
            globalLight.color = preset.GlobalColor;
            globalLight.intensity = preset.GlobalIntensity;
            PreviewChanged?.Invoke(preset);
            Debug.Log($"[Light preview] {preset.name} (F2 for the next one)");
        }
#endif

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