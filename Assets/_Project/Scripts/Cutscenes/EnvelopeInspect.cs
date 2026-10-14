using System.Collections;
using QuietWitness.Audio;
using QuietWitness.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace QuietWitness.Cutscenes
{
    // Close-up of the envelope after it is picked up:
    // the room blurs and darkens - the envelope comes up - [Open] it tears itself open -
    // [Take out] the things inside fly out and lie on the screen. Then they can be dragged around,
    // and each has a little dot: click it to hear what the hero thinks - [Close] back to the room.
    // E / Space / Enter or a click on the prompt goes on; Esc also closes at the end.
    // The plate with the hero's words is the object "Caption" (plate picture + Tail) with "Text" in it:
    // switch Caption on in the Hierarchy to see it outside Play mode and style it there - font, size,
    // colour on the Text; colour and picture on the plate and the tail. It hides itself when the game starts.
    [ExecuteAlways]
    public class EnvelopeInspect : MonoBehaviour
    {
        [Header("Parts")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Volume blur;
        [Tooltip("Black over the blurred room.")]
        [SerializeField] private Image dim;
        [Range(0f, 1f)]
        [SerializeField] private float dimAlpha = 0.6f;
        [SerializeField] private Image envelope;
        [Tooltip("Closed, tearing, open, things peeking out.")]
        [SerializeField] private Sprite[] tearFrames;
        [Tooltip("The open, empty envelope (after the things are taken out).")]
        [SerializeField] private Sprite emptyEnvelope;
        [SerializeField] private InspectItem[] items;

        [Header("Where the things land (canvas units, from the centre)")]
        [SerializeField] private Vector2[] itemTargets;
        [SerializeField] private float[] itemAngles;
        [SerializeField] private Vector2 envelopeAside = new Vector2(-560f, -40f);
        [SerializeField] private float envelopeAsideAngle = 6f;
        [SerializeField] private float envelopeAsideScale = 0.85f;

        [Header("Prompt (like the one in the room: icon, name above)")]
        [SerializeField] private RectTransform prompt;
        [SerializeField] private Image promptIcon;
        [SerializeField] private TMP_Text promptLabel;
        [SerializeField] private Sprite handIcon;
        [SerializeField] private Sprite closeIcon;

        [Header("What he says about a thing: the plate's size rules (looks: on Caption and its Text)")]
        [SerializeField] private RectTransform caption;
        [SerializeField] private Image captionPlate;
        [SerializeField] private Image captionTail;
        [SerializeField] private TMP_Text captionText;
        [Tooltip("The plate never gets wider than this (canvas units); longer text wraps into an even block.")]
        [SerializeField] private float maxWidth = 560f;
        [Tooltip("The plate is never narrower than this.")]
        [SerializeField] private float minWidth = 0f;
        [Tooltip("Plate around the text: x = left/right, y = top/bottom.")]
        [SerializeField] private Vector2 platePadding = new Vector2(24f, 16f);
        [Tooltip("Space between the thing and the plate's tail.")]
        [SerializeField] private float gapToItem = 16f;
        [SerializeField] private float lettersPerSecond = 45f;

        [Header("Timing")]
        [SerializeField] private float fadeTime = 0.4f;
        [SerializeField] private float tearFrameTime = 0.07f;
        [SerializeField] private float flyTime = 0.55f;
        [SerializeField] private string openedFlag = "prologue:envelope_opened";

        public static bool IsOpen { get; private set; }

        // Play mode starts without reloading scripts (Enter Play Mode Options), so static values
        // survive from the last run: a close-up left open when Play was stopped would block the next one.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsOpen = false;

        private void OnDestroy()
        {
            if (IsOpen) IsOpen = false;
        }

        private Vector2 promptHome;
        private float promptPop;
        private InspectItem captionItem;
        private float shownLetters;
        private float defaultFontSize;
        private const float Pixel = 4f;   // one art pixel on the canvas

        private void Awake()
        {
            if (!Application.isPlaying) return;      // outside Play mode only the caption preview runs
            MakePlateSprites();
            if (captionText != null) defaultFontSize = captionText.fontSize;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            if (blur != null) blur.weight = 0f;
            if (prompt != null) promptHome = prompt.anchoredPosition;
            foreach (var item in items) item.gameObject.SetActive(false);
            HideCaption();
        }

        public void Open()
        {
            if (IsOpen) return;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            IsOpen = true;
            InputBlocker.Push();
            group.blocksRaycasts = true;

            var env = envelope.rectTransform;
            envelope.sprite = tearFrames[0];
            env.anchoredPosition = new Vector2(0f, -700f);
            env.localRotation = Quaternion.identity;
            env.localScale = Vector3.one;
            foreach (var item in items) { item.gameObject.SetActive(false); item.SetInteractive(false); }
            ShowPrompt(null, null);
            HideCaption();

            // The room blurs and darkens, the envelope comes up.
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / fadeTime)
            {
                group.alpha = t;
                if (blur != null) blur.weight = t;
                SetDim(t);
                env.anchoredPosition = new Vector2(0f, Mathf.Lerp(-700f, 0f, EaseOut(t)));
                yield return null;
            }
            group.alpha = 1f;
            if (blur != null) blur.weight = 1f;
            SetDim(1f);
            env.anchoredPosition = Vector2.zero;

            // [Open]: it tears open by itself, the things peek out.
            ShowPrompt(handIcon, "Open");
            yield return WaitForPress(anywhere: true, keys: true, escape: false);
            ShowPrompt(null, null);
            UiSounds.Play(s => s.click);
            UiSounds.Play(s => s.envelopeTear);
            for (int i = 1; i < tearFrames.Length; i++)
            {
                envelope.sprite = tearFrames[i];
                yield return new WaitForSecondsRealtime(tearFrameTime);
            }

            // [Take out]: the things fly out and lie on the screen, the envelope goes aside.
            ShowPrompt(handIcon, "Take out");
            yield return WaitForPress(anywhere: true, keys: true, escape: false);
            ShowPrompt(null, null);
            UiSounds.Play(s => s.click);
            UiSounds.Play(s => s.envelopeItems);
            if (emptyEnvelope != null) envelope.sprite = emptyEnvelope;
            Vector2 mouth = new Vector2(0f, env.rect.height * 0.35f);
            foreach (var item in items)
            {
                var r = (RectTransform)item.transform;
                item.gameObject.SetActive(true);
                r.anchoredPosition = mouth;
                r.localScale = Vector3.one * 0.6f;
                r.localRotation = Quaternion.identity;
            }
            float stagger = 0.08f, total = flyTime + stagger * items.Length;
            for (float time = 0f; time < total; time += Time.unscaledDeltaTime)
            {
                float k = EaseOut(Mathf.Clamp01(time / flyTime));
                env.anchoredPosition = Vector2.Lerp(Vector2.zero, envelopeAside, k);
                env.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, envelopeAsideAngle, k));
                env.localScale = Vector3.one * Mathf.Lerp(1f, envelopeAsideScale, k);
                for (int i = 0; i < items.Length; i++)
                {
                    var r = (RectTransform)items[i].transform;
                    float t = Mathf.Clamp01((time - i * stagger) / flyTime);
                    float e = EaseOutBack(t);
                    Vector2 target = i < itemTargets.Length ? itemTargets[i] : Vector2.zero;
                    float angle = i < itemAngles.Length ? itemAngles[i] : 0f;
                    r.anchoredPosition = Vector2.LerpUnclamped(mouth, target, e) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 120f;
                    r.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(0f, angle, e));
                    r.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, e);
                }
                yield return null;
            }

            // Now they can be moved, and their dots clicked. [Close] when done.
            foreach (var item in items) item.SetInteractive(true);
            ShowPrompt(closeIcon, "Close");
            yield return WaitForPress(anywhere: false, keys: true, escape: true);    // E / Space / Enter, Esc, or a click on Close
            ShowPrompt(null, null);
            UiSounds.Play(s => s.click);
            HideCaption();
            foreach (var item in items) item.SetInteractive(false);
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / fadeTime)
            {
                group.alpha = t;
                if (blur != null) blur.weight = t;
                yield return null;
            }
            group.alpha = 0f;
            if (blur != null) blur.weight = 0f;
            group.blocksRaycasts = false;

            GameState.Instance?.SetFlag(openedFlag);
            // Everything from the envelope goes into the case file (Inventory).
            if (GameState.Instance != null)
                foreach (var item in items)
                    if (item != null && item.Clue != null) GameState.Instance.FindClue(item.Clue);
            InputBlocker.Pop();
            IsOpen = false;
        }

        private void SetDim(float t)
        {
            if (dim == null) return;
            var c = dim.color; c.a = dimAlpha * t; dim.color = c;
        }

        // ---- Prompt ----

        private void ShowPrompt(Sprite icon, string label)
        {
            if (prompt == null) return;
            prompt.gameObject.SetActive(icon != null);
            if (icon == null) return;
            promptIcon.sprite = icon;
            promptLabel.text = label;
            promptPop = 0f;
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                PreviewCaption();
                return;
            }
            UpdateCaption();
            if (prompt == null || !prompt.gameObject.activeSelf) return;
            // Pops in like the prompt in the room, then bobs gently while it waits.
            promptPop = Mathf.Min(1f, promptPop + Time.unscaledDeltaTime / 0.25f);
            prompt.localScale = Vector3.one * Mathf.LerpUnclamped(0.4f, 1f, EaseOutBack(promptPop));
            prompt.anchoredPosition = promptHome + Vector2.up * (Mathf.Sin(Time.unscaledTime * 3f) * 4f);
        }

        // E / Space / Enter (if keys), Esc (if escape), or a click: anywhere, or only on the prompt.
        private IEnumerator WaitForPress(bool anywhere, bool keys, bool escape)
        {
            yield return null;            // not the same press that opened it
            while (true)
            {
                var k = Keyboard.current;
                var m = Mouse.current;
                if (k != null && keys && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                    yield break;
                if (k != null && escape && k.escapeKey.wasPressedThisFrame) yield break;
                if (m != null && m.leftButton.wasPressedThisFrame)
                {
                    bool onPrompt = prompt != null && RectTransformUtility.RectangleContainsScreenPoint(prompt, m.position.ReadValue(), null);
                    if (anywhere || onPrompt) yield break;
                }
                yield return null;
            }
        }

        // ---- What he says about a thing ----

        public void ShowCaption(InspectItem item)
        {
            if (caption == null || string.IsNullOrWhiteSpace(item.Text)) return;
            captionItem = item;
            captionText.text = item.Text.Replace("\\n", "\n");
            shownLetters = 0f;
            captionText.maxVisibleCharacters = 0;
            caption.gameObject.SetActive(true);
            caption.SetAsLastSibling();
            LayoutCaption();
        }

        public void HideCaption()
        {
            captionItem = null;
            if (caption != null) caption.gameObject.SetActive(false);
        }

        private void UpdateCaption()
        {
            if (caption == null || !caption.gameObject.activeSelf || captionItem == null) return;
            // A click anywhere closes it (the click on a dot opens it again on release).
            var m = Mouse.current;
            if (m != null && m.leftButton.wasPressedThisFrame)
            {
                HideCaption();
                return;
            }
            // Typed out letter by letter.
            int total = captionText.text.Length;
            shownLetters = Mathf.Min(total, shownLetters + lettersPerSecond * Time.unscaledDeltaTime);
            captionText.maxVisibleCharacters = Mathf.CeilToInt(shownLetters);
            LayoutCaption();    // follows the thing when it is dragged, and live changes of the settings
        }

        // Outside Play mode: the plate fits round whatever text is in it, where it stands, so it can be styled.
        private void PreviewCaption()
        {
            if (caption == null || captionText == null || !caption.gameObject.activeInHierarchy) return;
            FitPlate(maxWidth);
            if (captionTail != null)
                captionTail.rectTransform.anchoredPosition = new Vector2(0f, -caption.sizeDelta.y / 2f - captionTail.rectTransform.sizeDelta.y / 2f);
        }

        // The text wraps at the width, the plate is the text plus the padding (on the pixel grid).
        private Vector2 FitPlate(float width) => FitPlate(width, platePadding);

        private Vector2 FitPlate(float width, Vector2 padding)
        {
            // After the font file changes (e.g. a Git merge), the text can still point at the old, destroyed material.
            if (captionText.fontSharedMaterial == null && captionText.font != null) captionText.fontSharedMaterial = captionText.font.material;
            width = QuietWitness.UI.TextBlock.Width(captionText, captionText.text, width);   // an even block
            Vector2 size = captionText.GetPreferredValues(captionText.text, width, 0f);
            size.x = Mathf.Max(minWidth, Mathf.Min(size.x, width));
            size.y = captionText.GetPreferredValues(captionText.text, size.x, 0f).y;
            captionText.rectTransform.sizeDelta = size;
            captionText.rectTransform.anchoredPosition = Vector2.zero;
            if (captionTail != null && captionPlate != null) captionTail.color = captionPlate.color;   // the tail is part of the plate
            caption.sizeDelta = new Vector2(Mathf.Ceil((size.x + padding.x * 2f) / Pixel) * Pixel,
                                            Mathf.Ceil((size.y + padding.y * 2f) / Pixel) * Pixel);
            return size;
        }

        // Size the plate around the text and put it over the thing (or under it when there is no room).
        private void LayoutCaption()
        {
            var item = captionItem;
            if (item == null) return;

            // this thing's own settings, or the usual ones
            captionText.fontSize = defaultFontSize;
            FitPlate(maxWidth, platePadding);

            var r = (RectTransform)item.transform;
            var half = ((RectTransform)transform).rect.size / 2f;
            float itemHalf = r.rect.height / 2f * r.localScale.y;
            float tail = captionTail != null ? captionTail.rectTransform.sizeDelta.y : 0f;
            float offset = itemHalf + gapToItem + tail + caption.sizeDelta.y / 2f;
            bool below = item.Side == CaptionSide.Below ||
                         (item.Side == CaptionSide.Auto && r.anchoredPosition.y + offset + caption.sizeDelta.y / 2f > half.y - 16f);
            Vector2 at = r.anchoredPosition + Vector2.up * (below ? -offset : offset) + item.PlateOffset;
            at.x = Mathf.Clamp(at.x, -half.x + caption.sizeDelta.x / 2f + 16f, half.x - caption.sizeDelta.x / 2f - 16f);
            at.y = Mathf.Clamp(at.y, -half.y + caption.sizeDelta.y / 2f + 16f, half.y - caption.sizeDelta.y / 2f - 16f);
            caption.anchoredPosition = new Vector2(Mathf.Round(at.x / Pixel) * Pixel, Mathf.Round(at.y / Pixel) * Pixel);

            if (captionTail != null)
            {
                // the little triangle points at the thing
                var t = captionTail.rectTransform;
                float tx = Mathf.Clamp(r.anchoredPosition.x - caption.anchoredPosition.x,
                                       -caption.sizeDelta.x / 2f + 16f, caption.sizeDelta.x / 2f - 16f);
                t.anchoredPosition = new Vector2(Mathf.Round(tx / Pixel) * Pixel,
                                                 below ? caption.sizeDelta.y / 2f + tail / 2f : -caption.sizeDelta.y / 2f - tail / 2f);
                t.localScale = new Vector3(1f, below ? -1f : 1f, 1f);
            }
        }

        // The same plate as the hero's speech: a 3x3 pixel square with cut corners (9-sliced), and a tail.
        private void MakePlateSprites()
        {
            if (captionPlate != null && captionPlate.sprite == null)
            {
                var tex = new Texture2D(3, 3, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < 3; y++)
                    for (int x = 0; x < 3; x++)
                        tex.SetPixel(x, y, (x != 1 && y != 1) ? Color.clear : Color.white);
                tex.Apply();
                captionPlate.sprite = Sprite.Create(tex, new Rect(0, 0, 3, 3), new Vector2(0.5f, 0.5f), 100f, 0,
                                                    SpriteMeshType.FullRect, new Vector4(1, 1, 1, 1));
                captionPlate.type = Image.Type.Sliced;
                captionPlate.pixelsPerUnitMultiplier = 1f / Pixel;   // one pixel of the corner = 4 canvas units
            }
            if (captionTail != null && captionTail.sprite == null)
            {
                var tex = new Texture2D(5, 3, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < 3; y++)
                    for (int x = 0; x < 5; x++)
                        tex.SetPixel(x, y, Mathf.Abs(x - 2) <= y ? Color.white : Color.clear);
                tex.Apply();
                captionTail.sprite = Sprite.Create(tex, new Rect(0, 0, 5, 3), new Vector2(0.5f, 0.5f), 100f);
                captionTail.rectTransform.sizeDelta = new Vector2(5f, 3f) * Pixel;
            }
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.4f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
