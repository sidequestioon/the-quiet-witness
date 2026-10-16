using System.Collections;
using QuietWitness.Core;
using QuietWitness.UI;
using QuietWitness.World;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace QuietWitness.Cutscenes
{
    // What every full-screen close-up shares (the drawer lock, the enlarger):
    // the room blurs and darkens, a prompt like the one in the room (icon, name above),
    // and a plate with what the hero thinks (its look is set on the Caption object and its Text).
    public abstract class CloseUpBase : MonoBehaviour
    {
        [Header("Screen")]
        [SerializeField] protected CanvasGroup group;
        [Tooltip("Black over the blurred room.")]
        [SerializeField] protected Image dim;
        [Range(0f, 1f)]
        [SerializeField] protected float dimAlpha = 0.65f;
        [Tooltip("Blur of the room (the same profile as the envelope close-up).")]
        [SerializeField] protected VolumeProfile blurProfile;
        [SerializeField] protected float fadeTime = 0.3f;

        [Header("Prompt")]
        [SerializeField] protected RectTransform prompt;
        [SerializeField] protected Image promptIcon;
        [SerializeField] protected TMP_Text promptLabel;
        [SerializeField] protected Sprite handIcon;
        [SerializeField] protected Sprite closeIcon;

        [Header("What he thinks (style it on Caption and Caption > Text)")]
        [SerializeField] protected RectTransform caption;
        [SerializeField] protected TMP_Text captionText;
        [Tooltip("The plate never gets wider than this; longer text wraps.")]
        [SerializeField] protected float captionMaxWidth = 760f;
        [Tooltip("Plate around the text: x = left/right, y = top/bottom.")]
        [SerializeField] protected Vector2 platePadding = new Vector2(28f, 18f);
        [SerializeField] protected float lettersPerSecond = 45f;

        public bool IsOpen { get; private set; }

        protected bool Escaped { get; private set; }

        private Vector2 promptHome;
        private float promptPop;
        private float shownLetters;
        private bool typing;
        private string fullCaption = string.Empty;   // the whole thought (the text shows part of it while typing)

        protected virtual void Awake()
        {
            if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
            if (prompt != null) { promptHome = prompt.anchoredPosition; prompt.gameObject.SetActive(false); }
            if (caption != null) caption.gameObject.SetActive(false);
            SetDim(0f);
        }

        protected virtual void Update()
        {
            if (prompt != null && prompt.gameObject.activeSelf)
            {
                // Pops in, then bobs gently while it waits.
                promptPop = Mathf.Min(1f, promptPop + Time.unscaledDeltaTime / 0.25f);
                prompt.localScale = Vector3.one * Mathf.LerpUnclamped(0.4f, 1f, EaseOutBack(promptPop));
                prompt.anchoredPosition = promptHome + Vector2.up * (Mathf.Sin(Time.unscaledTime * 3f) * 4f);
            }
            if (typing && captionText != null)
            {
                shownLetters += lettersPerSecond * Time.unscaledDeltaTime;
                QuietWitness.UI.TextBlock.Reveal(captionText, fullCaption, Mathf.FloorToInt(shownLetters));
                if (shownLetters >= fullCaption.Length) typing = false;
            }
        }

        // ---- Opening and closing ----

        protected IEnumerator FadeIn()
        {
            IsOpen = true;
            InputBlocker.Push();
            group.blocksRaycasts = true;
            group.interactable = true;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime))
            {
                Show(t);
                yield return null;
            }
            Show(1f);
        }

        protected IEnumerator FadeOut()
        {
            ShowPrompt(null, null);
            HideCaption();
            group.interactable = false;
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime))
            {
                Show(t);
                yield return null;
            }
            Show(0f);
            group.blocksRaycasts = false;
            InputBlocker.Pop();
            IsOpen = false;
        }

        private void Show(float t)
        {
            group.alpha = t;
            SetDim(t);
            ScreenBlur.Set(blurProfile, t);
        }

        private void SetDim(float t)
        {
            if (dim == null) return;
            var c = dim.color; c.a = dimAlpha * t; dim.color = c;
        }

        // ---- Prompt ----

        protected void ShowPrompt(Sprite icon, string label)
        {
            if (prompt == null) return;
            bool on = icon != null;
            prompt.gameObject.SetActive(on);
            if (!on) return;
            promptIcon.sprite = icon;
            if (promptLabel != null) promptLabel.text = label;
            promptPop = 0f;
        }

        // E / Space / Enter (if keys), a click on the prompt (or anywhere), Esc (if escape: sets Escaped).
        // While his thought is still typing, the first press only shows all of it.
        protected IEnumerator WaitForPress(bool keys, bool escape, bool anywhere = false)
        {
            Escaped = false;
            yield return null;            // not the same press that got us here
            while (true)
            {
                var k = Keyboard.current;
                var m = Mouse.current;
                if (k != null && keys && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                {
                    if (typing) FinishTyping();   // first press: the whole thought at once; the next one goes on
                    else yield break;
                }
                if (k != null && escape && k.escapeKey.wasPressedThisFrame) { Escaped = true; yield break; }
                if (m != null && m.leftButton.wasPressedThisFrame)
                {
                    if (typing) { FinishTyping(); }
                    else
                    {
                        bool onPrompt = prompt != null && prompt.gameObject.activeSelf &&
                                        RectTransformUtility.RectangleContainsScreenPoint(prompt, m.position.ReadValue(), null);
                        if (anywhere || onPrompt) yield break;
                    }
                }
                yield return null;
            }
        }

        // ---- What he thinks ----

        protected void Think(string lineId)
        {
            var line = HeroLines.Find(lineId);
            if (line == null || string.IsNullOrWhiteSpace(line.text) || caption == null || captionText == null)
            {
                HideCaption();
                return;
            }
            if (captionText.fontSharedMaterial == null && captionText.font != null) captionText.fontSharedMaterial = captionText.font.material;
            captionText.text = fullCaption = line.text.Replace("\\n", "\n");
            float wrap = QuietWitness.UI.TextBlock.Width(captionText, fullCaption, captionMaxWidth - platePadding.x * 2f, 3, 0f, 40);
            Vector2 size = captionText.GetPreferredValues(captionText.text, wrap, 0f);
            size.x = Mathf.Min(size.x, wrap);
            caption.sizeDelta = size + platePadding * 2f;
            var r = captionText.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = platePadding;
            r.offsetMax = -platePadding;
            shownLetters = 0f;
            QuietWitness.UI.TextBlock.Reveal(captionText, fullCaption, 0);   // letters appear where they will stay
            typing = true;
            caption.gameObject.SetActive(true);
        }

        protected void FinishTyping()
        {
            typing = false;
            if (captionText != null) QuietWitness.UI.TextBlock.Reveal(captionText, fullCaption, fullCaption.Length);
        }

        protected void HideCaption()
        {
            typing = false;
            if (caption != null) caption.gameObject.SetActive(false);
        }

        protected static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        protected static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
