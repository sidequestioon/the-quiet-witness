using UnityEngine;

namespace QuietWitness.Archive
{
    // Scene-side link to the archive. Call Open() from an Interactable:
    // the phone book on the office desk, the archive shelf at the police station.
    public class ArchiveOpener : MonoBehaviour
    {
        [SerializeField] private ArchiveBook book;

        public void Open() => ArchivePanel.Instance.Open(book);
    }
}
