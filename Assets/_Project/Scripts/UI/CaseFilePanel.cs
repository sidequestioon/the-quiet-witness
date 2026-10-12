using System.Collections.Generic;
using System.Text;
using QuietWitness.Audio;
using QuietWitness.Core;
using QuietWitness.Map;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace QuietWitness.UI
{
    // The case file. The small icon at the top of the screen opens it (or Tab / J); Esc closes.
    // The room behind blurs and darkens (like the envelope close-up). On the left: Objectives and Inventory.
    //  - Objectives: the main goal in yellow in the middle, its smaller steps under it (Objectives asset, by flags).
    //  - Inventory: what was found, one thing in the middle with its name and what the hero said about it.
    //    A / D, the arrows, the mouse wheel or a click on a thing at the side turn them.
    // A yellow dot on the icon and on the menu line = something new there.
    public class CaseFilePanel : MonoBehaviour
    {
        public enum Page { Objectives, Inventory }

        public static CaseFilePanel Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.open;

        [Header("Icon at the top")]
        [Tooltip("The icon (and Tab / J) work only once this flag is set. Empty = always.")]
        [SerializeField] private string showAfterFlag = "prologue:envelope_opened";
        [SerializeField] private CanvasGroup hud;
        [SerializeField] private Button hudButton;
        [SerializeField] private Image hudIcon;
        [SerializeField] private Sprite hudSprite;
        [SerializeField] private Sprite hudHover;
        [SerializeField] private GameObject hudBadge;

        [Header("Screen")]
        [SerializeField] private CanvasGroup window;
        [SerializeField] private Image dim;
        [Range(0f, 1f)]
        [SerializeField] private float dimAlpha = 0.65f;
        [Tooltip("Blur of the room behind (the same profile as the envelope close-up).")]
        [SerializeField] private VolumeProfile blurProfile;
        [SerializeField] private float fadeTime = 0.2f;
        [Tooltip("Big title at the top-left: the text of the chosen menu line.")]
        [SerializeField] private TMP_Text title;
        [Tooltip("Key hints at the bottom-right.")]
        [SerializeField] private TMP_Text hints;
        [SerializeField] private string objectivesHints = "Esc  Close";
        [SerializeField] private string inventoryHints = "A / D  Browse      Esc  Close";

        [Header("Menu on the left")]
        [SerializeField] private CaseFileTab objectivesTab;
        [SerializeField] private CaseFileTab inventoryTab;
        [SerializeField] private Color tabOn = new Color(0.96f, 0.94f, 0.91f, 1f);
        [SerializeField] private Color tabOff = new Color(0.96f, 0.94f, 0.91f, 0.4f);
        [SerializeField] private Color tabHover = new Color(0.96f, 0.94f, 0.91f, 0.75f);

        [Header("Objectives")]
        [SerializeField] private ObjectiveList objectives;
        [SerializeField] private GameObject objectivesPage;
        [SerializeField] private TMP_Text mainGoal;
        [SerializeField] private TMP_Text steps;
        [SerializeField] private string noGoals = "Nothing to do. For now.";
        [Tooltip("Colour of crossed-out goals.")]
        [SerializeField] private Color doneColor = new Color(0.96f, 0.94f, 0.91f, 0.35f);

        [Header("Inventory")]
        [SerializeField] private GameObject inventoryPage;
        [Tooltip("Hidden picture that gets copied for every thing found.")]
        [SerializeField] private Image itemTemplate;
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private TMP_Text itemText;
        [SerializeField] private TMP_Text counter;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text emptyInventory;
        [Tooltip("Distance between the things in the row (canvas units).")]
        [SerializeField] private float spacing = 440f;
        [Tooltip("The thing in the middle fits into this box, at a whole pixel scale (2x, 3x, 4x...).")]
        [SerializeField] private Vector2 itemBox = new Vector2(380f, 380f);
        [SerializeField] private int maxPixelScale = 6;
        [Tooltip("Size of the things at the sides.")]
        [Range(0.2f, 1f)]
        [SerializeField] private float sideScale = 0.55f;
        [Range(0f, 1f)]
        [SerializeField] private float sideAlpha = 0.3f;
        [SerializeField] private float turnSpeed = 12f;

        [Header("Sounds")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip turnSound;
        [Range(0f, 1f)]
        [SerializeField] private float soundVolume = 0.6f;

        private bool main;            // false on a second copy of Systems (it gets removed)
        private bool open;
        private float shownAmount;    // 0 = closed, 1 = open
        private Page page = Page.Objectives;
        private Volume blur;
        private Camera postCamera;
        private bool postWasOn;
        private bool hudHovered;

        private readonly List<ClueData> clues = new List<ClueData>();
        private readonly List<Image> pictures = new List<Image>();
        private readonly List<ObjectiveList.Goal> stepList = new List<ObjectiveList.Goal>();
        private int index;
        private float shownIndex;
        private float textIn = 1f;
        private float scrollSum;

        private void Awake()
        {
            if (hud != null) { hud.alpha = 0f; hud.blocksRaycasts = false; }
            if (window != null) { window.alpha = 0f; window.blocksRaycasts = false; window.interactable = false; }
            if (Instance != null && Instance != this) return;   // duplicate Systems, removed by its root
            Instance = this;
            main = true;

            if (itemTemplate != null) itemTemplate.gameObject.SetActive(false);
            if (hudButton != null) hudButton.onClick.AddListener(Toggle);
            if (objectivesTab != null && objectivesTab.button != null) objectivesTab.button.onClick.AddListener(() => ShowPage(Page.Objectives));
            if (inventoryTab != null && inventoryTab.button != null) inventoryTab.button.onClick.AddListener(() => ShowPage(Page.Inventory));
            if (prevButton != null) prevButton.onClick.AddListener(() => Turn(-1));
            if (nextButton != null) nextButton.onClick.AddListener(() => Turn(1));
            AddHover(hudButton != null ? hudButton.gameObject : null);
        }

        private void OnDestroy()
        {
            if (!main) return;
            if (blur != null) Destroy(blur.gameObject);
            if (Instance == this) Instance = null;
        }

        private static bool Travelling => SceneLoader.Instance != null && SceneLoader.Instance.IsTravelling;

        private bool Unlocked =>
            string.IsNullOrWhiteSpace(showAfterFlag) ||
            (GameState.Instance != null && GameState.Instance.HasFlag(showAfterFlag.Trim()));

        private bool CanOpen => Unlocked && !InputBlocker.IsBlocked && !Travelling;

        private void Update()
        {
            if (!main) return;
            float dt = Time.unscaledDeltaTime;

            // The icon: only when the hero is free to walk around (or while the case file is open).
            bool hudOn = open || CanOpen;
            if (hud != null)
            {
                hud.alpha = Mathf.MoveTowards(hud.alpha, hudOn ? 1f : 0f, dt / Mathf.Max(0.01f, fadeTime));
                hud.blocksRaycasts = hud.interactable = hudOn;
            }
            if (hudIcon != null) hudIcon.sprite = hudHovered && hudHover != null ? hudHover : hudSprite;
            bool anyNew = !open && Unlocked && (NewGoals() || NewClues());
            if (hudBadge != null)
            {
                hudBadge.SetActive(anyNew);
                if (anyNew) hudBadge.transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(Time.unscaledTime * 5f));
            }

            ReadKeys();

            // Fade the screen, the dark and the blur together.
            shownAmount = Mathf.MoveTowards(shownAmount, open ? 1f : 0f, dt / Mathf.Max(0.01f, fadeTime));
            if (window != null) window.alpha = shownAmount;
            if (dim != null) { var c = dim.color; c.a = dimAlpha * shownAmount; dim.color = c; }
            if (blur != null) blur.weight = shownAmount;
            if (!open && shownAmount <= 0f) RestorePost();

            if (open)
            {
                StyleTabs();
                if (page == Page.Inventory) AnimateInventory(dt);
            }
        }

        private void ReadKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            bool toggle = kb.tabKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame;

            if (!open)
            {
                if (toggle && CanOpen) Open();
                return;
            }
            if (toggle || kb.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) ShowPage(Page.Objectives);
            else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) ShowPage(Page.Inventory);
            else if (page == Page.Inventory)
            {
                if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) Turn(-1);
                else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) Turn(1);
            }

            // Mouse wheel turns the things too.
            if (page == Page.Inventory && Mouse.current != null)
            {
                float y = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(y) < 0.01f) scrollSum = 0f;
                scrollSum += y;
                if (Mathf.Abs(scrollSum) >= 1f)
                {
                    Turn(scrollSum > 0f ? -1 : 1);
                    scrollSum = 0f;
                }
            }
        }

        // ---- Open / close ----

        public void Toggle()
        {
            if (open) Close();
            else Open();
        }

        public void Open()
        {
            if (open || !CanOpen) return;
            open = true;
            InputBlocker.Push();
            if (window != null) { window.blocksRaycasts = true; window.interactable = true; }
            TurnOnBlur();
            Sfx.Play(openSound, soundVolume);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            // Open where the new things are.
            if (NewGoals()) ShowPage(Page.Objectives, force: true);
            else if (NewClues()) ShowPage(Page.Inventory, force: true);
            else ShowPage(page, force: true);
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            if (window != null) { window.blocksRaycasts = false; window.interactable = false; }
            InputBlocker.Pop();
            Sfx.Play(closeSound, soundVolume);
        }

        private void TurnOnBlur()
        {
            if (blur == null && blurProfile != null)
            {
                var go = new GameObject("CaseFile Blur");
                DontDestroyOnLoad(go);
                blur = go.AddComponent<Volume>();
                blur.isGlobal = true;
                blur.priority = 20f;
                blur.sharedProfile = blurProfile;
                blur.weight = 0f;
            }
            // The blur needs post-processing on the camera; switched back off after closing if it was off.
            var cam = Camera.main;
            if (cam != null && postCamera != cam && cam.TryGetComponent<UniversalAdditionalCameraData>(out var data))
            {
                RestorePost();
                postCamera = cam;
                postWasOn = data.renderPostProcessing;
                data.renderPostProcessing = true;
            }
        }

        private void RestorePost()
        {
            if (postCamera != null && postCamera.TryGetComponent<UniversalAdditionalCameraData>(out var data))
                data.renderPostProcessing = postWasOn;
            postCamera = null;
        }

        // ---- Pages ----

        public void ShowPage(Page p) => ShowPage(p, false);

        private void ShowPage(Page p, bool force)
        {
            if (!force && p == page) return;
            page = p;
            if (objectivesPage != null) objectivesPage.SetActive(p == Page.Objectives);
            if (inventoryPage != null) inventoryPage.SetActive(p == Page.Inventory);
            var tab = p == Page.Objectives ? objectivesTab : inventoryTab;
            if (title != null && tab != null && tab.label != null) title.text = tab.label.text;
            if (hints != null) hints.text = p == Page.Objectives ? objectivesHints : inventoryHints;

            if (p == Page.Objectives) RefreshObjectives();
            else RefreshInventory();
            StyleTabs();
        }

        private void StyleTabs()
        {
            Style(objectivesTab, page == Page.Objectives, page != Page.Objectives && NewGoals());
            Style(inventoryTab, page == Page.Inventory, page != Page.Inventory && NewClues());
        }

        private void Style(CaseFileTab tab, bool on, bool isNew)
        {
            if (tab == null) return;
            if (tab.label != null) tab.label.color = on ? tabOn : tab.Hovered ? tabHover : tabOff;
            if (tab.marker != null) tab.marker.enabled = on;
            if (tab.badge != null) tab.badge.SetActive(isNew);
        }

        // ---- Objectives ----

        private void RefreshObjectives()
        {
            if (objectives == null) return;
            var goal = objectives.Current(stepList);
            if (mainGoal != null)
            {
                if (goal == null) mainGoal.text = noGoals;
                else mainGoal.text = ObjectiveList.Done(goal) ? Crossed(goal.text) : goal.text;
            }
            if (steps != null)
            {
                var sb = new StringBuilder();
                foreach (var step in stepList)
                {
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(ObjectiveList.Done(step) ? Crossed(step.text) : step.text);
                }
                steps.text = sb.ToString();
            }

            // Everything on the page has been seen now.
            var state = GameState.Instance;
            if (state == null) return;
            if (goal != null) state.SetFlag(goal.SeenFlag);
            foreach (var step in stepList) state.SetFlag(step.SeenFlag);
        }

        private string Crossed(string text) => $"<color=#{ColorUtility.ToHtmlStringRGBA(doneColor)}><s>{text}</s></color>";

        private bool NewGoals()
        {
            if (objectives == null || GameState.Instance == null) return false;
            foreach (var g in objectives.goals)
                if (ObjectiveList.Shown(g) && !GameState.Instance.HasFlag(g.SeenFlag)) return true;
            return false;
        }

        // ---- Inventory ----

        private bool NewClues()
        {
            var state = GameState.Instance;
            if (state == null) return false;
            foreach (var clue in state.FoundClues)
                if (clue != null && !state.HasFlag(JournalPanel.SeenFlag(clue))) return true;
            return false;
        }

        private void RefreshInventory()
        {
            clues.Clear();
            var state = GameState.Instance;
            if (state != null)
                foreach (var clue in state.FoundClues)
                    if (clue != null) clues.Add(clue);

            // One picture per thing, in the order they were found.
            while (pictures.Count < clues.Count && itemTemplate != null)
            {
                var copy = Instantiate(itemTemplate, itemTemplate.transform.parent);
                int i = pictures.Count;
                var button = copy.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(() => Select(i));
                pictures.Add(copy);
            }
            for (int i = 0; i < pictures.Count; i++)
            {
                bool used = i < clues.Count;
                pictures[i].gameObject.SetActive(used);
                if (!used) continue;
                var sprite = clues[i].Icon;
                pictures[i].sprite = sprite;
                pictures[i].enabled = sprite != null;
                if (sprite != null)
                {
                    var size = sprite.rect.size;
                    int scale = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(itemBox.x / size.x, itemBox.y / size.y)), 1, Mathf.Max(1, maxPixelScale));
                    pictures[i].rectTransform.sizeDelta = size * scale;
                }
            }

            bool empty = clues.Count == 0;
            if (emptyInventory != null) emptyInventory.gameObject.SetActive(empty);
            if (itemName != null) itemName.gameObject.SetActive(!empty);
            if (itemText != null) itemText.gameObject.SetActive(!empty);
            if (counter != null) counter.gameObject.SetActive(!empty);

            // Start at the first thing not looked at yet.
            if (state != null)
                for (int i = 0; i < clues.Count; i++)
                    if (!state.HasFlag(JournalPanel.SeenFlag(clues[i]))) { index = i; break; }
            index = Mathf.Clamp(index, 0, Mathf.Max(0, clues.Count - 1));
            shownIndex = index;
            ShowItemText();
            AnimateInventory(0f);
        }

        private void Select(int i)
        {
            if (i == index || i < 0 || i >= clues.Count) return;
            index = i;
            Sfx.Play(turnSound, soundVolume);
            ShowItemText();
        }

        private void Turn(int direction)
        {
            if (!open || page != Page.Inventory) return;
            Select(Mathf.Clamp(index + direction, 0, clues.Count - 1));
        }

        private void ShowItemText()
        {
            bool any = clues.Count > 0;
            if (prevButton != null) prevButton.gameObject.SetActive(any && index > 0);
            if (nextButton != null) nextButton.gameObject.SetActive(any && index < clues.Count - 1);
            if (!any) return;
            var clue = clues[index];
            if (itemName != null) itemName.text = clue.Title;
            if (itemText != null) itemText.text = clue.Description != null ? clue.Description.Replace("\\n", "\n") : string.Empty;
            if (counter != null) counter.text = $"{index + 1} / {clues.Count}";
            textIn = 0f;
            GameState.Instance?.SetFlag(JournalPanel.SeenFlag(clue));
        }

        private void AnimateInventory(float dt)
        {
            shownIndex = dt > 0f ? Mathf.Lerp(shownIndex, index, 1f - Mathf.Exp(-turnSpeed * dt)) : index;
            if (Mathf.Abs(shownIndex - index) < 0.001f) shownIndex = index;

            for (int i = 0; i < clues.Count && i < pictures.Count; i++)
            {
                float offset = i - shownIndex;
                float away = Mathf.Abs(offset);
                float scale = Mathf.Lerp(1f, sideScale, Mathf.Clamp01(away));
                float alpha = away <= 1f ? Mathf.Lerp(1f, sideAlpha, away) : Mathf.Lerp(sideAlpha, 0f, away - 1f);
                var picture = pictures[i];
                picture.rectTransform.anchoredPosition = new Vector2(offset * spacing, 0f);
                picture.rectTransform.localScale = Vector3.one * scale;
                var c = picture.color; c.a = alpha; picture.color = c;
                picture.raycastTarget = alpha > 0.05f && i != index;   // click a thing at the side to bring it here
            }

            textIn = Mathf.MoveTowards(textIn, 1f, dt * 5f);   // the name and the words fade in after a turn
            SetAlpha(itemName, textIn);
            SetAlpha(itemText, textIn);
        }

        private static void SetAlpha(TMP_Text text, float a)
        {
            if (text != null) text.alpha = a;
        }

        // ---- Icon hover ----

        private void AddHover(GameObject go)
        {
            if (go == null) return;
            if (!go.TryGetComponent<EventTrigger>(out var trigger)) trigger = go.AddComponent<EventTrigger>();
            Add(trigger, EventTriggerType.PointerEnter, () => hudHovered = true);
            Add(trigger, EventTriggerType.PointerExit, () => hudHovered = false);
        }

        private static void Add(EventTrigger trigger, EventTriggerType type, System.Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }
    }
}
