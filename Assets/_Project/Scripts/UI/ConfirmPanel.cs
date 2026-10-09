using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace QuietWitness.UI
{
    // A small yes/no question box. Enter = yes, Esc = no.
    public class ConfirmPanel : MonoBehaviour
    {
        public static ConfirmPanel Instance { get; private set; }

        [SerializeField] private GameObject window;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text message;
        [SerializeField] private Button yesButton;
        [SerializeField] private TMP_Text yesLabel;
        [SerializeField] private Button noButton;
        [SerializeField] private TMP_Text noLabel;

        private Action onYes;
        private int openFrame;

        public bool IsOpen => window.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this) return; // duplicate Systems, removed by its root
            Instance = this;
            yesButton.onClick.AddListener(Yes);
            noButton.onClick.AddListener(No);
            window.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount == openFrame || Keyboard.current == null) return;
            if (Keyboard.current.enterKey.wasPressedThisFrame) Yes();
            else if (Keyboard.current.escapeKey.wasPressedThisFrame) No();
        }

        // noText = null shows only one button (a plain message).
        public void Ask(string titleText, string messageText, string yesText, string noText, Action yes)
        {
            if (IsOpen) return;
            onYes = yes;
            openFrame = Time.frameCount;

            title.text = titleText;
            message.text = messageText;
            yesLabel.text = yesText;
            noButton.gameObject.SetActive(!string.IsNullOrEmpty(noText));
            noLabel.text = noText ?? string.Empty;

            window.SetActive(true);
            QuietWitness.Core.InputBlocker.Push();
        }

        private void Yes()
        {
            var action = onYes;
            Hide();
            action?.Invoke();
        }

        private void No() => Hide();

        private void Hide()
        {
            if (!IsOpen) return;
            window.SetActive(false);
            onYes = null;
            QuietWitness.Core.InputBlocker.Pop();
        }
    }
}
