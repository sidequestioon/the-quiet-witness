using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuietWitness.Board
{
    // A card on the board UI. Cloned from a hidden template by BoardPanel.
    [RequireComponent(typeof(Button))]
    public class BoardCardView : MonoBehaviour
    {
        private static readonly Color ClueColor = new Color32(0xEE, 0xE6, 0xD3, 0xFF);
        private static readonly Color ConclusionColor = new Color32(0xF2, 0xC9, 0xB8, 0xFF);
        private static readonly Color SelectedColor = new Color32(0xE8, 0xB0, 0x5C, 0xFF);

        [SerializeField] private Image background;
        [SerializeField] private TMP_Text kind;
        [SerializeField] private TMP_Text title;

        public BoardCard Card { get; private set; }

        public void Setup(BoardCard card, bool selected, BoardPanel panel)
        {
            Card = card;
            kind.text = card.IsConclusion ? "CONCLUSION" : "CLUE";
            title.text = card.Title;
            background.color = selected ? SelectedColor : card.IsConclusion ? ConclusionColor : ClueColor;

            var button = GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => panel.Click(this));
        }
    }
}
