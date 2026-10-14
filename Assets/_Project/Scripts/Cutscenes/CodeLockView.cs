using System.Collections;
using QuietWitness.Audio;
using QuietWitness.Core;
using QuietWitness.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace QuietWitness.Cutscenes
{
    // The locked drawer up close: a lock with number wheels.
    // A / D (or a click on a wheel) chooses a wheel, W / S or the little arrows turn it. Esc leaves.
    // The right code opens it: the thing inside comes up, [Take] puts it in the case file.
    // Opened by a hotspot (Hotspot Steps > Then > Close Up Opener.Open Code Lock).
    public class CodeLockView : CloseUpBase
    {
        public static CodeLockView Instance { get; private set; }

        [Header("The lock")]
        [Tooltip("The code: as many digits as there are wheels.")]
        [SerializeField] private string code = "1174";
        [SerializeField] private RectTransform lockRoot;
        [SerializeField] private Image[] wheels;
        [Tooltip("Pictures of 0 to 9.")]
        [SerializeField] private Sprite[] digitSprites;
        [SerializeField] private Button[] upButtons;
        [SerializeField] private Button[] downButtons;
        [Tooltip("The frame over the chosen wheel.")]
        [SerializeField] private RectTransform selector;
        [Tooltip("What he thinks while looking at the lock (from Hero Lines).")]
        [HeroLine]
        [SerializeField] private string lockLine = "office.drawer_lock";

        [Header("Inside")]
        [SerializeField] private Image item;
        [Tooltip("Goes into the case file when taken.")]
        [SerializeField] private ClueData clue;
        [HeroLine]
        [SerializeField] private string foundLine = "office.drawer_badge";
        [SerializeField] private string takeLabel = "Take";
        [SerializeField] private string openedFlag = "office:drawer_opened";

        [Header("Sounds")]
        [SerializeField] private AudioClip turnSound;
        [SerializeField] private AudioClip openSound;
        [Range(0f, 1f)]
        [SerializeField] private float soundVolume = 0.6f;

        private int[] digits;
        private int selected;
        private bool turning;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != null && Instance != this) return;   // duplicate Systems, removed by its root
            Instance = this;
            for (int i = 0; i < wheels.Length; i++)
            {
                int w = i;
                if (i < upButtons.Length && upButtons[i] != null) upButtons[i].onClick.AddListener(() => Turn(w, 1));
                if (i < downButtons.Length && downButtons[i] != null) downButtons[i].onClick.AddListener(() => Turn(w, -1));
                var wheelButton = wheels[i].GetComponent<Button>();
                if (wheelButton != null) wheelButton.onClick.AddListener(() => Select(w));
            }
        }

        public void Open()
        {
            if (IsOpen || Instance != this) return;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            if (digits == null || digits.Length != wheels.Length) digits = new int[wheels.Length];
            selected = Mathf.Clamp(selected, 0, wheels.Length - 1);
            lockRoot.gameObject.SetActive(true);
            lockRoot.localScale = Vector3.one;
            item.gameObject.SetActive(false);
            Refresh();

            yield return FadeIn();
            Think(lockLine);

            // Turning the wheels until the code is right (or Esc).
            turning = true;
            bool solved = false;
            while (true)
            {
                var k = Keyboard.current;
                if (k != null)
                {
                    if (k.escapeKey.wasPressedThisFrame) break;
                    if (k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame) Select(selected - 1);
                    if (k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame) Select(selected + 1);
                    if (k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame) Turn(selected, 1);
                    if (k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame) Turn(selected, -1);
                }
                if (Matches()) { solved = true; break; }
                yield return null;
            }
            turning = false;

            if (!solved)
            {
                yield return FadeOut();
                yield break;
            }

            // Click: it opens.
            HideCaption();
            Sfx.Play(openSound, soundVolume);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.6f)
            {
                lockRoot.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.04f);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.2f);
            lockRoot.gameObject.SetActive(false);

            // What was inside comes up.
            item.gameObject.SetActive(true);
            var r = item.rectTransform;
            Vector2 home = r.anchoredPosition;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.45f)
            {
                float e = EaseOutBack(t);
                r.anchoredPosition = home + Vector2.down * (1f - e) * 120f;
                r.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, e);
                var c = item.color; c.a = Mathf.Clamp01(t * 2f); item.color = c;
                yield return null;
            }
            r.anchoredPosition = home;
            r.localScale = Vector3.one;
            Think(foundLine);
            ShowPrompt(handIcon, takeLabel);
            yield return WaitForPress(keys: true, escape: true);
            UiSounds.Play(s => s.click);

            var state = GameState.Instance;
            if (state != null)
            {
                if (clue != null) state.FindClue(clue);
                if (!string.IsNullOrWhiteSpace(openedFlag)) state.SetFlag(openedFlag.Trim());
            }
            yield return FadeOut();
            item.gameObject.SetActive(false);
        }

        private void Select(int wheel)
        {
            if (!turning || wheels.Length == 0) return;
            selected = (wheel + wheels.Length) % wheels.Length;
            Refresh();
        }

        private void Turn(int wheel, int by)
        {
            if (!turning || wheel < 0 || wheel >= digits.Length) return;
            selected = wheel;
            digits[wheel] = (digits[wheel] + by + 10) % 10;
            Sfx.Play(turnSound, soundVolume);
            Refresh();
        }

        private bool Matches()
        {
            if (string.IsNullOrEmpty(code) || code.Length != digits.Length) return false;
            for (int i = 0; i < digits.Length; i++)
                if (code[i] - '0' != digits[i]) return false;
            return true;
        }

        private void Refresh()
        {
            for (int i = 0; i < wheels.Length; i++)
                if (digitSprites != null && digitSprites.Length == 10) wheels[i].sprite = digitSprites[digits[i]];
            if (selector != null && wheels.Length > 0)
                selector.anchoredPosition = wheels[selected].rectTransform.anchoredPosition;
        }
    }
}
