using System.Collections;
using UnityEngine;

namespace QuietWitness.Audio
{
    // A sound that repeats: a wall clock (tick, tock, tick... every second), a beeping machine.
    // The clips are played one after another in a circle; the pause is random between Min and Max.
    [RequireComponent(typeof(AudioSource))]
    public class IntervalSound : MonoBehaviour
    {
        [SerializeField] private AudioClip[] clips;
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.15f;
        [Tooltip("Seconds between sounds (from - to). Same numbers = a steady rhythm.")]
        [SerializeField] private Vector2 interval = new Vector2(1f, 1f);
        [Tooltip("Small random change of pitch, so a repeated sound is not mechanical. 0 = always the same.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float pitchVariation = 0f;

        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        private void OnEnable() => StartCoroutine(Run());

        private IEnumerator Run()
        {
            if (clips == null || clips.Length == 0) yield break;
            int i = 0;
            yield return new WaitForSeconds(Random.Range(0f, interval.y));
            while (true)
            {
                var clip = clips[i++ % clips.Length];
                source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
                if (clip != null) source.PlayOneShot(clip, volume);
                yield return new WaitForSeconds(Random.Range(interval.x, interval.y));
            }
        }
    }
}
