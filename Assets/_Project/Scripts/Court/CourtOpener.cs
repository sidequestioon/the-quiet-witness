using UnityEngine;

namespace QuietWitness.Court
{
    // Scene-side link: starts the trial. Put it on the courtroom door's Interactable.
    public class CourtOpener : MonoBehaviour
    {
        [SerializeField] private CourtCase courtCase;

        public void Begin() => CourtPanel.Instance.Begin(courtCase);
    }
}
