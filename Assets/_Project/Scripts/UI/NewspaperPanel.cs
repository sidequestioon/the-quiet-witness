using System.Collections.Generic;
using QuietWitness.Core;
using QuietWitness.Map;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace QuietWitness.UI
{
    // The front page that opens every time segment (and at the start of the game).
    // Click an article to circle it: if it hides a clue, the clue is found.
    // N re-reads the latest edition. Esc / E puts it down.
    public class NewspaperPanel : MonoBehaviour
    {
        public static NewspaperPanel Instance { get; private set; }

        [SerializeField] private GameObject window;
        [SerializeField] private TMP_Text masthead;
        [SerializeField] private TMP_Text dateLine;
        [SerializeField] private TMP_Text headline;
        [SerializeField] private TMP_Text lead;
        [Tooltip("Hidden article that gets cloned for every printed article.")]
        [SerializeField] private NewspaperArticleView articleTemplate;

        [Tooltip("Every edition in the game. Use ⋮ > Collect all newspapers.")]
        [SerializeField] private List<NewspaperData> editions = new List<NewspaperData>();

        private readonly List<NewspaperArticleView> views = new List<NewspaperArticleView>();
        private NewspaperData latest;
        private int openFrame;

        public bool IsOpen => window.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this) return; // duplicate Systems, removed by its root
            Instance = this;
            articleTemplate.gameObject.SetActive(false);
            window.SetActive(false);
        }

        private void Start()
        {
            if (Instance != this) return;
            var time = TimeManager.Instance;
            if (time == null) return;

            time.SegmentChanged += OnSegmentChanged;
            OnSegmentChanged(time.Current); // the first edition opens the game
        }

        private void OnDestroy()
        {
            if (Instance == this && TimeManager.Instance != null)
                TimeManager.Instance.SegmentChanged -= OnSegmentChanged;
        }

        private void Update()
        {
            if (Instance != this || Keyboard.current == null) return;
            var keyboard = Keyboard.current;

            if (IsOpen)
            {
                if (Time.frameCount != openFrame &&
                    (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame))
                    Close();
            }
            else if (keyboard.nKey.wasPressedThisFrame && latest != null && !InputBlocker.IsBlocked)
            {
                Open(latest);
            }
        }

        private void OnSegmentChanged(TimeSegment segment)
        {
            var edition = editions.Find(e => e != null && e.Segment == segment);
            if (edition == null) return;
            if (GameState.Instance != null && GameState.Instance.HasFlag(edition.ReadFlag)) return;
            Open(edition);
        }

        public void Open(NewspaperData edition)
        {
            if (edition == null || IsOpen) return;
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsTravelling) return;

            var state = GameState.Instance;
            latest = edition;
            openFrame = Time.frameCount;

            masthead.text = edition.Masthead;
            dateLine.text = edition.DateLine;
            var h = edition.PickHeadline(state);
            headline.text = h != null ? h.headline : string.Empty;
            lead.text = h != null ? h.lead : string.Empty;

            int shown = 0;
            foreach (var article in edition.Articles)
            {
                if (article == null || (article.when != null && !article.when.IsMet(state))) continue;
                while (views.Count <= shown)
                    views.Add(Instantiate(articleTemplate, articleTemplate.transform.parent));
                views[shown].gameObject.SetActive(true);
                views[shown].Setup(article);
                shown++;
            }
            for (int i = shown; i < views.Count; i++) views[i].gameObject.SetActive(false);

            if (state != null) state.SetFlag(edition.ReadFlag);
            window.SetActive(true);
            InputBlocker.Push();
        }

        public void Close()
        {
            if (!IsOpen) return;
            window.SetActive(false);
            InputBlocker.Pop();
        }

#if UNITY_EDITOR
        [ContextMenu("Collect all newspapers")]
        private void CollectAllNewspapers()
        {
            editions.Clear();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:NewspaperData"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var edition = UnityEditor.AssetDatabase.LoadAssetAtPath<NewspaperData>(path);
                if (edition != null) editions.Add(edition);
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[Newspaper] Collected {editions.Count} editions.");
        }
#endif
    }
}
