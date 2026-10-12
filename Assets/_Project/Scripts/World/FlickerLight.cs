using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace QuietWitness.World
{
    // A faulty sign or lamp: it burns for a while, then sputters (on / half / off very fast),
    // and now and then goes dark for a moment before it catches again.
    // Swaps the sprite (On / Dim / Off) and the strength of its Light 2D together.
    // Depth 1 = goes fully dark when it sputters; lower = it only dims (a lamp that "breathes").
    public class FlickerLight : MonoBehaviour
    {
        [Header("Look")]
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Sprite onSprite;
        [Tooltip("Half-lit. Optional: without it, On is used for small dips.")]
        [SerializeField] private Sprite dimSprite;
        [SerializeField] private Sprite offSprite;
        [Tooltip("Its glow (optional).")]
        [SerializeField] private Light2D glow;
        [SerializeField] private float glowIntensity = 0.8f;

        [Header("Rhythm (seconds, from - to)")]
        [Tooltip("Off: it just burns steadily.")]
        [SerializeField] private bool faulty = true;
        [Tooltip("How long it burns steadily between sputters.")]
        [SerializeField] private Vector2 steady = new Vector2(2f, 6f);
        [Tooltip("How long one sputter lasts.")]
        [SerializeField] private Vector2 sputter = new Vector2(0.2f, 1f);
        [Tooltip("One flash inside a sputter.")]
        [SerializeField] private Vector2 flash = new Vector2(0.03f, 0.12f);
        [Tooltip("Chance that after a sputter it goes dark for a moment.")]
        [Range(0f, 1f)]
        [SerializeField] private float blackoutChance = 0.3f;
        [SerializeField] private Vector2 blackout = new Vector2(0.5f, 2f);
        [Tooltip("1 = goes fully dark when it flickers. 0.5 = only dims by half.")]
        [Range(0f, 1f)]
        [SerializeField] private float depth = 1f;

        private enum State { On, Dim, Off }

        private void Reset()
        {
            target = GetComponent<SpriteRenderer>();
            glow = GetComponentInChildren<Light2D>();
        }

        private void OnEnable()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            Set(State.On);
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(steady.x, steady.y));
                if (!faulty) continue;

                float end = Time.time + Random.Range(sputter.x, sputter.y);
                while (Time.time < end)
                {
                    float r = Random.value;
                    Set(r < 0.45f ? State.Off : r < 0.75f ? State.Dim : State.On);
                    yield return Flash();
                }

                if (Random.value < blackoutChance)
                {
                    Set(State.Off);
                    yield return new WaitForSeconds(Random.Range(blackout.x, blackout.y));
                    for (int i = 0; i < Random.Range(2, 5); i++)   // it catches again
                    {
                        Set(State.Dim); yield return Flash();
                        Set(State.Off); yield return Flash();
                    }
                }
                Set(State.On);
            }
        }

        private WaitForSeconds Flash() => new WaitForSeconds(Random.Range(flash.x, flash.y));

        private void Set(State state)
        {
            float k = state == State.On ? 1f : state == State.Dim ? 1f - depth * 0.6f : 1f - depth;

            if (glow != null)
            {
                glow.intensity = glowIntensity * k;
                glow.enabled = k > 0.01f;
            }
            if (target == null) return;
            Sprite sprite = k > 0.8f ? onSprite
                          : k > 0.25f ? (dimSprite != null ? dimSprite : onSprite)
                          : (offSprite != null ? offSprite : dimSprite);
            if (sprite != null) target.sprite = sprite;
        }
    }
}
