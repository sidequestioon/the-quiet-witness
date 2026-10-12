using UnityEngine;

namespace QuietWitness.World
{
    // Plays a list of sprites in a loop: a ceiling fan, fish in a tank, a blinking sign.
    // Filled in by Setup > Location backgrounds from Art/Locations/<Scene>/Anim/<Name>/.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlipbook : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(0.01f)] private float frameTime = 0.1f;
        [Tooltip("Off: plays once and stays on the last frame (e.g. an envelope sliding under a door).")]
        [SerializeField] private bool loop = true;
        [Tooltip("Off: waits for Play() (called by a script).")]
        [SerializeField] private bool playOnAwake = true;

        private SpriteRenderer spriteRenderer;
        private float timer;
        private int index;
        private bool playing;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            playing = playOnAwake;
            Show();
        }

        private void Update()
        {
            if (!playing || frames == null || frames.Length < 2) return;
            timer += Time.deltaTime;
            while (timer >= frameTime)
            {
                timer -= frameTime;
                if (!loop && index == frames.Length - 1)
                {
                    playing = false;
                    return;
                }
                index = (index + 1) % frames.Length;
                Show();
            }
        }

        // From the first frame.
        public void Play()
        {
            index = 0;
            timer = 0f;
            playing = true;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            Show();
        }

        // Jump to the last frame and stop (e.g. the envelope already lies on the floor).
        public void ShowLast()
        {
            if (frames == null || frames.Length == 0) return;
            index = frames.Length - 1;
            playing = false;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            Show();
        }

        private void Show()
        {
            if (frames != null && frames.Length > 0) spriteRenderer.sprite = frames[index];
        }
    }
}
