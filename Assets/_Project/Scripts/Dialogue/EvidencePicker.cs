using System;
using System.Collections.Generic;
using QuietWitness.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace QuietWitness.Dialogue
{
    // "Present evidence" window: the clues found so far, pick one and present it.
    // Esc or Back cancels. Opened by DialoguePanel.
    public class EvidencePicker : MonoBehaviour
    {
        [SerializeField] private GameObject window;
        [Tooltip("Hidden list line that gets cloned for every clue.")]
        [SerializeField] private DialogueChoiceButton itemTemplate;
        [SerializeField] private TMP_Text emptyText;

        [Header("Selected clue")]
        [SerializeField] private Image clueIcon;
        [SerializeField] private TMP_Text clueTitle;
        [SerializeField] private TMP_Text clueDescription;
        [SerializeField] private Button presentButton;
        [SerializeField] private Button backButton;

        private readonly List<DialogueChoiceButton> items = new List<DialogueChoiceButton>();
        private Action<ClueData> onPresent;
        private Action onCancel;
        private ClueData selected;
        private int openFrame;

        public bool IsOpen => window.activeSelf;

        private void Awake()
        {
            itemTemplate.gameObject.SetActive(false);
            presentButton.onClick.AddListener(Confirm);
            backButton.onClick.AddListener(Cancel);
            window.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount == openFrame || Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) Cancel();
            else if (Keyboard.current.enterKey.wasPressedThisFrame) Confirm();
        }

        public void Open(Action<ClueData> presentCallback, Action cancelCallback)
        {
            onPresent = presentCallback;
            onCancel = cancelCallback;
            openFrame = Time.frameCount;

            IReadOnlyList<ClueData> clues = GameState.Instance != null
                ? GameState.Instance.FoundClues
                : (IReadOnlyList<ClueData>)Array.Empty<ClueData>();
            for (int i = 0; i < clues.Count; i++)
            {
                while (items.Count <= i)
                    items.Add(Instantiate(itemTemplate, itemTemplate.transform.parent));
                var clue = clues[i];
                items[i].gameObject.SetActive(true);
                items[i].Setup(clue.Title, () => Select(clue));
            }
            for (int i = clues.Count; i < items.Count; i++)
                items[i].gameObject.SetActive(false);

            emptyText.gameObject.SetActive(clues.Count == 0);
            Select(clues.Count > 0 ? clues[0] : null);
            window.SetActive(true);
        }

        private void Select(ClueData clue)
        {
            selected = clue;
            clueTitle.text = clue != null ? clue.Title : string.Empty;
            clueDescription.text = clue != null ? clue.Description : string.Empty;
            clueIcon.sprite = clue != null ? clue.Icon : null;
            clueIcon.gameObject.SetActive(clue != null && clue.Icon != null);
            presentButton.interactable = clue != null;
        }

        private void Confirm()
        {
            if (selected == null) return;
            var clue = selected;
            window.SetActive(false);
            onPresent?.Invoke(clue);
        }

        public void Cancel()
        {
            if (!IsOpen) return;
            window.SetActive(false);
            onCancel?.Invoke();
        }
    }
}
