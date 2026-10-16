using System;
using System.Collections;
using System.Collections.Generic;
using QuietWitness.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuietWitness.UI
{
    // One line of a voice that isn't the hero's (a message on the answering machine, a voice on the radio).
    [Serializable]
    public struct VoiceLine
    {
        [Tooltip("Who speaks, small and yellow over the words (e.g. SAL). Empty = no name.")]
        public string speaker;
        [TextArea(1, 3)]
        public string text;
    }

    // Subtitles for a voice at the bottom of the screen: one short line at a time, typed in place, it stays a
    // moment and fades, then the next one. E / Space / Enter / a click: the whole line at once, then the next;
    // Esc: stops. The hero can't walk while it plays. Each line is laid out as an even block.
    // Looks: the objects under Screen > Plate (Speaker, Text). Preview shows it outside Play mode.
    [ExecuteAlways]
    public class VoiceSubtitles : MonoBehaviour
    {
        public static VoiceSubtitles Instance { get; private set; }
        public static bool IsPlaying => Instance != null && Instance.playing;

        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform plate;
        [SerializeField] private TMP_Text speaker;
        [SerializeField] private TMP_Text text;
        [Tooltip("The widest the words get (canvas units); shorter lines make the plate narrower.")]
        [SerializeField] private float widest = 760f;
        [SerializeField] private Vector2 padding = new Vector2(32f, 20f);
        [SerializeField] private float lettersPerSecond = 40f;
        [Tooltip("How long a line stays once typed: this much, plus a little for each letter.")]
        [SerializeField] private float holdTime = 1.1f;
        [SerializeField] private float holdPerLetter = 0.035f;
        [SerializeField] private float fadeTime = 0.15f;

        [Header("Preview outside Play mode")]
        [SerializeField] private bool preview;
        [SerializeField] private VoiceLine previewLine = new VoiceLine { speaker = "SAL", text = "Hey, it's Sal. Yeah, I know. Long time." };

        private bool playing;
        private bool skipLine, stopAll;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (!Application.isPlaying) return;
            if (Instance != null && Instance != this) { enabled = false; return; }
            Instance = this;
            SetAlpha(0f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Play(IList<VoiceLine> lines, Action done = null)
        {
            if (playing || lines == null || lines.Count == 0) { done?.Invoke(); return; }
            StartCoroutine(Run(lines, done));
        }

        private IEnumerator Run(IList<VoiceLine> lines, Action done)
        {
            playing = true;
            stopAll = false;
            InputBlocker.Push();
            foreach (var line in lines)
            {
                if (stopAll) break;
                Fill(line);
                string full = line.text ?? string.Empty;
                skipLine = false;
                // fade in while the letters come
                float a = 0f;
                for (float shown = 0f; shown < full.Length && !skipLine && !stopAll; )
                {
                    float dt = Time.unscaledDeltaTime;
                    shown += dt * lettersPerSecond;
                    a = Mathf.MoveTowards(a, 1f, dt / fadeTime);
                    SetAlpha(a);
                    TextBlock.Reveal(text, full, Mathf.FloorToInt(shown));
                    yield return null;
                }
                text.text = full;
                a = 1f;
                SetAlpha(1f);
                skipLine = false;
                yield return null;
                for (float t = holdTime + holdPerLetter * full.Length; t > 0f && !skipLine && !stopAll; t -= Time.unscaledDeltaTime)
                    yield return null;
                for (; a > 0f; a -= Time.unscaledDeltaTime / fadeTime) { SetAlpha(a); yield return null; }
                SetAlpha(0f);
            }
            SetAlpha(0f);
            playing = false;
            InputBlocker.Pop();
            done?.Invoke();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                if (group == null) return;
                if (preview) { Fill(previewLine); SetAlpha(1f); }
                else SetAlpha(0f);
                return;
            }
            if (!playing) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { stopAll = true; return; }
            if ((kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) ||
                (mouse != null && mouse.leftButton.wasPressedThisFrame))
                skipLine = true;
        }

        // The words and the plate around them: an even block, the name over it.
        private void Fill(VoiceLine line)
        {
            if (text == null || plate == null) return;
            bool named = speaker != null && !string.IsNullOrWhiteSpace(line.speaker);
            if (speaker != null)
            {
                speaker.gameObject.SetActive(named);
                if (named && speaker.text != line.speaker) speaker.text = line.speaker;
            }
            string full = line.text ?? string.Empty;
            text.text = full;
            float wrap = TextBlock.Width(text, full, widest);
            Vector2 size = text.GetPreferredValues(full, wrap, 0f);
            size.x = Mathf.Min(size.x, wrap);
            float nameW = named ? speaker.GetPreferredValues(line.speaker, 100000f, 0f).x : 0f;
            float nameH = named ? speaker.GetPreferredValues(line.speaker, 100000f, 0f).y + 6f : 0f;
            float w = Mathf.Max(size.x, nameW);
            text.rectTransform.sizeDelta = new Vector2(w, size.y);
            text.rectTransform.anchoredPosition = new Vector2(0f, -nameH / 2f);
            if (named)
            {
                speaker.rectTransform.sizeDelta = new Vector2(w, nameH);
                speaker.rectTransform.anchoredPosition = new Vector2(0f, size.y / 2f);
            }
            plate.sizeDelta = new Vector2(Mathf.Ceil((w + padding.x * 2f) / 4f) * 4f,
                                          Mathf.Ceil((size.y + nameH + padding.y * 2f) / 4f) * 4f);
        }

        private void SetAlpha(float a)
        {
            if (group == null) return;
            if (!Mathf.Approximately(group.alpha, a)) group.alpha = a;
            group.blocksRaycasts = false;
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
