using System;
using System.Collections.Generic;
using QuietWitness.Audio;
using UnityEngine;
using UnityEngine.Events;

namespace QuietWitness.World
{
    public enum ActionKind { Look, Take, Use, Open, Talk, Leave, UseItem }

    [Serializable]
    public class HotspotAction
    {
        public ActionKind kind;
        [Tooltip("Shown over the icon, e.g. \"Aquarium\" or \"Inspect cork board\". Empty = Look, Take...")]
        public string label;
        [Tooltip("What the hero says when this is chosen: a line from Hero Lines (Resources/HeroLines). " +
                 "Pick it in the list; its text and plate width can be edited right under it. Empty = Lines below.")]
        [HeroLine]
        public string line;
        [Tooltip("Only used when Line is empty. Enter (or \\n) inside a box starts a new line.")]
        [TextArea(2, 8)]
        public string[] lines;
        [Tooltip("Only used when Line is empty. Widest the plate may get, in pixels. 0 = the usual width (Hero Speech > Max Width).")]
        public int plateWidth;
        public UnityEvent onChosen;

        public string Label => string.IsNullOrEmpty(label) ? DefaultLabel(kind) : label;

        // What he says: the Hero Lines text, or Lines when there is none.
        public string[] SaidLines
        {
            get
            {
                var found = HeroLines.Find(line);
                return found != null && !string.IsNullOrWhiteSpace(found.text) ? new[] { found.text } : lines;
            }
        }

        public int SaidWidth
        {
            get
            {
                var found = HeroLines.Find(line);
                return found != null && !string.IsNullOrWhiteSpace(found.text) ? found.plateWidth : plateWidth;
            }
        }

        public bool HasSpeech
        {
            get { var said = SaidLines; return said != null && said.Length > 0; }
        }

        public static string DefaultLabel(ActionKind kind) => kind switch
        {
            ActionKind.Look => "Look",
            ActionKind.Take => "Take",
            ActionKind.Use => "Use",
            ActionKind.Open => "Open",
            ActionKind.Talk => "Talk",
            ActionKind.Leave => "Leave",
            ActionKind.UseItem => "Use item",
            _ => kind.ToString()
        };
    }

    // What you can do with a hotspot: shown by InteractionPrompt (an icon per action with its name)
    // over the hotspot E would use.
    [RequireComponent(typeof(Interactable))]
    public class HotspotActions : MonoBehaviour
    {
        [SerializeField] private List<HotspotAction> actions = new List<HotspotAction>();
        [Tooltip("Where the icon's centre sits, relative to this hotspot (world units).")]
        [SerializeField] private Vector2 promptOffset;
        [Tooltip("Story-important: its icon and name are yellow instead of white.")]
        [SerializeField] private bool story;

        // Every hotspot in the scene, so the prompt can show small icons for the ones nearby.
        private static readonly List<HotspotActions> all = new List<HotspotActions>();
        public static IReadOnlyList<HotspotActions> All => all;
        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        public IReadOnlyList<HotspotAction> Actions => actions;
        public Vector2 PromptPoint => (Vector2)transform.position + promptOffset;
        public bool IsStory => story;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.85f, 0.6f);
            Gizmos.DrawWireSphere(PromptPoint, 6.5f / 36f);
        }

        public void Choose(int index)
        {
            if (index < 0 || index >= actions.Count) return;
            var action = actions[index];
            UiSounds.Play(s => s.click);
            Debug.Log($"[{action.kind}] {name}");
            bool said = action.HasSpeech && HeroSpeech.Say(action);
            if (action.onChosen != null && action.onChosen.GetPersistentEventCount() > 0) action.onChosen.Invoke();
            else if (!said) GetComponent<Interactable>().Interact();
        }
    }
}
