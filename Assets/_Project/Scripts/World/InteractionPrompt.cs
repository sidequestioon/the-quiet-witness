using System.Collections.Generic;
using QuietWitness.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuietWitness.World
{
    // Backbone-style prompt over the hotspot E would use: a black round icon per action
    // (eye, hand, footsteps...) with the action's name above it.
    // It pops in when the hero walks up, and fades out when he walks away.
    // Several actions: icons in a row, W/S or arrows (or the mouse) pick one, its name is shown.
    // E or a click does it. Drawn under the hero.
    public class InteractionPrompt : MonoBehaviour
    {
        [Tooltip("One icon per ActionKind, in its order: Look, Take, Use, Open, Talk, Leave, UseItem. White.")]
        [SerializeField] private Sprite[] icons = new Sprite[7];
        [Tooltip("The same, yellow: for story-important hotspots.")]
        [SerializeField] private Sprite[] storyIcons = new Sprite[7];
        [SerializeField] private TMP_FontAsset font;
        [Tooltip("Unlit, so evening and night lighting don't darken the icon.")]
        [SerializeField] private Material iconMaterial;
        [Header("Size and place (can be tuned in Play mode and seen at once)")]
        [Tooltip("1 = the icon as drawn (13 px). Below 1 is smaller.")]
        [SerializeField, Range(0.3f, 1.5f)] private float iconSize = 1f;
        [Tooltip("Pixels to move every prompt up (minus = down).")]
        [SerializeField] private int raise = 0;
        [SerializeField] private float fontSize = 2f;
        [SerializeField] private int iconSpacing = 16;   // px between icon centres
        [SerializeField] private int textGap = 2;        // px between icon top and text
        [Header("Other things nearby")]
        [Tooltip("Things this close to the hero (pixels, left/right) show a small icon without a name.")]
        [SerializeField] private int nearbyDistance = 70;
        [Tooltip("Size of those small icons, compared with the full one.")]
        [SerializeField, Range(0.2f, 1f)] private float nearbySize = 0.6f;
        [SerializeField, Range(0f, 1f)] private float nearbyOpacity = 0.75f;
        [Header("Look")]
        [Tooltip("Names of ordinary hotspots.")]
        [SerializeField] private Color plainTextColor = new Color(0.96f, 0.94f, 0.91f);
        [Tooltip("Names of story-important hotspots.")]
        [SerializeField] private Color storyTextColor = new Color(0.94f, 0.85f, 0.64f);
        [Tooltip("The hero is 10: lower numbers are drawn behind him.")]
        [SerializeField] private int sortingOrder = 5;

        public static InteractionPrompt Instance { get; private set; }
        public TMP_FontAsset Font => font;

        private const float PixelsPerUnit = 36f;
        private const float ShowTime = 0.22f;
        private const float HideTime = 0.12f;
        private const float HitRadius = 7f / PixelsPerUnit;   // scaled by iconSize

        private readonly List<SpriteRenderer> iconRenderers = new List<SpriteRenderer>();

        private class Minor
        {
            public SpriteRenderer renderer;
            public float appear;
            public bool wanted;
        }
        private readonly Dictionary<HotspotActions, Minor> minors = new Dictionary<HotspotActions, Minor>();
        private Transform hero;
        private TextMeshPro label;
        private HotspotActions target;
        private int selected;
        private bool visible;
        private float appear;      // 0 hidden .. 1 shown
        private float pop = 1f;    // 0 right after showing .. 1
        private float textIn = 1f; // 0 right after the name changed .. 1

        private void Awake()
        {
            Instance = this;
            var go = new GameObject("Label");
            go.transform.SetParent(transform, false);
            label = go.AddComponent<TextMeshPro>();
            if (font != null) label.font = font;
            label.fontSize = fontSize;
            label.color = plainTextColor;
            label.alignment = TextAlignmentOptions.Bottom;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.pivot = new Vector2(0.5f, 0f);
            label.rectTransform.sizeDelta = new Vector2(8f, 1f);
            label.sortingOrder = sortingOrder + 1;
            // A soft dark shadow so the name reads on busy backgrounds.
            var mat = new Material(label.fontSharedMaterial);
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.8f));
            mat.SetFloat("_UnderlaySoftness", 0.5f);
            mat.SetFloat("_UnderlayDilate", 0.4f);
            label.fontSharedMaterial = mat;
            label.text = "";
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(HotspotActions actions)
        {
            if (actions == null || actions.Actions.Count == 0) { Hide(); return; }
            if (actions == target && visible) return;
            target = actions;
            selected = 0;
            visible = true;
            appear = 0f;
            pop = 0f;
            textIn = 0f;
            Build();
        }

        public void Hide() => visible = false;

        // E: does the chosen action. False if nothing is shown.
        public bool Execute()
        {
            if (!visible || target == null || appear < 0.3f) return false;
            target.Choose(selected);
            return true;
        }

        private void Build()
        {
            int count = target.Actions.Count;
            while (iconRenderers.Count < count)
            {
                var go = new GameObject("Icon");
                go.transform.SetParent(transform, false);
                var r = go.AddComponent<SpriteRenderer>();
                if (iconMaterial != null) r.sharedMaterial = iconMaterial;
                r.sortingOrder = sortingOrder;
                iconRenderers.Add(r);
            }
            for (int i = 0; i < iconRenderers.Count; i++)
            {
                bool used = i < count;
                iconRenderers[i].enabled = used;
                if (used)
                {
                    int kind = (int)target.Actions[i].kind;
                    var set = target.IsStory && storyIcons != null && kind < storyIcons.Length && storyIcons[kind] != null ? storyIcons : icons;
                    iconRenderers[i].sprite = kind < set.Length ? set[kind] : null;
                }
            }
            label.text = target.Actions[selected].Label;
        }

        private void Select(int index)
        {
            if (index == selected) return;
            selected = index;
            label.text = target.Actions[selected].Label;
            textIn = 0f;
        }

        private void Update()
        {
            if (target == null) return;
            if (visible && !InputBlocker.IsBlocked) HandleInput();

            appear = Mathf.MoveTowards(appear, visible ? 1f : 0f, Time.deltaTime / (visible ? ShowTime : HideTime));
            pop = Mathf.MoveTowards(pop, 1f, Time.deltaTime / ShowTime);
            textIn = Mathf.MoveTowards(textIn, 1f, Time.deltaTime / ShowTime);

            bool any = visible || appear > 0f;
            label.enabled = any;
            int count = target.Actions.Count;
            for (int i = 0; i < count; i++) iconRenderers[i].enabled = any;
            if (!any) return;

            // Icons in a row, centred on the hotspot's prompt point, on the pixel grid.
            Vector2 point = target.PromptPoint;
            int cx = Mathf.RoundToInt(point.x * PixelsPerUnit);
            int cy = Mathf.RoundToInt(point.y * PixelsPerUnit) + raise;
            label.fontSize = fontSize;
            float iconScale = visible ? Mathf.LerpUnclamped(0.4f, 1f, EaseOutBack(pop)) : Mathf.Lerp(0.7f, 1f, appear);
            float alpha = visible ? Mathf.Clamp01(appear * 1.6f) : appear;
            int iconHeight = 13;
            for (int i = 0; i < count; i++)
            {
                var r = iconRenderers[i];
                int size = r.sprite != null ? Mathf.RoundToInt(r.sprite.rect.width) : 13;
                iconHeight = Mathf.RoundToInt(size * iconSize);
                int x = cx + Mathf.RoundToInt((i - (count - 1) / 2f) * iconSpacing * iconSize);
                int cornerX = x - (size - 1) / 2, cornerY = cy - (size - 1) / 2;
                r.transform.position = new Vector3((cornerX + size / 2f) / PixelsPerUnit, (cornerY + size / 2f) / PixelsPerUnit, 0f);
                bool on = i == selected;
                float s = iconScale * iconSize * (on || count == 1 ? 1f : 0.85f);
                r.transform.localScale = new Vector3(s, s, 1f);
                var c = r.color; c.a = alpha * (on || count == 1 ? 1f : 0.55f); r.color = c;
            }

            // The name above, sliding up a little as it fades in.
            float slide = (1f - EaseOut(textIn)) * 3f;
            float textY = (cy + (iconHeight + 1) / 2 + textGap - slide) / PixelsPerUnit;
            float textX = (cx + Mathf.RoundToInt((selected - (count - 1) / 2f) * iconSpacing * iconSize) + 0.5f) / PixelsPerUnit;
            label.transform.position = new Vector3(textX, textY, 0f);
            var tc = target.IsStory ? storyTextColor : plainTextColor;
            tc.a *= alpha * Mathf.Clamp01(textIn * 1.5f);
            label.color = tc;
        }

        private void HandleInput()
        {
            int count = target.Actions.Count;
            var kb = Keyboard.current;
            if (kb != null && count > 1)
            {
                if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) Select((selected - 1 + count) % count);
                if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) Select((selected + 1) % count);
            }

            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return;
            Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            for (int i = 0; i < count; i++)
            {
                float hit = HitRadius * iconSize;
                if (((Vector2)iconRenderers[i].transform.position - world).sqrMagnitude > hit * hit) continue;
                if (mouse.delta.ReadValue().sqrMagnitude > 0f) Select(i);
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    Select(i);
                    target.Choose(selected);
                }
                break;
            }
        }

        // Small, dimmer icons over the other things near the hero (no name), so you can tell
        // there is more to do around here. The closest one gets the full prompt instead.
        private void LateUpdate()
        {
            if (hero == null)
            {
                var mover = FindFirstObjectByType<PlayerMover>();
                if (mover != null) hero = mover.transform;
            }

            foreach (var m in minors.Values) m.wanted = false;
            if (hero != null && !InputBlocker.IsBlocked)
            {
                foreach (var spot in HotspotActions.All)
                {
                    if (spot == null || spot.Actions.Count == 0) continue;
                    if (visible && spot == target) continue;
                    if (Mathf.Abs(spot.PromptPoint.x - hero.position.x) * PixelsPerUnit > nearbyDistance) continue;
                    if (!minors.TryGetValue(spot, out var m))
                    {
                        var go = new GameObject("Nearby");
                        go.transform.SetParent(transform, false);
                        var r = go.AddComponent<SpriteRenderer>();
                        if (iconMaterial != null) r.sharedMaterial = iconMaterial;
                        r.sortingOrder = sortingOrder;
                        m = new Minor { renderer = r };
                        minors[spot] = m;
                    }
                    m.wanted = true;
                }
            }

            foreach (var pair in minors)
            {
                var spot = pair.Key;
                var m = pair.Value;
                m.appear = Mathf.MoveTowards(m.appear, m.wanted ? 1f : 0f, Time.deltaTime / (m.wanted ? ShowTime : HideTime));
                m.renderer.enabled = m.appear > 0f && spot != null;
                if (!m.renderer.enabled) continue;

                int kind = (int)spot.Actions[0].kind;
                var set = spot.IsStory && storyIcons != null && kind < storyIcons.Length && storyIcons[kind] != null ? storyIcons : icons;
                m.renderer.sprite = kind < set.Length ? set[kind] : null;

                Vector2 point = spot.PromptPoint;
                int cx = Mathf.RoundToInt(point.x * PixelsPerUnit);
                int cy = Mathf.RoundToInt(point.y * PixelsPerUnit) + raise;
                int size = m.renderer.sprite != null ? Mathf.RoundToInt(m.renderer.sprite.rect.width) : 13;
                int cornerX = cx - (size - 1) / 2, cornerY = cy - (size - 1) / 2;
                m.renderer.transform.position = new Vector3((cornerX + size / 2f) / PixelsPerUnit, (cornerY + size / 2f) / PixelsPerUnit, 0f);
                float e = EaseOut(m.appear);
                float scale = iconSize * nearbySize * Mathf.Lerp(0.5f, 1f, e);
                m.renderer.transform.localScale = new Vector3(scale, scale, 1f);
                var c = m.renderer.color; c.a = nearbyOpacity * e; m.renderer.color = c;
            }
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
