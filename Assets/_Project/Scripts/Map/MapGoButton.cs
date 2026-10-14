using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuietWitness.Map
{
    // The "Go there" row at the bottom of a district's plate: hovering lights it up (a soft plate behind it,
    // the icon a little bigger, the text brighter), a click goes there - same as a second click on the dot.
    public class MapGoButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Tooltip("The soft plate behind the row (this object's picture).")]
        [SerializeField] private Image back;
        [SerializeField] private RectTransform icon;
        [SerializeField] private TMP_Text label;
        [Tooltip("Colour of the plate behind while the mouse is over it.")]
        [SerializeField] private Color hoverBack = new Color(0.94f, 0.85f, 0.64f, 0.14f);
        [Tooltip("How much bigger the icon gets on hover.")]
        [SerializeField] private float hoverScale = 1.15f;
        [Tooltip("Text colour on hover (the usual colour is the one set on the text).")]
        [SerializeField] private Color hoverText = new Color(1f, 0.95f, 0.82f, 1f);
        [SerializeField] private float speed = 14f;

        private bool hovered;
        private float amount;          // 0 = normal, 1 = hovered
        private Color normalText;
        private bool hasNormal;

        private void OnEnable()
        {
            hovered = false;
            amount = 0f;
            if (label != null && !hasNormal) { normalText = label.color; hasNormal = true; }
            Apply();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (!Can) return;
            hovered = true;
        }

        public void OnPointerExit(PointerEventData e) => hovered = false;

        public void OnPointerClick(PointerEventData e)
        {
            var map = TownMap.Instance;
            if (map == null) return;
            map.Clicked();
            if (Can) map.GoShown();
        }

        // Only when the round icon is there (he can go to this place).
        private bool Can => icon != null && icon.gameObject.activeSelf;

        private void Update()
        {
            float target = hovered && Can ? 1f : 0f;
            if (Mathf.Approximately(amount, target)) return;
            amount = Mathf.MoveTowards(amount, target, Time.unscaledDeltaTime * speed);
            Apply();
        }

        private void Apply()
        {
            if (back != null) { var c = hoverBack; c.a *= amount; back.color = c; }
            if (icon != null) icon.localScale = Vector3.one * Mathf.Lerp(1f, hoverScale, amount);
            if (label != null && hasNormal) label.color = Color.Lerp(normalText, hoverText, amount);
        }
    }
}
