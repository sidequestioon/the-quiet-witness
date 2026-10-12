using UnityEngine;

namespace QuietWitness.Audio
{
    // One-shot sounds that are not tied to a place on screen: the map, a knock, a lock.
    // Made on first use and kept between scenes.
    public static class Sfx
    {
        private static AudioSource source;

        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            if (source == null)
            {
                var go = new GameObject("Sfx");
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
            }
            source.PlayOneShot(clip, volume);
        }
    }
}
