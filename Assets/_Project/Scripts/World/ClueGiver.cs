using QuietWitness.Core;
using UnityEngine;

namespace QuietWitness.World
{
    // Gives a clue when called, e.g. from an Interactable's On Interact.
    public class ClueGiver : MonoBehaviour
    {
        [SerializeField] private ClueData clue;

        public void Give()
        {
            if (GameState.Instance.FindClue(clue))
                Debug.Log($"New clue: {clue.Title}");
        }
    }
}