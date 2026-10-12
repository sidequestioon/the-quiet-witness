using QuietWitness.World;
using UnityEngine;

namespace QuietWitness.Audio
{
    // The hero's steps in this location: wood in the office, tiles in the darkroom.
    // One per scene; it listens to the hero (PlayerMover) and plays a step every Step Time while he walks.
    [RequireComponent(typeof(AudioSource))]
    public class FootstepSounds : MonoBehaviour
    {
        [SerializeField] private AudioClip[] steps;
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.35f;
        [Tooltip("Seconds between two steps.")]
        [SerializeField] private float stepTime = 0.4f;
        [Range(0f, 0.3f)]
        [SerializeField] private float pitchVariation = 0.1f;

        private AudioSource source;
        private PlayerMover hero;
        private float timer;
        private int last = -1;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        private void Start() => hero = Object.FindFirstObjectByType<PlayerMover>();

        private void Update()
        {
            if (hero == null || steps == null || steps.Length == 0) return;
            if (!hero.IsMoving)
            {
                timer = stepTime * 0.5f;          // the first step comes quickly after he starts
                return;
            }
            timer += Time.deltaTime;
            if (timer < stepTime) return;
            timer = 0f;

            int i = steps.Length == 1 ? 0 : Random.Range(0, steps.Length);
            if (i == last && steps.Length > 1) i = (i + 1) % steps.Length;
            last = i;
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            source.PlayOneShot(steps[i], volume * Random.Range(0.85f, 1f));
        }
    }
}
