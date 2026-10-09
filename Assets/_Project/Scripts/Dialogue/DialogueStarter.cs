using UnityEngine;

namespace QuietWitness.Dialogue
{
    // Scene-side link to the dialogue. Call Begin() from an Interactable on a character.
    public class DialogueStarter : MonoBehaviour
    {
        [Tooltip("Name of the ink knot, e.g. test_stranger.")]
        [SerializeField] private string knot;

        public void Begin() => DialogueRunner.Instance.StartDialogue(knot);
    }
}
