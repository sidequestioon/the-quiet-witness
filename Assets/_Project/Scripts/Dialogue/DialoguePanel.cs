using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuietWitness.Dialogue
{
    // The dialogue box at the bottom of the screen. Shows what DialogueRunner says:
    // types the line out, then waits for E / Space / Enter / click, or for a choice (click or 1-9).
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

        [Header("Typing")]
        [SerializeField] private float charactersPerSecond = 50f;

        private readonly List<DialogueChoiceButton> choiceButtons = new List<DialogueChoiceButton>();
        private DialogueRunner runner;
        private bool typing;
        private bool choicesShown;
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
            if (!window.activeSelf || Time.frameCount == lineFrame) return; // ignore the key that started it

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
                if (number > 0 && number <= runner.Choices.Count) Choose(number - 1);
                return;
            }

            if (AdvancePressed()) runner.Next();
        }

        public void Choose(int index)
        {
            HideChoices();
            runner.Choose(index);
        }

        private void OnStarted()
        {
            HideChoices();
            window.SetActive(true);
        }

        private void OnLine(DialogueLine line)
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

        private void OnChoicesReady()
        {
            if (!typing && !choicesShown) ShowChoices();
        }

        private void FinishTyping()
        {
            typing = false;
            lineText.maxVisibleCharacters = int.MaxValue;

            if (runner.WaitingForChoice) ShowChoices();
            else continueHint.SetActive(true);
        }

        private void ShowChoices()
        {
            var choices = runner.Choices;
            for (int i = 0; i < choices.Count; i++)
            {
                while (choiceButtons.Count <= i)
                    choiceButtons.Add(Instantiate(choiceTemplate, choiceTemplate.transform.parent));
                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].Setup(i, choices[i].text, this);
            }
            for (int i = choices.Count; i < choiceButtons.Count; i++)
                choiceButtons[i].gameObject.SetActive(false);

            choicesShown = true;
            continueHint.SetActive(false);
        }

        private void HideChoices()
        {
            foreach (var button in choiceButtons) button.gameObject.SetActive(false);
            choicesShown = false;
        }

        private void OnEnded()
        {
            HideChoices();
            typing = false;
            window.SetActive(false);
        }

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
