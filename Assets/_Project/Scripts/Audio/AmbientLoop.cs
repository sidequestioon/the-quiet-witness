using System.Collections;
using UnityEngine;

namespace QuietWitness.Audio
{
    // A quiet background loop for one location (rain and wind in the office). Fades in when the scene starts.
    [RequireComponent(typeof(AudioSource))]
    public class AmbientLoop : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.35f;
        [SerializeField] private float fadeIn = 1.5f;

        private AudioSource source;
        private bool fading;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        private void OnEnable() => StartCoroutine(FadeIn());

        private IEnumerator FadeIn()
        {
            if (clip == null) yield break;
            fading = true;
            source.volume = 0f;
            source.Play();
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeIn))
            {
                source.volume = volume * t;
                yield return null;
            }
            source.volume = volume;
            fading = false;
        }

        // Volume can be tuned while playing.
        private void Update()
        {
            if (!fading) source.volume = volume;
        }
    }
}
