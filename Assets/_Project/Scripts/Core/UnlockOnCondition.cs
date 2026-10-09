using UnityEngine;

namespace QuietWitness.Core
{
    // Shows the target only when the condition is met.
    public class UnlockOnCondition : MonoBehaviour
    {
        [SerializeField] private Condition condition;
        [SerializeField] private GameObject target;
        [Tooltip("Optional: flag to set when unlocked, e.g. conclusion:bottle_not_weapon")]
        [SerializeField] private string flagOnUnlock;

        private void Start()
        {
            GameState.Instance.FlagSet += OnFlagSet;
            Refresh();
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null) GameState.Instance.FlagSet -= OnFlagSet;
        }

        private void OnFlagSet(string flag) => Refresh();

        private void Refresh()
        {
            bool met = condition.IsMet(GameState.Instance);
            target.SetActive(met);

            if (met && !string.IsNullOrEmpty(flagOnUnlock))
                GameState.Instance.SetFlag(flagOnUnlock);
        }
    }
}