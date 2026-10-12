using UnityEngine;
using UnityEngine.EventSystems;

namespace QuietWitness.Cutscenes
{
    // The little round dot on a thing from the envelope: click it to hear the hero's thought.
    public class InspectDot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private InspectItem item;

        private void Awake() => item = GetComponentInParent<InspectItem>();

        public void OnPointerClick(PointerEventData e) => item.DotClick();
        public void OnPointerEnter(PointerEventData e) => item.DotEnter();
        public void OnPointerExit(PointerEventData e) => item.DotExit();
    }
}
