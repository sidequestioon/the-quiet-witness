using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QuietWitness.Dialogue
{
    // A button with a text label: a dialogue choice or a line in the evidence list.
    // Cloned from a hidden template.
    [RequireComponent(typeof(Button))]
    public class DialogueChoiceButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        public void Setup(string text, UnityAction onClick)
        {
            label.text = text;
            var button = GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }
    }
}
