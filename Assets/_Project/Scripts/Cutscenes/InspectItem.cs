using QuietWitness.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuietWitness.Cutscenes
{
    // One thing from the envelope lying on the screen: it can be dragged with the mouse,
    // and its little round dot shows what the hero thinks about it (Lines).
    // The dot is the child object "Dot": move it where you want it on the thing (Rect Transform > Pos X / Pos Y).
    public enum CaptionSide { Auto, Above, Below }

    public class InspectItem : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("The evidence this thing is. Its Description is what the hero says when the dot is clicked, " +
                 "and the same words are shown in the case file (Inventory). Opening the envelope adds it there.")]
        [SerializeField] private ClueData clue;
        [Tooltip("Used only when there is no Clue: what the hero says about it. Enter (or \\n) starts a new line.")]
        [TextArea(2, 6)]
        public string[] lines;
        [Header("Its plate (0 = as set on Envelope Inspect / Caption)")]
        [Tooltip("Widest the plate may get for this text (canvas units). 0 = the usual width (Envelope Inspect > Max Width).")]
        [SerializeField] private float plateWidth = 0f;
        [Tooltip("Text size for this thing. 0 = the size on Caption > Text.")]
        [SerializeField] private float fontSize = 0f;
        [Tooltip("Plate around the text for this thing (x = left/right, y = top/bottom). 0 = Envelope Inspect > Plate Padding.")]
        [SerializeField] private Vector2 platePadding = Vector2.zero;
        [Tooltip("Where the plate goes: Auto = over the thing (under it when there is no room).")]
        [SerializeField] private CaptionSide side = CaptionSide.Auto;
        [Tooltip("Moves this thing's plate from its usual place (canvas units, x right, y up).")]
        [SerializeField] private Vector2 plateOffset = Vector2.zero;
        [SerializeField] private Image dot;
        [SerializeField] private Sprite dotSprite;
        [SerializeField] private Sprite dotHover;

        public bool Interactive { get; private set; }
        public float PlateWidth => plateWidth;
        public float FontSize => fontSize;
        public Vector2 PlatePadding => platePadding;
        public CaptionSide Side => side;
        public Vector2 PlateOffset => plateOffset;
        public bool Read { get; private set; }
        public ClueData Clue => clue;

        // What the hero says about it: the clue's Description, or Lines when there is no clue.
        public string Text => clue != null && !string.IsNullOrWhiteSpace(clue.Description)
            ? clue.Description
            : lines != null ? string.Join(" ", lines) : string.Empty;

        private RectTransform rect;
        private Canvas canvas;
        private EnvelopeInspect owner;
        private bool hovered;
        private float dotIn;

        private void Awake()
        {
            rect = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
            owner = GetComponentInParent<EnvelopeInspect>();
            SetInteractive(false);
        }

        public void SetInteractive(bool on)
        {
            Interactive = on;
            dotIn = 0f;
            if (dot != null) dot.gameObject.SetActive(on && !string.IsNullOrWhiteSpace(Text));
        }

        private void Update()
        {
            if (dot == null || !dot.gameObject.activeSelf) return;
            // The dot pops in, stays upright however the paper is turned, and grows a little under the mouse.
            dotIn = Mathf.Min(1f, dotIn + Time.unscaledDeltaTime * 4f);
            dot.rectTransform.rotation = Quaternion.identity;
            float s = Mathf.Lerp(0.3f, 1f, dotIn) * (hovered ? 1.25f : 1f);
            dot.rectTransform.localScale = Vector3.one * s;
            dot.sprite = hovered && dotHover != null ? dotHover : dotSprite;
            if (dot.sprite != null) dot.rectTransform.sizeDelta = dot.sprite.rect.size * 4f;   // pixel art at 4x
            float s2 = hovered && dotHover != null ? 1f : s;
            dot.rectTransform.localScale = Vector3.one * s2;
            dot.color = Color.white;   // always the same black dot, read or not
        }

        // Called by the dot.
        public void DotEnter() => hovered = true;
        public void DotExit() => hovered = false;
        public void DotClick()
        {
            if (!Interactive) return;
            Read = true;
            rect.SetAsLastSibling();
            owner.ShowCaption(this);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!Interactive) return;
            rect.SetAsLastSibling();               // the one you touch lies on top
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (!Interactive) return;
            owner.HideCaption();
            rect.localScale = Vector3.one * 1.05f; // lifted
        }

        public void OnDrag(PointerEventData e)
        {
            if (!Interactive) return;
            var p = rect.anchoredPosition + e.delta / canvas.scaleFactor;
            var half = ((RectTransform)canvas.transform).rect.size / 2f;
            p.x = Mathf.Clamp(p.x, -half.x + 40f, half.x - 40f);
            p.y = Mathf.Clamp(p.y, -half.y + 40f, half.y - 40f);
            rect.anchoredPosition = p;
        }

        public void OnEndDrag(PointerEventData e)
        {
            rect.localScale = Vector3.one;
        }
    }
}
