using System.Collections.Generic;
using System.Text;
using QuietWitness.Core;
using QuietWitness.Dialogue;
using QuietWitness.Map;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace QuietWitness.UI
{
    // The case file: evidence cards and the log of everything said.
    // J opens and closes it (Esc closes). New clues are marked NEW until looked at.
    public class JournalPanel : MonoBehaviour
    {
        public static JournalPanel Instance { get; private set; }

        [SerializeField] private GameObject window;

        [Header("Tabs")]
        [SerializeField] private Button evidenceTab;
        [SerializeField] private Button conversationsTab;
        [SerializeField] private GameObject evidencePage;
        [SerializeField] private GameObject conversationsPage;

        [Header("Evidence")]
        [Tooltip("Hidden list line that gets cloned for every clue.")]
        [SerializeField] private DialogueChoiceButton clueTemplate;
        [SerializeField] private TMP_Text emptyEvidence;
        [SerializeField] private Image clueIcon;
        [SerializeField] private TMP_Text clueTitle;
        [Tooltip("'Key evidence' label, shown for key clues.")]
        [SerializeField] private GameObject keyLabel;
        [SerializeField] private TMP_Text clueDescription;

        [Header("Conversations")]
        [SerializeField] private TMP_Text logText;
        [SerializeField] private ScrollRect logScroll;

        private readonly List<DialogueChoiceButton> clueButtons = new List<DialogueChoiceButton>();
        private ClueData selected;

        public bool IsOpen => window.activeSelf;

        public static string SeenFlag(ClueData clue) => "seen:" + clue.Flag;

        private void Awake()
        {
            if (Instance != null && Instance != this) return; // duplicate Systems, removed by its root
            Instance = this;
            clueTemplate.gameObject.SetActive(false);
            evidenceTab.onClick.AddListener(ShowEvidence);
            conversationsTab.onClick.AddListener(ShowConversations);
            window.SetActive(false);
        }

        private void Update()
        {
            if (Instance != this || Keyboard.current == null) return;
            var keyboard = Keyboard.current;

            if (IsOpen)
            {
                if (keyboard.jKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame) Close();
            }
            else if (keyboard.jKey.wasPressedThisFrame)
            {
                Open();
            }
        }

        public void Open()
        {
            if (IsOpen || InputBlocker.IsBlocked) return;
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsTravelling) return;

            window.SetActive(true);
            InputBlocker.Push();
            ShowEvidence();
        }

        public void Close()
        {
            if (!IsOpen) return;
            window.SetActive(false);
            InputBlocker.Pop();
        }

        // ---- Evidence ----

        private void ShowEvidence()
        {
            evidencePage.SetActive(true);
            conversationsPage.SetActive(false);
            evidenceTab.interactable = false;
            conversationsTab.interactable = true;

            var clues = FoundClues();
            emptyEvidence.gameObject.SetActive(clues.Count == 0);
            Select(clues.Count > 0 ? clues[0] : null); // newest first
        }

        private void Select(ClueData clue)
        {
            selected = clue;
            if (clue != null && GameState.Instance != null) GameState.Instance.SetFlag(SeenFlag(clue));

            clueTitle.text = clue != null ? clue.Title : string.Empty;
            clueDescription.text = clue != null ? clue.Description : string.Empty;
            keyLabel.SetActive(clue != null && clue.IsKey);
            clueIcon.sprite = clue != null ? clue.Icon : null;
            clueIcon.gameObject.SetActive(clue != null && clue.Icon != null);

            RebuildList();
        }

        private void RebuildList()
        {
            var clues = FoundClues();
            var state = GameState.Instance;

            for (int i = 0; i < clues.Count; i++)
            {
                while (clueButtons.Count <= i)
                    clueButtons.Add(Instantiate(clueTemplate, clueTemplate.transform.parent));

                var clue = clues[i];
                string label = clue.IsKey ? $"<b>{clue.Title}</b>" : clue.Title;
                if (state != null && !state.HasFlag(SeenFlag(clue))) label += "  <color=#B3261E>NEW</color>";
                if (clue == selected) label = "> " + label;

                clueButtons[i].gameObject.SetActive(true);
                clueButtons[i].Setup(label, () => Select(clue));
            }
            for (int i = clues.Count; i < clueButtons.Count; i++)
                clueButtons[i].gameObject.SetActive(false);
        }

        private static List<ClueData> FoundClues()
        {
            var list = new List<ClueData>();
            if (GameState.Instance == null) return list;
            var found = GameState.Instance.FoundClues;
            for (int i = found.Count - 1; i >= 0; i--) list.Add(found[i]); // newest first
            return list;
        }

        // ---- Conversations ----

        private void ShowConversations()
        {
            evidencePage.SetActive(false);
            conversationsPage.SetActive(true);
            evidenceTab.interactable = true;
            conversationsTab.interactable = false;

            var history = DialogueRunner.Instance != null ? DialogueRunner.Instance.History : null;
            if (history == null || history.Count == 0)
            {
                logText.text = "<i>No conversations yet.</i>";
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var line in history)
                {
                    if (line.Text == DialogueRunner.Separator) sb.Append("\n<align=center>* * *</align>\n");
                    else if (line.IsNarration) sb.Append("<i>").Append(line.Text).Append("</i>\n");
                    else sb.Append("<b>").Append(line.Speaker).Append(":</b> ").Append(line.Text).Append('\n');
                }
                logText.text = sb.ToString();
            }

            Canvas.ForceUpdateCanvases();
            logScroll.verticalNormalizedPosition = 0f; // newest at the bottom
        }
    }
}
