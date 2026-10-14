using System.Collections;
using QuietWitness.Audio;
using QuietWitness.Core;
using QuietWitness.World;
using UnityEngine;
using UnityEngine.UI;

namespace QuietWitness.Cutscenes
{
    // The film frames under the enlarger: a sheet of paper in the developer tray, under the red
    // safelight, and the picture slowly comes up on it. Then the hero says what he sees, and
    // [Close] puts the print in the case file.
    // Opened by a hotspot (Hotspot Steps > Then > Close Up Opener.Open Enlarger).
    public class DevelopView : CloseUpBase
    {
        public static DevelopView Instance { get; private set; }

        [Header("The print")]
        [SerializeField] private RectTransform tray;
        [Tooltip("The picture that comes up on the paper.")]
        [SerializeField] private Image print;
        [Tooltip("Seconds until the picture is fully there.")]
        [SerializeField] private float developTime = 3.5f;
        [Tooltip("What he says when it's there (from Hero Lines).")]
        [HeroLine]
        [SerializeField] private string line = "darkroom.film_print";
        [SerializeField] private string closeLabel = "Close";

        [Header("Story")]
        [Tooltip("Goes into the case file.")]
        [SerializeField] private ClueData clue;
        [SerializeField] private string doneFlag = "darkroom:film_enlarged";

        protected override void Awake()
        {
            base.Awake();
            if (Instance != null && Instance != this) return;   // duplicate Systems, removed by its root
            Instance = this;
        }

        public void Open()
        {
            if (IsOpen || Instance != this) return;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            SetPrint(0f);
            yield return FadeIn();
            yield return new WaitForSecondsRealtime(0.5f);
            UiSounds.Play(s => s.page);

            // The picture comes up: slow at first, then all at once, like in the tray.
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.1f, developTime))
            {
                SetPrint(t * t * (3f - 2f * t));
                if (tray != null) tray.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 1.6f) * 0.6f);
                yield return null;
            }
            SetPrint(1f);
            if (tray != null) tray.localRotation = Quaternion.identity;

            Think(line);
            ShowPrompt(closeIcon, closeLabel);
            yield return WaitForPress(keys: false, escape: true);
            UiSounds.Play(s => s.click);

            var state = GameState.Instance;
            if (state != null)
            {
                if (clue != null) state.FindClue(clue);
                if (!string.IsNullOrWhiteSpace(doneFlag)) state.SetFlag(doneFlag.Trim());
            }
            yield return FadeOut();
        }

        private void SetPrint(float amount)
        {
            if (print == null) return;
            var c = print.color; c.a = amount; print.color = c;
        }
    }
}
