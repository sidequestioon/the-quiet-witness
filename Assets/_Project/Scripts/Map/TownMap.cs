using QuietWitness.Audio;
using QuietWitness.Core;
using QuietWitness.UI;
using QuietWitness.World;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace QuietWitness.Map
{
    // The town map: the cork board from the office, in the middle of the screen over the blurred, darkened
    // room. Districts are dots (Map District): hover one to read about it, click to go there.
    // The place the story goes next has a pulsing yellow ring, and the hero's hint for it is shown under
    // the board. Esc closes. Opened like the old map (Map Panel.Open).
    [ExecuteAlways]
    public class TownMap : MonoBehaviour
    {
        public static TownMap Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.open;

        [Header("Screen")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image dim;
        [Range(0f, 1f)]
        [SerializeField] private float dimAlpha = 0.7f;
        [SerializeField] private VolumeProfile blurProfile;
        [SerializeField] private float fadeTime = 0.25f;
        [Tooltip("The board picture.")]
        [SerializeField] private RectTransform board;
        [Tooltip("How big the board is: 3 = each pixel of the picture is 3x3 (1440x810 on a 1920x1080 screen), " +
                 "4 = the whole screen. Whole numbers keep the pixels sharp.")]
        [Range(1, 6)]
        [SerializeField] private int boardScale = 3;

        [Header("Info about a district (on hover)")]
        [SerializeField] private RectTransform info;
        [SerializeField] private TMP_Text infoTitle;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private TMP_Text infoAction;
        [SerializeField] private string goText = "Click  Go there";
        [SerializeField] private string hereText = "You are here";
        [SerializeField] private string lockedText = "Not yet";
        [Tooltip("Gap between the dot and the info box (canvas units).")]
        [SerializeField] private float infoGap = 28f;

        [Header("Hint under the board")]
        [SerializeField] private TMP_Text hint;
        [HeroLine]
        [SerializeField] private string defaultHint = "map.default";

        [Header("Preview outside Play mode")]
        [Tooltip("Shows the map with the info box over a district, so it can be styled right here: the box is " +
                 "the object Info (its picture and colour; Width on its Rect Transform; Padding and Spacing on " +
                 "Vertical Layout Group), with Title, Text and Action inside (font, size, colour).")]
        [SerializeField] private bool preview;
        [Tooltip("Which district the preview box shows. Empty = the first one.")]
        [SerializeField] private MapDistrict previewDistrict;

        [Header("Sounds")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [Range(0f, 1f)]
        [SerializeField] private float soundVolume = 0.6f;

        private MapDistrict[] districts;
        private MapPin[] pins;
        private MapDistrict shownInfo;
        private bool open;
        private float shown;

        private void Awake()
        {
            if (!Application.isPlaying) return;
            if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
            if (info != null) info.gameObject.SetActive(false);
            if (Instance != null && Instance != this) return;   // duplicate Systems, removed by its root
            Instance = this;
            districts = GetComponentsInChildren<MapDistrict>(true);
            if (board != null) board.localScale = Vector3.one * boardScale;
            pins = GetComponentsInChildren<MapPin>(true);
        }

        public void Open()
        {
            if (open || Instance != this) return;
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsTravelling) return;
            open = true;
            InputBlocker.Push();
            group.blocksRaycasts = true;
            group.interactable = true;
            Sfx.Play(openSound, soundVolume);
            HideInfo(null);

            string hintLine = defaultHint;
            foreach (var d in districts)
            {
                d.Refresh();
                if (d.gameObject.activeSelf && d.IsLit && !string.IsNullOrWhiteSpace(d.Hint)) hintLine = d.Hint;
            }
            foreach (var p in pins)
            {
                p.Refresh();
                if (p.gameObject.activeSelf && p.IsLit && !string.IsNullOrWhiteSpace(p.Hint)) hintLine = p.Hint;
            }
            if (hint != null)
            {
                var line = HeroLines.Find(hintLine);
                hint.text = line != null ? line.text : string.Empty;
                hint.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(hint.text));
            }
        }

        public void Close(bool sound = true)
        {
            if (!open) return;
            open = false;
            HideInfo(null);
            group.blocksRaycasts = false;
            group.interactable = false;
            InputBlocker.Pop();
            if (sound) Sfx.Play(closeSound, soundVolume);
        }

        // Called by a district dot or a pin.
        public void Go(LocationData location)
        {
            if (!open || location == null) return;
            UiSounds.Play(s => s.click);
            Close(sound: false);
            if (SceneLoader.Instance != null) SceneLoader.Instance.Travel(location);
        }

        // ---- The info box ----

        public void ShowInfo(MapDistrict district)
        {
            if (!open || info == null || district == null) return;
            shownInfo = district;
            string action = district.IsHere ? hereText : district.CanGo ? goText : district.Locked ? lockedText : string.Empty;
            FillInfo(district.Title, district.Description, action);
            PlaceInfo(district);
        }

        private void FillInfo(string title, string text, string action)
        {
            if (infoTitle != null && infoTitle.text != title) infoTitle.text = title;
            if (infoText != null && infoText.text != text) infoText.text = text;
            if (infoAction != null)
            {
                if (infoAction.text != action) infoAction.text = action;
                bool on = !string.IsNullOrEmpty(action);
                if (infoAction.gameObject.activeSelf != on) infoAction.gameObject.SetActive(on);
            }
            if (!info.gameObject.activeSelf) info.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(info);
        }

        private void PlaceInfo(MapDistrict district)
        {

            // Next to the dot: to the right, or to the left when there's no room; kept on screen.
            var parent = (RectTransform)info.parent;
            Vector2 at = parent.InverseTransformPoint(district.transform.position);
            Vector2 size = info.rect.size;
            Vector2 half = parent.rect.size / 2f;
            bool right = at.x + infoGap + size.x < half.x - 16f;
            info.pivot = new Vector2(right ? 0f : 1f, 0.5f);
            Vector2 pos = at + new Vector2(right ? infoGap : -infoGap, 0f);
            pos.y = Mathf.Clamp(pos.y, -half.y + size.y / 2f + 16f, half.y - size.y / 2f - 16f);
            info.anchoredPosition = pos;
        }

        public void HideInfo(MapDistrict district)
        {
            if (info == null) return;
            if (district != null && district != shownInfo) return;
            shownInfo = null;
            info.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                EditorPreview();
                return;
            }
            if (Instance != this) return;
            if (open && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();

            float target = open ? 1f : 0f;
            if (Mathf.Approximately(shown, target)) return;
            shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime));
            group.alpha = shown;
            if (dim != null) { var c = dim.color; c.a = dimAlpha * shown; dim.color = c; }
            ScreenBlur.Set(blurProfile, shown);
            if (board != null) board.localScale = Vector3.one * boardScale * Mathf.Lerp(0.96f, 1f, shown);
        }

        // Outside Play mode: the board with the info box over a district, while Preview is on.
        private void EditorPreview()
        {
            if (group == null) return;
            float a = preview ? 1f : 0f;
            if (!Mathf.Approximately(group.alpha, a)) group.alpha = a;
            if (dim != null && !Mathf.Approximately(dim.color.a, dimAlpha * a)) { var c = dim.color; c.a = dimAlpha * a; dim.color = c; }
            if (board != null && board.localScale != Vector3.one * boardScale) board.localScale = Vector3.one * boardScale;
            if (info == null) return;
            if (!preview)
            {
                if (info.gameObject.activeSelf) info.gameObject.SetActive(false);
                return;
            }
            var district = previewDistrict != null ? previewDistrict : GetComponentInChildren<MapDistrict>(true);
            if (district == null) return;
            FillInfo(district.Title, district.Description, goText);
            PlaceInfo(district);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        }
#endif
    }
}
