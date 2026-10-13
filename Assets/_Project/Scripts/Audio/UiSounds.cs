using System;
using UnityEngine;

namespace QuietWitness.Audio
{
    // The small sounds of the game, in one file: Assets/_Project/Resources/UiSounds.asset.
    // Swap a clip or change its volume there; empty clip = silent.
    [CreateAssetMenu(fileName = "UiSounds", menuName = "The Quiet Witness/UI Sounds")]
    public class UiSounds : ScriptableObject
    {
        [Serializable]
        public class Sound
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.5f;
        }

        [Tooltip("E / Space / a click that does something: an action on a hotspot, a prompt, a menu line, the dot on a thing.")]
        public Sound click = new Sound();
        [Tooltip("Not played at the moment (the dot on a thing plays Click).")]
        public Sound thought = new Sound();
        [Tooltip("Turning the things in the inventory.")]
        public Sound page = new Sound();
        [Tooltip("The envelope tears itself open.")]
        public Sound envelopeTear = new Sound();
        [Tooltip("The things fly out of the envelope.")]
        public Sound envelopeItems = new Sound();

        public const string ResourceName = "UiSounds";
        private static UiSounds loaded;
        private static int lastFrame = -1;
        private static AudioClip lastClip;

        public static UiSounds Get()
        {
            if (loaded == null) loaded = Resources.Load<UiSounds>(ResourceName);
            return loaded;
        }

        // UiSounds.Play(s => s.click)
        public static void Play(Func<UiSounds, Sound> pick)
        {
            var all = Get();
            if (all == null || pick == null) return;
            var sound = pick(all);
            if (sound == null || sound.clip == null) return;
            // The same sound twice in one frame (two scripts reacting to one press) plays once.
            if (Time.frameCount == lastFrame && lastClip == sound.clip) return;
            lastFrame = Time.frameCount;
            lastClip = sound.clip;
            Sfx.Play(sound.clip, sound.volume);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            loaded = null;
            lastFrame = -1;
            lastClip = null;
        }
    }
}
