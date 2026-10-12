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

        private SpriteRenderer spriteRenderer;
        private float timer;
        private int index;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            Show();
        }

        private void Update()
        {
            if (frames == null || frames.Length < 2) return;
            timer += Time.deltaTime;
            while (timer >= frameTime)
            {
                timer -= frameTime;
                index = (index + 1) % frames.Length;
                Show();
            }
        }

        private void Show()
        {
            if (frames != null && frames.Length > 0) spriteRenderer.sprite = frames[index];
        }
    }
}
