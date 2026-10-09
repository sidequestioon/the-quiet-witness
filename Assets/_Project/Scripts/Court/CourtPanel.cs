using System.Collections.Generic;
using QuietWitness.Board;
using QuietWitness.Core;
using QuietWitness.Dialogue;
using QuietWitness.Map;
using QuietWitness.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuietWitness.Court
{
    // The courtroom finale. Intro -> each claim (present evidence or let it pass) -> verdict.
    public class CourtPanel : MonoBehaviour
    {
        public static CourtPanel Instance { get; private set; }

        [SerializeField] private GameObject window;
        [SerializeField] private TMP_Text header;
        [Tooltip("Big line: the claim, 'Sustained', the verdict.")]
        [SerializeField] private TMP_Text stage;
        [Tooltip("Smaller text under it.")]
        [SerializeField] private TMP_Text body;

        [Header("Evidence")]
        [SerializeField] private GameObject evidenceArea;
        [Tooltip("Hidden button cloned for every clue and conclusion.")]
        [SerializeField] private DialogueChoiceButton cardTemplate;

        [Header("Buttons")]
        [SerializeField] private Button passButton;
        [SerializeField] private Button continueButton;

        private readonly List<DialogueChoiceButton> cards = new List<DialogueChoiceButton>();
        private CourtCase currentCase;
        private int pointIndex;
        private int score;
        private bool finished;
        private CourtCase.Verdict verdict;

        public bool IsOpen => window.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this) return; // duplicate Systems, removed by its root
            Instance = this;
            cardTemplate.gameObject.SetActive(false);
            passButton.onClick.AddListener(() => Answer(null));
            continueButton.onClick.AddListener(Continue);
            window.SetActive(false);
        }

        public void Begin(CourtCase courtCase)
        {
            if (IsOpen || courtCase == null) return;
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsTravelling) return;

            currentCase = courtCase;
            pointIndex = -1;
            score = 0;
            finished = false;
            verdict = null;

            window.SetActive(true);
            InputBlocker.Push();

            header.text = courtCase.Title;
            stage.text = "All rise.";
            body.text = courtCase.Intro;
            ShowEvidence(false);
            continueButton.gameObject.SetActive(true);
        }

        // Next claim, or the verdict after the last one.
        private void Continue()
        {
            if (finished)
            {
                Close();
                return;
            }

            pointIndex++;
            if (pointIndex < currentCase.Points.Count) AskPoint();
            else ShowVerdict();
        }

        private void AskPoint()
        {
            var point = currentCase.Points[pointIndex];
            stage.text = $"Prosecution: \"{point.claim}\"";
            body.text = "Present evidence to object, or let it pass.";
            ShowEvidence(true);
            continueButton.gameObject.SetActive(false);
        }

        private void Answer(BoardCard card)
        {
            var point = currentCase.Points[pointIndex];
            bool right = card != null && point.IsAnsweredBy(card.Flag);
            if (right) score++;

            stage.text = card == null ? "You stay silent." : right ? "Objection sustained." : "Objection overruled.";
            string presented = card != null ? $"<i>You present: {card.Title}.</i>\n" : string.Empty;
            body.text = presented + (right ? point.sustained : point.overruled);

            ShowEvidence(false);
            continueButton.gameObject.SetActive(true);
        }

        private void ShowVerdict()
        {
            var state = GameState.Instance;
            verdict = currentCase.PickVerdict(score, state);
            finished = true;

            if (verdict != null)
            {
                if (state != null)
                {
                    state.SetFlag(verdict.Flag);
                    state.SetFlag("court:" + currentCase.Id);
                }
                stage.text = verdict.title;
                body.text = verdict.text;
            }
            else
            {
                stage.text = "The court is adjourned.";
                body.text = string.Empty;
            }
            ShowEvidence(false);
            continueButton.gameObject.SetActive(true);
        }

        private void Close()
        {
            window.SetActive(false);
            InputBlocker.Pop();
            if (verdict != null && verdict.newspaper != null && NewspaperPanel.Instance != null)
                NewspaperPanel.Instance.Open(verdict.newspaper); // Tuesday's paper
        }

        private void ShowEvidence(bool show)
        {
            evidenceArea.SetActive(show);
            passButton.gameObject.SetActive(show);
            if (!show) return;

            var list = CollectCards();
            for (int i = 0; i < list.Count; i++)
            {
                while (cards.Count <= i)
                    cards.Add(Instantiate(cardTemplate, cardTemplate.transform.parent));
                var card = list[i];
                string label = card.IsConclusion ? $"<b>{card.Title}</b>" : card.Title;
                cards[i].gameObject.SetActive(true);
                cards[i].Setup(label, () => Answer(card));
            }
            for (int i = list.Count; i < cards.Count; i++) cards[i].gameObject.SetActive(false);
        }

        // Found clues and reached conclusions (the board knows every conclusion).
        private static List<BoardCard> CollectCards()
        {
            var result = new List<BoardCard>();
            var state = GameState.Instance;
            if (state == null) return result;

            foreach (var clue in state.FoundClues) result.Add(BoardCard.From(clue));
            if (BoardPanel.Instance != null)
                foreach (var conclusion in BoardPanel.Instance.Conclusions)
                    if (conclusion != null && state.HasFlag(conclusion.Flag)) result.Add(BoardCard.From(conclusion));
            return result;
        }
    }
}
