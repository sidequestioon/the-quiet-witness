using System.Collections.Generic;
using QuietWitness.Core;
using QuietWitness.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QuietWitness.Map
{
    // A district on the town map: a dot you can hover to read about the place (Title, Description),
    // and click to go there if it has a Location. Without a Location it is only for reading.
    // Lit (yellow ring) while Light When flags are set and the place isn't visited yet:
    // where the story goes next; its Hint (a Hero Lines line) is shown under the board.
    // Move the dot on the board by its Rect Transform (board pixels from the centre).
    public class MapDistrict : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private string title = "DOWNTOWN";
        [TextArea(2, 6)]
        [SerializeField] private string description;
        [Tooltip("Where clicking takes you. Empty = only to read about.")]
        [SerializeField] private LocationData location;
        [Tooltip("The dot shows only once these flags are set. Empty = always.")]
        [SerializeField] private List<string> showWhen = new List<string>();
        [Tooltip("Lit while all of these are set and the place isn't visited yet. Empty = never lit.")]
        [SerializeField] private List<string> lightWhen = new List<string>();
        [Tooltip("The hero's hint under the board while this dot is lit (from Hero Lines).")]
        [HeroLine]
        [SerializeField] private string hint;

        [Header("Its info plate (like the envelope things)")]
        [Tooltip("Where the plate goes: Auto = over the dot (under it when there is no room).")]
        [SerializeField] private Cutscenes.CaptionSide side = Cutscenes.CaptionSide.Auto;
        [Tooltip("Moves this dot's plate from its usual place (canvas units, x right, y up).")]
        [SerializeField] private Vector2 plateOffset = Vector2.zero;

        [Header("A paper to read (a newspaper on the board)")]
        [Tooltip("Its pages, in order: a click on the dot opens them to read (arrows turn the pages). " +
                 "Empty = the dot only shows its plate.")]
        [SerializeField] private Sprite[] readPages;

        [Header("Parts")]
        [SerializeField] private Image dot;
        [SerializeField] private Image ring;
        [SerializeField] private Sprite dotSprite;
        [SerializeField] private Sprite dotHover;
        [SerializeField] private Sprite dotHere;

        public string Title => title;
        public string Description => description;
        public string Hint => hint;
        public LocationData Location => location;
        public Cutscenes.CaptionSide Side => side;
        public Vector2 PlateOffset => plateOffset;
        public bool Readable => readPages != null && readPages.Length > 0;
        public bool IsLit { get; private set; }
        public bool IsHere { get; private set; }
        public bool CanGo { get; private set; }
        public bool Locked => location != null && !CanGo && !IsHere;

        private bool hovered;

        public void Refresh()
        {
            var state = GameState.Instance;
            bool visible = All(showWhen, state);
            gameObject.SetActive(visible);
            if (!visible) return;

            IsHere = location != null && SceneManager.GetActiveScene().name == location.SceneName;
            if (IsHere && state != null) state.SetFlag(location.VisitedFlag);
            bool visited = location != null && state != null && state.HasFlag(location.VisitedFlag);
            CanGo = location != null && !IsHere && location.IsUnlocked(state);
            IsLit = !IsHere && !visited && lightWhen.Count > 0 && All(lightWhen, state);
            if (ring != null) ring.gameObject.SetActive(IsLit);
            hovered = false;
        }

        private void Update()
        {
            if (ring != null && ring.gameObject.activeSelf)
            {
                float p = (Time.unscaledTime * 0.8f) % 1f;
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.4f, p);
                var c = ring.color; c.a = 1f - p; ring.color = c;
            }
            if (dot != null)
            {
                dot.sprite = IsHere && dotHere != null ? dotHere : hovered && dotHover != null ? dotHover : dotSprite;
                dot.rectTransform.localScale = Vector3.one * (hovered ? 1.2f : 1f);
            }
        }

        // Like the things from the envelope: the dot stays where it is; hovering only lights it up,
        // a click opens its plate, a second click on the same dot goes there (if he can).
        public void OnPointerEnter(PointerEventData e) => hovered = true;

        public void OnPointerExit(PointerEventData e) => hovered = false;

        public void OnPointerClick(PointerEventData e)
        {
            var map = TownMap.Instance;
            if (map == null) return;
            map.Clicked();
            if (Readable && UI.PaperReader.Instance != null)
            {
                map.HideInfo(null);
                hovered = false;
                UI.PaperReader.Instance.Show(readPages);
                return;
            }
            if (map.IsShowing(this))
            {
                if (CanGo) map.Go(location);
            }
            else map.ShowInfo(this);
        }

        private void OnDisable() => hovered = false;

        private static bool All(List<string> flags, GameState state)
        {
            foreach (var f in flags)
                if (!string.IsNullOrWhiteSpace(f) && (state == null || !state.HasFlag(f.Trim()))) return false;
            return true;
        }
    }
}
