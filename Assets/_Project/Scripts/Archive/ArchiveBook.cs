using System.Collections.Generic;
using UnityEngine;

namespace QuietWitness.Archive
{
    // A set of records opened as one: the phone book in the office,
    // the police archive, a newspaper morgue. Same panel, different book.
    [CreateAssetMenu(fileName = "ArchiveBook", menuName = "The Quiet Witness/Archive Book")]
    public class ArchiveBook : ScriptableObject
    {
        [SerializeField] private string displayName = "Phone book";
        [Tooltip("Shown while the search field is empty and nothing is listed yet.")]
        [SerializeField] private string searchHint = "Type a name or a place.";
        [Tooltip("Off: records appear only when searched for (phone book).\n" +
                 "On: every available record is listed right away (a drawer of clippings).")]
        [SerializeField] private bool listAllAvailable;
        [SerializeField] private List<ArchiveEntry> entries = new List<ArchiveEntry>();

        public string DisplayName => displayName;
        public string SearchHint => searchHint;
        public bool ListAllAvailable => listAllAvailable;
        public IReadOnlyList<ArchiveEntry> Entries => entries;
    }
}
