using System;
using System.Collections.Generic;
using Ink.Runtime;
using Ink.UnityIntegration;
using QuietWitness.Core;
using QuietWitness.Map;
using UnityEngine;

namespace QuietWitness.Dialogue
{
    // Runs the game's ink story. One story for the whole game, so ink
    // remembers what was said across conversations. Lives on the Systems root.
    //
    // Functions ink can call (declare them as EXTERNAL in Main.ink):
    //   is_set("flag")     -> true if the flag is set, e.g. is_set("clue:coroner_blow_to_head")
    //   set_flag("flag")   -> sets a flag, e.g. set_flag("conclusion:bottle_not_weapon")
    //   give_clue("id")    -> the player finds the clue with this id
    // When a conversation ends, "talked:<knot>" is set.
    public class DialogueRunner : MonoBehaviour
    {
        public static DialogueRunner Instance { get; private set; }

        public event Action Started;
        public event Action<DialogueLine> LineShown;
        // Choices are waiting, but no new line came with them
        // (e.g. after "~ give_clue(...)" the story goes straight to the choices).
        public event Action ChoicesReady;
        public event Action Ended;

        [Tooltip("Main.ink from Assets/_Project/Dialogue.")]
        [SerializeField] private InkFile inkFile;
        [Tooltip("Every clue ink may give by id. Use ⋮ > Collect all clues.")]
        [SerializeField] private List<ClueData> clues = new List<ClueData>();

        private Story story;
        private string currentKnot;
        private readonly List<DialogueLine> history = new List<DialogueLine>();

        public bool IsRunning { get; private set; }
        public IReadOnlyList<DialogueLine> History => history;
        public bool WaitingForChoice => IsRunning && !story.canContinue && story.currentChoices.Count > 0;
        public List<Choice> Choices => story.currentChoices;

        private void Awake()
        {
            if (Instance != null && Instance != this) return; // duplicate Systems, removed by its root
            Instance = this;

            if (inkFile == null || !inkFile.isCompiled)
            {
                Debug.LogWarning("[Dialogue] No compiled ink story assigned (or Main.ink has errors).");
                return;
            }

            story = new Story(inkFile.storyJson);
            story.onError += (message, type) =>
            {
                if (type == Ink.ErrorType.Warning) Debug.LogWarning("[Ink] " + message);
                else Debug.LogError("[Ink] " + message);
            };

            story.BindExternalFunction("is_set",
                new Func<string, object>(flag => GameState.Instance != null && GameState.Instance.HasFlag(flag)),
                true);
            story.BindExternalFunction("set_flag",
                new Action<string>(flag => { if (GameState.Instance != null) GameState.Instance.SetFlag(flag); }));
            story.BindExternalFunction("give_clue",
                new Action<string>(GiveClue));
        }

        public void StartDialogue(string knot)
        {
            if (IsRunning || story == null || string.IsNullOrWhiteSpace(knot)) return;
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsTravelling) return;

            try
            {
                story.ChoosePathString(knot);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Dialogue] Can't start '{knot}': {e.Message}");
                return;
            }

            currentKnot = knot;
            if (history.Count > 0) history.Add(new DialogueLine(string.Empty, Separator));
            IsRunning = true;
            InputBlocker.Push();
            Started?.Invoke();
            Next();
        }

        // Shows the next line, or ends the conversation.
        // While choices are waiting, does nothing: call Choose instead.
        public void Next()
        {
            if (!IsRunning) return;

            while (story.canContinue)
            {
                string raw = story.Continue();
                if (string.IsNullOrWhiteSpace(raw)) continue; // empty lines from logic

                var line = DialogueLine.Parse(raw, story.currentTags);
                history.Add(line);
                LineShown?.Invoke(line);
                return;
            }

            if (story.currentChoices.Count > 0)
            {
                ChoicesReady?.Invoke();
                return;
            }

            End();
        }

        public void Choose(int index)
        {
            if (!WaitingForChoice || index < 0 || index >= story.currentChoices.Count) return;
            if (IsSilence(story.currentChoices[index].text))
                history.Add(new DialogueLine(string.Empty, "(silence)"));
            story.ChooseChoiceIndex(index);
            Next();
        }

        // ---- Present evidence & silence ----
        // In ink, special choices are not shown as buttons:
        //   * [present:<clue id>]  -> taken when the player presents that clue
        //   + [present:any]        -> taken for any other clue (the "that proves nothing" answer)
        //   * [silence]            -> shown as "... (stay silent)"

        // Marks the start of a new conversation in History.
        public const string Separator = "---";

        public const string PresentPrefix = "present:";
        public const string PresentAny = "present:any";
        public const string Silence = "silence";

        public static bool IsPresent(string choiceText) =>
            Clean(choiceText).StartsWith(PresentPrefix, StringComparison.Ordinal);

        public static bool IsSilence(string choiceText) => Clean(choiceText) == Silence;

        // True when the current choices accept evidence.
        public bool CanPresent =>
            WaitingForChoice && story.currentChoices.Exists(c => IsPresent(c.text));

        // Picks the matching present:<id> choice, or present:any.
        // Returns false if the story has no answer for this clue.
        public bool Present(ClueData clue)
        {
            if (clue == null || !CanPresent) return false;

            var choices = story.currentChoices;
            int index = choices.FindIndex(c => Clean(c.text) == PresentPrefix + clue.Id.ToLowerInvariant());
            if (index < 0) index = choices.FindIndex(c => Clean(c.text) == PresentAny);
            if (index < 0) return false;

            history.Add(new DialogueLine(string.Empty, $"(presented: {clue.Title})"));
            story.ChooseChoiceIndex(index);
            Next();
            return true;
        }

        private static string Clean(string text) => (text ?? string.Empty).Trim().ToLowerInvariant();

        private void End()
        {
            IsRunning = false;
            InputBlocker.Pop();
            if (GameState.Instance != null) GameState.Instance.SetFlag("talked:" + currentKnot);
            Ended?.Invoke();
        }

        private void GiveClue(string id)
        {
            var clue = clues.Find(c => c != null && c.Id == id);
            if (clue == null)
            {
                Debug.LogWarning($"[Dialogue] give_clue: no clue with id '{id}' in DialogueRunner > Clues.");
                return;
            }
            if (GameState.Instance != null) GameState.Instance.FindClue(clue);
        }

#if UNITY_EDITOR
        [ContextMenu("Collect all clues")]
        private void CollectAllClues()
        {
            clues.Clear();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:ClueData"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var clue = UnityEditor.AssetDatabase.LoadAssetAtPath<ClueData>(path);
                if (clue != null) clues.Add(clue);
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[Dialogue] Collected {clues.Count} clues.");
        }
#endif
    }
}
