using System.Collections.Generic;
using QuietWitness.Core;
using QuietWitness.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuietWitness.UI
{
    // Two words in yellow in the bottom-left corner when something new goes into the case file, so it can't
    // be missed (the yellow dot on the icon is easy to overlook): NEW EVIDENCE when a thing is found,
    // NEW OBJECTIVE when a new goal shows up. It waits until the hero is free (no close-up, no speech), slides in,
    // stays a few seconds, fades. Opening the case file clears it. Looks: the Kind text under it;
    // Preview shows it outside Play mode. (Icon and Title are optional and not used now.)
    [ExecuteAlways]
    public class CaseFileNotice : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform plate;
        [Tooltip("The thing's picture, for one new piece of evidence. Hidden otherwise.")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text kind;
        [SerializeField] private TMP_Text title;
        [SerializeField] private ObjectiveList objectives;
        [Tooltip("A long title wraps onto more lines past this width (canvas units).")]
        [SerializeField] private float maxTitleWidth = 460f;

        [Header("Words")]
        [SerializeField] private string evidenceLabel = "NEW EVIDENCE";
        [SerializeField] private string entryLabel = "NEW OBJECTIVE";
        [Tooltip("When several things come at once. {0} = how many.")]
        [SerializeField] private string manyItems = "{0} new items";
        [SerializeField] private string manyEntries = "{0} new entries";

        [Header("Timing")]
        [SerializeField] private float showTime = 3.5f;
        [SerializeField] private float fadeTime = 0.3f;
        [Tooltip("How far it slides in from the left (canvas units).")]
        [SerializeField] private float slide = 30f;

        [Header("Preview outside Play mode")]
        [SerializeField] private bool preview;
        [SerializeField] private bool previewEvidence = true;
        [SerializeField] private string previewTitle = "Cinema Ticket";
        [SerializeField] private Sprite previewIcon;

        private struct Notice
        {
            public string kind, title;
            public Sprite icon;
        }

        private readonly Queue<Notice> queue = new Queue<Notice>();
        private readonly List<ClueData> newClues = new List<ClueData>();
        private readonly List<ObjectiveList.Goal> newGoals = new List<ObjectiveList.Goal>();
        private GameState state;
        private Vector2 home;
        private float gatherUntil;
        private float shown;           // 0 hidden .. 1 fully in
        private float timeLeft;
        private bool showing;

        private void Start()
        {
            if (!Application.isPlaying) return;
            if (plate != null) home = plate.anchoredPosition;
            SetAlpha(0f);
            state = GameState.Instance;
            if (state == null || state.transform.root != transform.root) { state = null; return; }   // duplicate Systems
            state.ClueFound += OnClueFound;
            state.FlagSet += OnFlagSet;
        }

        private void OnDestroy()
        {
            if (state == null) return;
            state.ClueFound -= OnClueFound;
            state.FlagSet -= OnFlagSet;
        }

        private void OnClueFound(ClueData clue)
        {
            if (clue == null) return;
            newClues.Add(clue);
            gatherUntil = Time.unscaledTime + 0.4f;          // things found together come as one notice
        }

        // A goal that has just shown up in the case file (and wasn't announced before).
        private void OnFlagSet(string flag)
        {
            if (objectives == null || flag.StartsWith("told:") || flag.StartsWith("seen:")) return;
            foreach (var g in objectives.goals)
            {
                if (g == null || string.IsNullOrEmpty(g.id)) continue;
                string told = "told:goal:" + g.id;
                if (!ObjectiveList.Shown(g) || ObjectiveList.Done(g) || state.HasFlag(told) || state.HasFlag(g.SeenFlag)) continue;
                state.SetFlag(told);
                newGoals.Add(g);
                gatherUntil = Time.unscaledTime + 0.4f;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                EditorPreview();
                return;
            }
            if (state == null) return;

            // Gather what came together, then queue it.
            if ((newClues.Count > 0 || newGoals.Count > 0) && Time.unscaledTime >= gatherUntil)
            {
                if (newGoals.Count > 0)
                {
                    // the biggest of them: the main goal, or a step (not a mini step)
                    var top = newGoals.Find(g => g.main) ?? newGoals.Find(g => !g.mini) ?? newGoals[0];
                    string text = top != null ? top.text : string.Format(manyEntries, newGoals.Count);
                    queue.Enqueue(new Notice { kind = entryLabel, title = text });
                }
                if (newClues.Count > 0)
                {
                    bool one = newClues.Count == 1;
                    queue.Enqueue(new Notice
                    {
                        kind = evidenceLabel,
                        title = one ? newClues[0].Title : string.Format(manyItems, newClues.Count),
                        icon = one ? newClues[0].Icon : null,
                    });
                }
                newGoals.Clear();
                newClues.Clear();
            }

            // Opening the case file: everything is seen.
            if (CaseFilePanel.IsOpen)
            {
                queue.Clear();
                if (showing) timeLeft = 0f;
            }

            bool free = !InputBlocker.IsBlocked && !CaseFilePanel.IsOpen &&
                        (SceneLoader.Instance == null || !SceneLoader.Instance.IsTravelling);
            if (!showing && queue.Count > 0 && free)
            {
                Fill(queue.Dequeue());
                showing = true;
                timeLeft = showTime;
            }

            float dt = Time.unscaledDeltaTime;
            if (showing)
            {
                timeLeft -= dt;
                float target = timeLeft > 0f ? 1f : 0f;
                shown = Mathf.MoveTowards(shown, target, dt / Mathf.Max(0.01f, fadeTime));
                if (timeLeft <= 0f && shown <= 0f) showing = false;
            }
            SetAlpha(shown);
            if (plate != null)
            {
                float ease = 1f - (1f - shown) * (1f - shown);
                plate.anchoredPosition = home + Vector2.left * slide * (1f - ease);
            }
        }

        private void Fill(Notice n)
        {
            if (kind != null) kind.text = n.kind;
            if (title != null) { title.text = n.title; FitTitle(); }
            if (icon != null)
            {
                icon.sprite = n.icon;
                icon.gameObject.SetActive(n.icon != null);
                if (n.icon != null)
                {
                    var size = n.icon.rect.size;
                    float scale = Mathf.Max(1f, Mathf.Floor(48f / Mathf.Max(size.x, size.y)));
                    icon.rectTransform.sizeDelta = size * scale;
                    var le = icon.GetComponent<LayoutElement>();
                    if (le != null) { le.preferredWidth = size.x * scale; le.preferredHeight = size.y * scale; }
                }
            }
            if (plate != null) LayoutRebuilder.ForceRebuildLayoutImmediate(plate);
        }

        // Short titles keep the plate small; long ones wrap at Max Title Width.
        private void FitTitle()
        {
            if (title == null || !title.TryGetComponent<LayoutElement>(out var le)) return;
            float w = Mathf.Min(maxTitleWidth, Mathf.Ceil(title.GetPreferredValues(title.text, 100000f, 0f).x) + 2f);
            if (!Mathf.Approximately(le.preferredWidth, w)) le.preferredWidth = w;
        }

        private void SetAlpha(float a)
        {
            if (group == null) return;
            if (!Mathf.Approximately(group.alpha, a)) group.alpha = a;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void EditorPreview()
        {
            if (group == null) return;
            SetAlpha(preview ? 1f : 0f);
            if (!preview) return;
            var n = new Notice
            {
                kind = previewEvidence ? evidenceLabel : entryLabel,
                title = previewTitle,
                icon = previewEvidence ? previewIcon : null,
            };
            if (kind != null && kind.text != n.kind) kind.text = n.kind;
            if (title != null && title.text != n.title) title.text = n.title;
            FitTitle();
            if (icon != null)
            {
                bool on = n.icon != null;
                if (icon.gameObject.activeSelf != on) icon.gameObject.SetActive(on);
                if (on && icon.sprite != n.icon) icon.sprite = n.icon;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        }
#endif
    }
}
