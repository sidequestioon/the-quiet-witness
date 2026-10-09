using System.Collections.Generic;
using QuietWitness.Core;
using QuietWitness.Map;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace QuietWitness.Board
{
    // The evidence board: clues and conclusions as cards.
    // Click one card, then another, to connect them with a red thread.
    // A right pair gives a conclusion (sets "conclusion:<id>"); a wrong pair does nothing.
    public class BoardPanel : MonoBehaviour
    {
        public static BoardPanel Instance { get; private set; }

        [SerializeField] private GameObject window;

        [Header("Cards")]
        [Tooltip("Grid that holds the cards.")]
        [SerializeField] private RectTransform cardsRoot;
        [Tooltip("Hidden card that gets cloned for every clue and conclusion.")]
        [SerializeField] private BoardCardView cardTemplate;

        [Header("Threads")]
        [Tooltip("Layer above the cards where the red threads are drawn.")]
        [SerializeField] private RectTransform threadsRoot;
        [Tooltip("Hidden thin red image, cloned for every thread.")]
        [SerializeField] private RectTransform threadTemplate;

        [Header("Text")]
        [Tooltip("Description of the selected card.")]
        [SerializeField] private TMP_Text detailText;
        [Tooltip("Result of the last connection.")]
        [SerializeField] private TMP_Text statusText;

        [Header("Data")]
        [Tooltip("Every conclusion in the game. Use ⋮ > Collect all conclusions.")]
        [SerializeField] private List<ConclusionData> conclusions = new List<ConclusionData>();

        private readonly List<BoardCardView> views = new List<BoardCardView>();
        private readonly List<RectTransform> threads = new List<RectTransform>();
        private BoardCard selected;

        public bool IsOpen => window.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this) return; // duplicate Systems, removed by its root
            Instance = this;
            cardTemplate.gameObject.SetActive(false);
            threadTemplate.gameObject.SetActive(false);
            window.SetActive(false);
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        public void Open()
        {
            if (IsOpen) return;
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsTravelling) return;

            selected = null;
            window.SetActive(true);
            InputBlocker.Push();
            int cardCount = Rebuild();
            detailText.text = string.Empty;
            statusText.text = cardCount < 2
                ? "Not enough evidence yet."
                : "Pick two cards to connect them.";
        }

        public void Close()
        {
            if (!IsOpen) return;
            window.SetActive(false);
            InputBlocker.Pop();
        }

        // Called by a card.
        public void Click(BoardCardView view)
        {
            var card = view.Card;
            detailText.text = $"<b>{card.Title}</b>\n{card.Description}";

            if (selected == null)
            {
                selected = card;
                statusText.text = "Now pick a second card.";
            }
            else if (selected.Flag == card.Flag)
            {
                selected = null; // clicked the same card again
                statusText.text = "Pick two cards to connect them.";
            }
            else
            {
                Connect(selected, card);
                selected = null;
            }
            Rebuild();
        }

        private void Connect(BoardCard a, BoardCard b)
        {
            var conclusion = conclusions.Find(c => c != null && c.Matches(a.Flag, b.Flag));
            var state = GameState.Instance;

            if (conclusion == null)
            {
                statusText.text = "These don't connect.";
            }
            else if (state.HasFlag(conclusion.Flag))
            {
                statusText.text = $"Already connected: {conclusion.Title}";
            }
            else
            {
                state.SetFlag(conclusion.Flag);
                statusText.text = $"<color=#E8B05C>New conclusion:</color> {conclusion.Title}";
                detailText.text = $"<b>{conclusion.Title}</b>\n{conclusion.Description}";
            }
        }

        // ---- Building the board ----

        private int Rebuild()
        {
            var cards = CollectCards();

            for (int i = 0; i < cards.Count; i++)
            {
                while (views.Count <= i)
                    views.Add(Instantiate(cardTemplate, cardTemplate.transform.parent));
                views[i].gameObject.SetActive(true);
                views[i].Setup(cards[i], selected != null && selected.Flag == cards[i].Flag, this);
            }
            for (int i = cards.Count; i < views.Count; i++)
                views[i].gameObject.SetActive(false);

            DrawThreads(cards.Count);
            return cards.Count;
        }

        private List<BoardCard> CollectCards()
        {
            var cards = new List<BoardCard>();
            var state = GameState.Instance;
            if (state == null) return cards;

            foreach (var clue in state.FoundClues) cards.Add(BoardCard.From(clue));
            foreach (var conclusion in conclusions)
                if (conclusion != null && state.HasFlag(conclusion.Flag))
                    cards.Add(BoardCard.From(conclusion));
            return cards;
        }

        // A red thread between the two cards of every conclusion reached.
        private void DrawThreads(int activeCards)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardsRoot);

            var state = GameState.Instance;
            int used = 0;

            foreach (var conclusion in conclusions)
            {
                if (conclusion == null || state == null || !state.HasFlag(conclusion.Flag)) continue;
                var inputs = conclusion.InputFlags();
                if (inputs.Count != 2) continue;

                var a = FindView(inputs[0], activeCards);
                var b = FindView(inputs[1], activeCards);
                if (a == null || b == null) continue;

                while (threads.Count <= used)
                    threads.Add(Instantiate(threadTemplate, threadsRoot));
                Stretch(threads[used], PinPoint(a), PinPoint(b));
                threads[used].gameObject.SetActive(true);
                used++;
            }

            for (int i = used; i < threads.Count; i++)
                threads[i].gameObject.SetActive(false);
        }

        private BoardCardView FindView(string flag, int activeCards)
        {
            for (int i = 0; i < activeCards && i < views.Count; i++)
                if (views[i].Card != null && views[i].Card.Flag == flag) return views[i];
            return null;
        }

        // Where the pin is: top middle of the card, in thread-layer space.
        private Vector2 PinPoint(BoardCardView view)
        {
            var rt = (RectTransform)view.transform;
            Vector3 top = rt.TransformPoint(new Vector3(rt.rect.center.x, rt.rect.yMax - 14f, 0f));
            return threadsRoot.InverseTransformPoint(top);
        }

        private static void Stretch(RectTransform thread, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            thread.pivot = new Vector2(0f, 0.5f);
            thread.localPosition = from;
            thread.sizeDelta = new Vector2(delta.magnitude, thread.sizeDelta.y);
            thread.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

#if UNITY_EDITOR
        [ContextMenu("Collect all conclusions")]
        private void CollectAllConclusions()
        {
            conclusions.Clear();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:ConclusionData"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var conclusion = UnityEditor.AssetDatabase.LoadAssetAtPath<ConclusionData>(path);
                if (conclusion != null) conclusions.Add(conclusion);
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[Board] Collected {conclusions.Count} conclusions.");
        }
#endif
    }
}
