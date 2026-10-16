using QuietWitness.Audio;
using QuietWitness.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuietWitness.UI
{
    // A small window in the middle of the screen with the game's controls (walk, look/take/use, pick an action,
    // case file, close). Shown once, when the hero gets up in the prologue; E / Space / Enter / Esc or a click
    // closes it. Looks: the objects under Window (title, rows, the keys pictures in Art/UI/Controls).
    // Preview shows it outside Play mode.
    [ExecuteAlways]
    public class ControlsHint : MonoBehaviour
    {
        public static ControlsHint Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.open;

        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform window;
        [Tooltip("Set once it has been shown (so it comes only once).")]
        [SerializeField] private string shownFlag = "tutorial:controls";
        [SerializeField] private float fadeTime = 0.25f;
        [Tooltip("Seconds before it can be closed, so a key still held from the cutscene doesn't skip it.")]
        [SerializeField] private float minTime = 0.6f;
        [Tooltip("How far the window rises as it comes up (canvas units).")]
        [SerializeField] private float rise = 12f;

        [Header("Preview outside Play mode")]
        [SerializeField] private bool preview;

        private bool open;
        private float shown;
        private float openedAt;
        private Vector2 home;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (!Application.isPlaying) return;
            if (Instance != null && Instance != this) { enabled = false; return; }
            Instance = this;
            if (window != null) home = window.anchoredPosition;
            SetShown(0f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // The first time only (the prologue calls this when he has got up).
        public void ShowOnce()
        {
            var state = GameState.Instance;
            if (state != null && state.HasFlag(shownFlag)) return;
            state?.SetFlag(shownFlag);
            Show();
        }

        public void Show()
        {
            if (open) return;
            open = true;
            openedAt = Time.unscaledTime;
            InputBlocker.Push();
            UiSounds.Play(s => s.page);
        }

        private void Close()
        {
            if (!open) return;
            open = false;
            InputBlocker.Pop();
            UiSounds.Play(s => s.click);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                if (group != null)
                {
                    float a = preview ? 1f : 0f;
                    if (!Mathf.Approximately(group.alpha, a)) group.alpha = a;
                }
                return;
            }
            if (Instance != this) return;

            shown = Mathf.MoveTowards(shown, open ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime));
            SetShown(shown);
            if (window != null)
            {
                float ease = 1f - (1f - shown) * (1f - shown);
                var p = home + Vector2.down * rise * (1f - ease);
                window.anchoredPosition = new Vector2(Mathf.Round(p.x / 4f) * 4f, Mathf.Round(p.y / 4f) * 4f);
            }

            if (!open || Time.unscaledTime - openedAt < minTime) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool press = (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame ||
                                         kb.enterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) ||
                         (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (press) Close();
        }

        private void SetShown(float a)
        {
            if (group == null) return;
            group.alpha = a;
            group.blocksRaycasts = open || a > 0f;
            group.interactable = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        }
#endif
    }
}
