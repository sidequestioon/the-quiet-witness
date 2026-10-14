using System.Collections.Generic;
using QuietWitness.Core;
using QuietWitness.World;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QuietWitness.Map
{
    // One place on the town map. Move it on the paper where the place is drawn.
    // Shown when the place is unlocked (its Location's condition) and Show When flags are set.
    // Lit (yellow, pulsing ring) while Light When flags are set and the place isn't visited yet:
    // that's where the story goes next, and its Hint is written at the bottom of the map.
    [RequireComponent(typeof(Button))]
    public class MapPin : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private LocationData location;
        [Tooltip("Extra flags needed to show this pin (on top of the location's own unlock condition).")]
        [SerializeField] private List<string> showWhen = new List<string>();
        [Tooltip("Lit while all of these are set and the place isn't visited yet. Empty = never lit.")]
        [SerializeField] private List<string> lightWhen = new List<string>();
        [Tooltip("The hero's hint at the bottom of the map while this pin is lit (from Hero Lines).")]
        [HeroLine]
        [SerializeField] private string hint;

        [Header("Parts")]
        [SerializeField] private Image pin;
        [SerializeField] private Image ring;
        [SerializeField] private TMP_Text label;
        [Tooltip("\"You are here\" marker.")]
        [SerializeField] private GameObject here;
        [SerializeField] private Color labelColor = new Color(0.17f, 0.13f, 0.11f, 1f);
        [SerializeField] private Color litColor = new Color(0.72f, 0.45f, 0.08f, 1f);

        public bool IsLit { get; private set; }
        public string Hint => hint;

        private Button button;
        private bool hovered;
        private bool isHere;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(() => { if (!isHere) TownMap.Instance?.Go(location); });
        }

        public void Refresh()
        {
            var state = GameState.Instance;
            bool visible = location != null && location.IsUnlocked(state) && All(showWhen, state);
            gameObject.SetActive(visible);
            if (!visible) return;

            isHere = SceneManager.GetActiveScene().name == location.SceneName;
            if (isHere && state != null) state.SetFlag(location.VisitedFlag);
            bool visited = state != null && state.HasFlag(location.VisitedFlag);
            IsLit = !isHere && !visited && lightWhen.Count > 0 && All(lightWhen, state);

            if (here != null) here.SetActive(isHere);
            if (ring != null) ring.gameObject.SetActive(IsLit);
            if (label != null)
            {
                label.text = location.DisplayName;
                label.color = IsLit ? litColor : labelColor;
            }
            if (button == null) button = GetComponent<Button>();
            button.interactable = !isHere;
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            if (ring != null && ring.gameObject.activeSelf)
            {
                float p = (t * 0.8f) % 1f;
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.5f, p);
                var c = ring.color; c.a = 1f - p; ring.color = c;
            }
            if (pin != null)
                pin.rectTransform.localScale = Vector3.one * (hovered && !isHere ? 1.15f : 1f);
            if (label != null)
                label.fontStyle = hovered && !isHere ? FontStyles.Normal : FontStyles.Normal;
        }

        public void OnPointerEnter(PointerEventData e) => hovered = true;
        public void OnPointerExit(PointerEventData e) => hovered = false;
        private void OnDisable() => hovered = false;

        private static bool All(List<string> flags, GameState state)
        {
            foreach (var f in flags)
                if (!string.IsNullOrWhiteSpace(f) && (state == null || !state.HasFlag(f.Trim()))) return false;
            return true;
        }
    }
}
