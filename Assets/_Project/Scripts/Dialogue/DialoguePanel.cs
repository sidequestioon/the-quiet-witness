using System.Collections;
using System.Collections.Generic;
using QuietWitness.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace QuietWitness.Dialogue
{
    // The dialogue box at the bottom of the screen. Shows what DialogueRunner says:
    // types the line out, then waits for E / Space / Enter / click, or for a choice (click or 1-9).
    // When the story accepts evidence, "Present evidence" (P) opens the EvidencePicker.
    public class DialoguePanel : MonoBehaviour
    {
        [SerializeField] private GameObject window;
        [SerializeField] private GameObject speakerBox;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text lineText;
        [Tooltip("Small 'E' hint, shown when the line is done.")]
        [SerializeField] private GameObject continueHint;

        [Header("Choices")]
        [Tooltip("Hidden button that gets cloned for every choice.")]
        [SerializeField] private DialogueChoiceButton choiceTemplate;

        [Header("Evidence & silence")]
        [SerializeField] private EvidencePicker picker;
        [Tooltip("Said when the player presents a clue the story has no answer for.")]
        [SerializeField] private string wrongEvidenceLine = "That doesn't prove anything.";
        [Tooltip("How long the investigator stays silent, in seconds.")]
        [SerializeField] private float silencePause = 1.5f;

        [Header("Typing")]
        [SerializeField] private float charactersPerSecond = 50f;

        private readonly List<DialogueChoiceButton> choiceButtons = new List<DialogueChoiceButton>();
        private readonly List<UnityAction> numberedActions = new List<UnityAction>();
        private DialogueRunner runner;
        private bool typing;
        private bool choicesShown;
        private bool presentShown;
        private bool busy; // silence pause or evidence picker
        private float revealed;
        private int totalCharacters;
        private int lineFrame;

        private void Start()
        {
            choiceTemplate.gameObject.SetActive(false);
            window.SetActive(false);

            runner = DialogueRunner.Instance;
            if (runner == null || runner.transform.root != transform.root) return; // duplicate Systems

            runner.Started += OnStarted;
            runner.LineShown += OnLine;
            runner.ChoicesReady += OnChoicesReady;
            runner.Ended += OnEnded;
        }

        private void OnDestroy()
        {
            if (runner == null) return;
            runner.Started -= OnStarted;
            runner.LineShown -= OnLine;
            runner.ChoicesReady -= OnChoicesReady;
            runner.Ended -= OnEnded;
        }

        private void Update()
        {
            if (!window.activeSelf || busy || Time.frameCount == lineFrame) return; // ignore the key that started it

            if (typing)
            {
                revealed += charactersPerSecond * Time.unscaledDeltaTime;
                lineText.maxVisibleCharacters = Mathf.Min(totalCharacters, (int)revealed);
                if (AdvancePressed() || revealed >= totalCharacters) FinishTyping();
                return;
            }

            if (choicesShown)
            {
                int number = NumberPressed();
                if (number > 0 && number <= numberedActions.Count) numberedActions[number - 1].Invoke();
                else if (presentShown && Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
                    OpenPicker();
                return;
            }

            if (AdvancePressed()) runner.Next();
        }

        // ---- Runner events ----

        private void OnStarted()
        {
            HideChoices();
            busy = false;
            window.SetActive(true);
        }

        private void OnLine(DialogueLine line) => ShowLine(line);

        private void OnChoicesReady()
        {
            if (!typing && !choicesShown && !busy) ShowChoices();
        }

        private void OnEnded()
        {
            StopAllCoroutines();
            HideChoices();
            typing = false;
            busy = false;
            window.SetActive(false);
        }

        // ---- Lines ----

        private void ShowLine(DialogueLine line)
        {
            lineFrame = Time.frameCount;

            speakerBox.SetActive(!line.IsNarration);
            speakerText.text = line.Speaker;
            // Narration and the investigator's thoughts are in italics.
            lineText.fontStyle = line.IsNarration ? FontStyles.Italic : FontStyles.Normal;
            lineText.text = line.Text;
            lineText.maxVisibleCharacters = 0;
            lineText.ForceMeshUpdate();
            totalCharacters = lineText.textInfo.characterCount;

            revealed = 0f;
            typing = true;
            continueHint.SetActive(false);
        }

        private void FinishTyping()
        {
            typing = false;
            lineText.maxVisibleCharacters = int.MaxValue;

            if (runner.WaitingForChoice) ShowChoices();
            else continueHint.SetActive(true);
        }

        // ---- Choices ----

        private void ShowChoices()
        {
            HideChoices();
            var choices = runner.Choices;
            int shown = 0;

            for (int i = 0; i < choices.Count; i++)
            {
                string text = choices[i].text;
                if (DialogueRunner.IsPresent(text)) continue; // handled by the Present button

                int inkIndex = i;
                UnityAction action;
                string label;
                if (DialogueRunner.IsSilence(text))
                {
                    action = () => StartCoroutine(StaySilent(inkIndex));
                    label = "<i>... (stay silent)</i>";
                }
                else
                {
                    action = () => Choose(inkIndex);
                    label = text;
                }

                numberedActions.Add(action);
                AddButton(shown++, $"{numberedActions.Count}. {label}", action);
            }

            if (runner.CanPresent && picker != null)
            {
                presentShown = true;
                AddButton(shown++, "<color=#E8B05C>P. Present evidence</color>", OpenPicker);
            }

            for (int i = shown; i < choiceButtons.Count; i++)
                choiceButtons[i].gameObject.SetActive(false);

            choicesShown = true;
            continueHint.SetActive(false);
        }

        private void AddButton(int index, string label, UnityAction action)
        {
            while (choiceButtons.Count <= index)
                choiceButtons.Add(Instantiate(choiceTemplate, choiceTemplate.transform.parent));
            choiceButtons[index].gameObject.SetActive(true);
            choiceButtons[index].Setup(label, action);
        }

        private void HideChoices()
        {
            foreach (var button in choiceButtons) button.gameObject.SetActive(false);
            numberedActions.Clear();
            choicesShown = false;
            presentShown = false;
        }

        public void Choose(int inkIndex)
        {
            HideChoices();
            runner.Choose(inkIndex);
        }

        // ---- Silence ----

        private IEnumerator StaySilent(int inkIndex)
        {
            HideChoices();
            busy = true;
            continueHint.SetActive(false);
            speakerBox.SetActive(false);
            lineText.fontStyle = FontStyles.Italic;
            lineText.maxVisibleCharacters = int.MaxValue;

            for (int dots = 1; dots <= 3; dots++)
            {
                lineText.text = new string('.', dots);
                yield return new WaitForSecondsRealtime(silencePause / 3f);
            }

            busy = false;
            runner.Choose(inkIndex);
        }

        // ---- Present evidence ----

        private void OpenPicker()
        {
            HideChoices();
            busy = true;
            picker.Open(OnPresented, OnPickerCancelled);
        }

        private void OnPresented(ClueData clue)
        {
            busy = false;
            if (!runner.Present(clue))
                ShowLine(new DialogueLine(string.Empty, wrongEvidenceLine)); // then the choices come back
        }

        private void OnPickerCancelled()
        {
            busy = false;
            lineFrame = Time.frameCount; // the Esc/click that closed the picker doesn't count
            ShowChoices();
        }

        // ---- Input ----

        private static bool AdvancePressed()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            return (keyboard != null && (keyboard.eKey.wasPressedThisFrame ||
                                         keyboard.spaceKey.wasPressedThisFrame ||
                                         keyboard.enterKey.wasPressedThisFrame))
                   || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }

        private static int NumberPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return 0;
            for (int n = 1; n <= 9; n++)
            {
                var key = keyboard[(Key)((int)Key.Digit1 + n - 1)];
                if (key.wasPressedThisFrame) return n;
            }
            return 0;
        }
    }
}
