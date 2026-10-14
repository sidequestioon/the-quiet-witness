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
    // room. Districts are dots (Map District): click one to read about it, click it again to go there.
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

        [Header("Info about a district (on click): a plate over the dot, like the envelope things")]
        [SerializeField] private RectTransform info;
        [Tooltip("The little triangle under the plate that points at the dot (its colour follows the plate).")]
        [SerializeField] private Image infoTail;
        [SerializeField] private TMP_Text infoTitle;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private TMP_Text infoAction;
        [Tooltip("The faint line over the action (Setup > Map: info plate like the envelope).")]
        [SerializeField] private GameObject infoDivider;
        [Tooltip("The row with the round icon and the action's name.")]
        [SerializeField] private GameObject infoActionRow;
        [Tooltip("The round walking-man icon: shown only when he can go there.")]
        [SerializeField] private GameObject infoGoIcon;
        [Tooltip("Next to the round icon when he can go there (a second click on the dot goes).")]
        [SerializeField] private string clickAgainText = "Click again  Go there";
        [SerializeField] private string hereText = "You are here";
        [SerializeField] private string lockedText = "Not yet";
        [Tooltip("Gap between the dot's centre and the tail of the plate (canvas units).")]
        [SerializeField] private float infoGap = 20f;

        [Header("Hint under the board")]
        [SerializeField] private TMP_Text hint;
        [HeroLine]
        [SerializeField] private string defaultHint = "map.default";
        [Tooltip("Seconds the hint stays after the map opens, then it fades away. 0 = stays.")]
        [SerializeField] private float hintTime = 4f;
        [SerializeField] private float hintFadeTime = 0.8f;

        [Header("Preview outside Play mode")]
        [Tooltip("Shows the map with the info box over a district, so it can be styled right here: the box is " +
                 "the object Info (its picture and colour; Width on its Rect Transform; Padding and Spacing on " +
                 "Vertical Layout Group), with Title, Text and Action inside (font, size, colour).")]
        [SerializeField] private bool preview;
        [Tooltip("Which district the preview box shows. Empty = the first one.")]
        [SerializeField] private MapDistrict previewDistrict;

        [Header("Leaving for good")]
        [Tooltip("Scenes he may not come back to: going anywhere from here asks first.")]
        [SerializeField] private System.Collections.Generic.List<string> askWhenLeaving =
            new System.Collections.Generic.List<string> { "Office", "Darkroom" };
        [Tooltip("What he asks himself before leaving (from Hero Lines).")]
        [HeroLine]
        [SerializeField] private string leaveLine = "map.leave_office";
        [SerializeField] private string leaveYes = "Go";
        [SerializeField] private string leaveNo = "Stay";

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
        private bool asking;   // the "Am I done here?" question is up

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
                hintLeft = hintTime;
                SetHintAlpha(1f);
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
            if (!open || location == null || asking) return;
            UiSounds.Play(s => s.click);

            // Leaving the office for good: "Am I done here?" first.
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            var confirm = ConfirmPanel.Instance;
            var line = HeroLines.Find(leaveLine);
            if (askWhenLeaving.Contains(scene) && confirm != null && line != null && !string.IsNullOrWhiteSpace(line.text))
            {
                asking = true;
                HideInfo(null);
                confirm.Ask(string.Empty, line.text, leaveYes, leaveNo, () => Leave(location));
                return;
            }
            Leave(location);
        }

        private void Leave(LocationData location)
        {
            asking = false;
            Close(sound: false);
            if (SceneLoader.Instance != null) SceneLoader.Instance.Travel(location);
        }

        // ---- The info box ----

        public void ShowInfo(MapDistrict district)
        {
            if (!open || info == null || district == null) return;
            shownInfo = district;
            UiSounds.Play(s => s.click);
            string action = district.IsHere ? hereText : district.CanGo ? clickAgainText : district.Locked ? lockedText : string.Empty;
            FillInfo(district.Title, district.Description, action, district.CanGo);
            PlaceInfo(district);
        }

        private void FillInfo(string title, string text, string action, bool canGo)
        {
            if (infoTitle != null && infoTitle.text != title) infoTitle.text = title;
            if (infoText != null && infoText.text != text) infoText.text = text;
            bool on = !string.IsNullOrEmpty(action);
            if (infoAction != null)
            {
                if (infoAction.text != action) infoAction.text = action;
                if (infoAction.gameObject.activeSelf != on) infoAction.gameObject.SetActive(on);
            }
            SetActive(infoDivider, on);
            SetActive(infoActionRow, on);
            SetActive(infoGoIcon, on && canGo);
            if (!info.gameObject.activeSelf) info.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(info);
        }

        private void PlaceInfo(MapDistrict district)
        {
            // Over the dot (under it when there's no room, or as the district says), kept on screen;
            // the tail points at the dot.
            var parent = (RectTransform)info.parent;
            Vector2 at = parent.InverseTransformPoint(district.transform.position);
            if (info.pivot != new Vector2(0.5f, 0.5f)) info.pivot = new Vector2(0.5f, 0.5f);
            Vector2 size = info.rect.size;
            Vector2 half = parent.rect.size / 2f;
            float tail = infoTail != null ? infoTail.rectTransform.rect.height : 0f;
            float offset = infoGap + tail + size.y / 2f;
            bool below = district.Side == Cutscenes.CaptionSide.Below ||
                         (district.Side == Cutscenes.CaptionSide.Auto && at.y + offset + size.y / 2f > half.y - 16f);
            Vector2 pos = at + Vector2.up * (below ? -offset : offset) + district.PlateOffset;
            pos.x = Mathf.Clamp(pos.x, -half.x + size.x / 2f + 16f, half.x - size.x / 2f - 16f);
            pos.y = Mathf.Clamp(pos.y, -half.y + size.y / 2f + 16f, half.y - size.y / 2f - 16f);
            pos = new Vector2(Mathf.Round(pos.x / 4f) * 4f, Mathf.Round(pos.y / 4f) * 4f);   // on the 4x pixel grid
            if (info.anchoredPosition != pos) info.anchoredPosition = pos;

            if (infoTail == null) return;
            var t = infoTail.rectTransform;
            float tx = Mathf.Clamp(at.x - pos.x, -size.x / 2f + 16f, size.x / 2f - 16f);
            var tp = new Vector2(Mathf.Round(tx / 4f) * 4f, below ? size.y / 2f + tail / 2f : -size.y / 2f - tail / 2f);
            if (t.anchoredPosition != tp) t.anchoredPosition = tp;
            var ts = new Vector3(1f, below ? -1f : 1f, 1f);
            if (t.localScale != ts) t.localScale = ts;
            if (info.TryGetComponent<Image>(out var plate) && infoTail.color != plate.color) infoTail.color = plate.color;
        }

        // ---- The hint under the board: shown when the map opens, then fades away ----

        private float hintLeft;
        private CanvasGroup hintGroup;

        private void SetHintAlpha(float a)
        {
            if (hint == null) return;
            if (hintGroup == null)
            {
                var plate = hint.transform.parent.gameObject;
                if (!plate.TryGetComponent(out hintGroup)) hintGroup = plate.AddComponent<CanvasGroup>();
                hintGroup.blocksRaycasts = false;
            }
            hintGroup.alpha = a;
        }

        private void FadeHint()
        {
            if (hint == null || hintTime <= 0f || !hint.transform.parent.gameObject.activeSelf) return;
            hintLeft -= Time.unscaledDeltaTime;
            if (hintLeft > 0f) return;
            float a = 1f + hintLeft / Mathf.Max(0.01f, hintFadeTime);
            SetHintAlpha(Mathf.Clamp01(a));
            if (a <= 0f) hint.transform.parent.gameObject.SetActive(false);
        }

        private static void SetActive(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        public bool IsShowing(MapDistrict district) => district != null && shownInfo == district;

        // A dot was clicked this frame (so the click doesn't also close its plate).
        private int clickFrame = -1;
        public void Clicked() => clickFrame = Time.frameCount;

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
            // Esc closes the map, but not on the frame it closed the question box.
            bool confirmOpen = ConfirmPanel.Instance != null && ConfirmPanel.Instance.IsOpen;
            if (open && !confirmOpen && !asking && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (shownInfo != null) HideInfo(null);   // Esc closes the plate first, then the map
                else Close();
            }
            if (!confirmOpen) asking = false;
            if (open) FadeHint();

            float target = open ? 1f : 0f;
            if (Mathf.Approximately(shown, target)) return;
            shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime));
            group.alpha = shown;
            if (dim != null) { var c = dim.color; c.a = dimAlpha * shown; dim.color = c; }
            ScreenBlur.Set(blurProfile, shown);
            if (board != null) board.localScale = Vector3.one * boardScale * Mathf.Lerp(0.96f, 1f, shown);
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || Instance != this || !open || shownInfo == null || asking) return;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasReleasedThisFrame && clickFrame != Time.frameCount) HideInfo(null);
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
            FillInfo(district.Title, district.Description, clickAgainText, true);
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
