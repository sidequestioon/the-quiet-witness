using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuietWitness.Archive
{
    // One line in the archive list. Cloned from a hidden template by ArchivePanel.
    [RequireComponent(typeof(Button))]
    public class ArchiveListItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private ArchiveEntry entry;
        private ArchivePanel panel;

        public void Setup(ArchiveEntry entry, ArchivePanel panel, bool isNew)
        {
            this.entry = entry;
            this.panel = panel;
            label.text = isNew ? entry.Title + "  <color=#B3261E>NEW</color>" : entry.Title;

            var button = GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => this.panel.Show(this.entry));
        }
    }
}
