using System.Collections.Generic;
using UnityEngine;

namespace QuietWitness.World
{
    // The dot over a hotspot: while the hero is in reach it rises a few pixels from the object,
    // popping in and fading in. Black with a white ring and a dark outline, so it shows on light
    // and dark backgrounds alike. The hotspot E would open gets a bigger dot with a glow.
    // E or a click on the dot opens its list of actions (ActionMenu). Drawn under the hero.
    // PlayerInteractor tells it when; Setup > Office hotspots fills it in.
    [RequireComponent(typeof(Interactable))]
    public class InteractionIcon : MonoBehaviour
    {
        [SerializeField] private Sprite dot;
        [SerializeField] private Sprite dotHighlighted;
        [Tooltip("The point on the object the dot belongs to, relative to this hotspot (world units).")]
        [SerializeField] private Vector2 offset;
        [Tooltip("How many pixels above that point the dot floats.")]
        [SerializeField] private int lift = 6;
        [Tooltip("Unlit, so evening and night lighting don't darken the dot.")]
        [SerializeField] private Material material;
        [Tooltip("The hero is 10: lower numbers are drawn behind him.")]
        [SerializeField] private int sortingOrder = 5;

        private const float ShowTime = 0.22f;
        private const float HideTime = 0.14f;
        private const float StartScale = 0.3f;
        private const float PixelsPerUnit = 36f;
        private const float ClickRadius = 6f / PixelsPerUnit;

        private static readonly List<InteractionIcon> shown = new List<InteractionIcon>();
        public static IReadOnlyList<InteractionIcon> Shown => shown;

        private SpriteRenderer dotRenderer;
        private bool visible;
        private bool highlighted;
        private float appear;    // 0 hidden .. 1 fully shown
        private float pop = 1f;  // 0 right after it appears or gets highlighted .. 1
        private Vector2 dotCentre;

        public Material Material => material;
        public Vector2 Centre => dotCentre;
        public float HalfHeight => dotRenderer != null && dotRenderer.sprite != null
            ? dotRenderer.sprite.rect.height / (2f * PixelsPerUnit) : 0f;

        private void Awake()
        {
            var go = new GameObject("Dot");
            go.transform.SetParent(transform, false);
            dotRenderer = go.AddComponent<SpriteRenderer>();
            if (material != null) dotRenderer.sharedMaterial = material;
            dotRenderer.sortingOrder = sortingOrder;
            dotRenderer.enabled = false;
            dotCentre = (Vector2)transform.position + offset + new Vector2(0f, lift / PixelsPerUnit);
            ChooseSprite();
        }

        private void OnDisable() => shown.Remove(this);

        public void SetVisible(bool value)
        {
            if (value && !visible) pop = 0f;
            visible = value;
            if (value)
            {
                dotRenderer.enabled = true;
                if (!shown.Contains(this)) shown.Add(this);
            }
            else
            {
                highlighted = false;
                shown.Remove(this);
            }
            ChooseSprite();
        }

        public void SetHighlighted(bool value)
        {
            if (value && !highlighted) pop = 0f;
            highlighted = value;
            ChooseSprite();
        }

        public bool Contains(Vector2 world) => visible && (world - dotCentre).sqrMagnitude <= ClickRadius * ClickRadius;

        private void ChooseSprite()
        {
            if (dotRenderer == null) return;
            dotRenderer.sprite = highlighted && dotHighlighted != null ? dotHighlighted : dot;
        }

        private void Update()
        {
            float speed = 1f / (visible ? ShowTime : HideTime);
            appear = Mathf.MoveTowards(appear, visible ? 1f : 0f, speed * Time.deltaTime);
            pop = Mathf.MoveTowards(pop, 1f, Time.deltaTime / ShowTime);

            if (!visible && appear <= 0f)
            {
                dotRenderer.enabled = false;
                return;
            }

            // Everything in whole pixels so it stays crisp.
            Vector2 anchor = (Vector2)transform.position + offset;
            int ax = Mathf.RoundToInt(anchor.x * PixelsPerUnit);
            int ay = Mathf.RoundToInt(anchor.y * PixelsPerUnit);
            float rise = 1f - (1f - appear) * (1f - appear) * (1f - appear);   // ease out
            int liftNow = Mathf.RoundToInt(lift * rise);

            var sprite = dotRenderer.sprite;
            int w = sprite != null ? Mathf.RoundToInt(sprite.rect.width) : 1;
            int h = sprite != null ? Mathf.RoundToInt(sprite.rect.height) : 1;
            int cornerX = ax - (w - 1) / 2;
            int cornerY = ay + liftNow - (h - 1) / 2;
            dotCentre = new Vector2(cornerX + w / 2f, cornerY + h / 2f) / PixelsPerUnit;
            dotRenderer.transform.position = new Vector3(dotCentre.x, dotCentre.y, transform.position.z);

            float scale = visible ? Mathf.LerpUnclamped(StartScale, 1f, EaseOutBack(pop)) : Mathf.Lerp(0.6f, 1f, appear);
            dotRenderer.transform.localScale = new Vector3(scale, scale, 1f);

            var c = dotRenderer.color;
            c.a = visible ? Mathf.Clamp01(appear * 1.6f) : appear;
            dotRenderer.color = c;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 anchor = (Vector2)transform.position + offset;
            Gizmos.color = new Color(1f, 0.35f, 0.3f);
            Gizmos.DrawWireSphere(anchor + new Vector2(0f, lift / PixelsPerUnit), 3.5f / PixelsPerUnit);
        }
    }
}
