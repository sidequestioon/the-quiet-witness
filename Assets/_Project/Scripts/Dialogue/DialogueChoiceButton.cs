using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuietWitness.Dialogue
{
    // One answer option. Cloned from a hidden template by DialoguePanel.
    [RequireComponent(typeof(Button))]
    public class DialogueChoiceButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        public void Setup(int index, string text, DialoguePanel panel)
        {
            label.text = $"{index + 1}. {text}";
            var button = GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => panel.Choose(index));
        }
    }
}
