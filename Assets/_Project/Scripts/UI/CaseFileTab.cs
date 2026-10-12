using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace QuietWitness.UI
{
    // One line of the menu on the left of the case file (Objectives, Inventory).
    // The title of the page at the top-left is this label's text.
    public class CaseFileTab : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Button button;
        public TMP_Text label;
        [Tooltip("The little yellow pointer next to the chosen line.")]
        public Image marker;
        [Tooltip("The yellow dot: something new on this page.")]
        public GameObject badge;

        public bool Hovered { get; private set; }

        public void OnPointerEnter(PointerEventData e) => Hovered = true;
        public void OnPointerExit(PointerEventData e) => Hovered = false;
        private void OnDisable() => Hovered = false;
    }
}
