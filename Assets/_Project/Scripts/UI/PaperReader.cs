using System.Collections.Generic;
using QuietWitness.Audio;
using QuietWitness.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace QuietWitness.UI
{
    // A paper to read (a newspaper from the cork board): it comes up over everything, the screen dark behind it.
    // The arrows at its sides (or the Left / Right keys, A / D) turn the pages; the little squares under it
    // show which page it is. Esc or a click outside the paper closes it.
    // Open with PaperReader.Instance.Show(pages). Built by Setup > Map: the Gazette to read.
    public class PaperReader : MonoBehaviour
    {
        public static PaperReader Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.open;
        // The frame it closed in: the Esc that closed it must not close the map under it too.
        public static int ClosedFrame { get; private set; } = -1;

        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image page;
        [SerializeField] private Image pageShadow;
        [Tooltip("How big a page is: 2 = each pixel of the picture is 2x2.")]
        [Range(1, 4)]
        [SerializeField] private int pageScale = 2;
        [SerializeField] private Image prevArrow;
        [SerializeField] private Image nextArrow;
        [Tooltip("Gap between the paper and each arrow (canvas units).")]
        [SerializeField] private float arrowGap = 60f;
        [SerializeField] private RectTransform pageDots;
        [Tooltip("One little square; copied for each page.")]
        [SerializeField] private Image dotTemplate;
        [SerializeField] private Color arrowColor = new Color(0.93f, 0.88f, 0.77f, 1f);
        [SerializeField] private Color arrowHover = Color.white;
        [Range(0f, 1f)]
        [SerializeField] private float offAlpha = 0.2f;
        [SerializeField] private float fadeTime = 0.2f;
        [Tooltip("How far the paper rises as it comes up (canvas units).")]
        [SerializeField] private float rise = 16f;

        private Sprite[] pages;
        private int index;
        private bool open;
        private float shown;
        private int openedFrame = -1;
        private Vector2 home;
        private float nudge;          // the page moves a little to the side as it turns, then settles
        private readonly List<Image> dots = new List<Image>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            ClosedFrame = -1;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { enabled = false; return; }
            Instance = this;
            if (page != null) home = page.rectTransform.anchoredPosition;
            if (dotTemplate != null) dotTemplate.gameObject.SetActive(false);
            SetShown(0f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(Sprite[] newPages)
        {
            if (newPages == null || newPages.Length == 0 || open) return;
            pages = newPages;
            index = 0;
            open = true;
            openedFrame = Time.frameCount;
            InputBlocker.Push();
            BuildDots();
            ShowPage(0);
            UiSounds.Play(s => s.page);
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            ClosedFrame = Time.frameCount;
            InputBlocker.Pop();
            UiSounds.Play(s => s.page);
        }

        private void Turn(int dir)
        {
            int next = Mathf.Clamp(index + dir, 0, pages.Length - 1);
            if (next == index) return;
            ShowPage(next);
            nudge = 8f * dir;
            UiSounds.Play(s => s.page);
        }

        private void ShowPage(int i)
        {
            index = i;
            var sprite = pages[i];
            Vector2 size = sprite.rect.size * pageScale;
            foreach (var img in new[] { page, pageShadow })
            {
                if (img == null) continue;
                img.sprite = sprite;
                img.rectTransform.sizeDelta = size;
            }
            float side = size.x / 2f + arrowGap;
            if (prevArrow != null) prevArrow.rectTransform.anchoredPosition = home + Vector2.left * side;
            if (nextArrow != null) nextArrow.rectTransform.anchoredPosition = home + Vector2.right * side;
            if (pageDots != null) pageDots.anchoredPosition = home + Vector2.down * (size.y / 2f + 26f);
            for (int k = 0; k < dots.Count; k++)
            {
                var c = arrowColor; c.a = k == i ? 1f : 0.3f;
                dots[k].color = c;
            }
        }

        private void BuildDots()
        {
            foreach (var d in dots) if (d != null) Destroy(d.gameObject);
            dots.Clear();
            if (dotTemplate == null || pageDots == null || pages.Length < 2) return;
            float step = 28f;
            for (int k = 0; k < pages.Length; k++)
            {
                var d = Instantiate(dotTemplate, pageDots);
                d.gameObject.SetActive(true);
                d.rectTransform.anchoredPosition = new Vector2((k - (pages.Length - 1) / 2f) * step, 0f);
                dots.Add(d);
            }
        }

        private void Update()
        {
            if (Instance != this) return;
            float dt = Time.unscaledDeltaTime;
            shown = Mathf.MoveTowards(shown, open ? 1f : 0f, dt / Mathf.Max(0.01f, fadeTime));
            SetShown(shown);
            if (!open) return;

            var keys = Keyboard.current;
            var mouse = Mouse.current;
            bool many = pages.Length > 1;
            Vector2 at = mouse != null ? mouse.position.ReadValue() : new Vector2(-1f, -1f);
            bool overPrev = many && Over(prevArrow, at);
            bool overNext = many && Over(nextArrow, at);
            Paint(prevArrow, index > 0, overPrev);
            Paint(nextArrow, index < pages.Length - 1, overNext);

            if (Time.frameCount == openedFrame) return;      // the click that opened it
            if (keys != null)
            {
                if (keys.escapeKey.wasPressedThisFrame) { Close(); return; }
                if (keys.leftArrowKey.wasPressedThisFrame || keys.aKey.wasPressedThisFrame) Turn(-1);
                if (keys.rightArrowKey.wasPressedThisFrame || keys.dKey.wasPressedThisFrame) Turn(1);
            }
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                if (overPrev) Turn(-1);
                else if (overNext) Turn(1);
                else if (!Over(page, at)) Close();               // a click outside the paper
            }
        }

        private void LateUpdate()
        {
            if (page == null) return;
            nudge = Mathf.MoveTowards(nudge, 0f, Time.unscaledDeltaTime * 80f);
            float ease = 1f - (1f - shown) * (1f - shown);
            Vector2 p = home + Vector2.down * rise * (1f - ease) + Vector2.right * nudge;
            // whole pixels of the page, so it never breaks apart
            p = new Vector2(Mathf.Round(p.x / pageScale) * pageScale, Mathf.Round(p.y / pageScale) * pageScale);
            page.rectTransform.anchoredPosition = p;
            if (pageShadow != null) pageShadow.rectTransform.anchoredPosition = p + new Vector2(10f, -10f);
        }

        private void Paint(Image arrow, bool can, bool hover)
        {
            if (arrow == null) return;
            bool show = pages != null && pages.Length > 1;
            if (arrow.gameObject.activeSelf != show) arrow.gameObject.SetActive(show);
            var c = can && hover ? arrowHover : arrowColor;
            c.a = can ? 1f : offAlpha;
            arrow.color = c;
        }

        private static bool Over(Graphic g, Vector2 screen)
        {
            if (g == null || !g.gameObject.activeInHierarchy) return false;
            var r = g.rectTransform;
            // a little bigger than the arrow itself, easy to hit
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(r, screen, null, out local)) return false;
            var rect = r.rect;
            rect.xMin -= 16f; rect.xMax += 16f; rect.yMin -= 16f; rect.yMax += 16f;
            return rect.Contains(local);
        }

        private void SetShown(float a)
        {
            if (group == null) return;
            group.alpha = a;
            bool on = open || a > 0f;
            group.blocksRaycasts = on;
            group.interactable = false;
        }
    }
}
