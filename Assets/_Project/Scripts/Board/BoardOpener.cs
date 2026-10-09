using UnityEngine;

namespace QuietWitness.Board
{
    // Scene-side link to the evidence board. Call Open() from an Interactable in the office.
    public class BoardOpener : MonoBehaviour
    {
        public void Open() => BoardPanel.Instance.Open();
    }
}
