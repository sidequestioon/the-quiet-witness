using UnityEngine;

namespace QuietWitness.Core
{
    // Temporary test helper: gives a clue from the Inspector menu.
    public class DebugClueGiver : MonoBehaviour
    {
        [SerializeField] private ClueData clue;

        private void Start()
        {
            GameState.Instance.ClueFound += LogClue;
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null) GameState.Instance.ClueFound -= LogClue;
        }

        [ContextMenu("Give clue")]
        private void GiveClue()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play mode first.");
                return;
            }
            if (!GameState.Instance.FindClue(clue))
                Debug.Log($"Already found: {clue.Title}");
        }

        private void LogClue(ClueData found)
        {
            Debug.Log($"New clue: {found.Title} (key: {found.IsKey})");
        }
    }
}