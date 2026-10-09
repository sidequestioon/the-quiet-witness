using System.Collections.Generic;
using QuietWitness.Core;
using QuietWitness.Map;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace QuietWitness.Archive
{
    // Dossiers & archive: one window for the whole game.
    // Left: search field and the list of records. Right: the open record.
    // Opened by ArchiveOpener with a specific ArchiveBook.
    public class ArchivePanel : MonoBehaviour
    {
        public static ArchivePanel Instance { get; private set; }

        [Tooltip("The archive window that is shown and hidden.")]
        [SerializeField] private GameObject window;
        [SerializeField] private TMP_Text bookTitle;
        [SerializeField] private TMP_InputField searchField;

        [Header("List")]
        [Tooltip("Hidden list line that gets cloned for every record.")]
        [SerializeField] private ArchiveListItem itemTemplate;
        [Tooltip("Shows the hint or 'No records found.'")]
        [SerializeField] private TMP_Text emptyText;

        [Header("Open record")]
        [SerializeField] private GameObject details;
        [SerializeField] private Image photo;
        [SerializeField] private TMP_Text entryTitle;
        [SerializeField] private TMP_Text entrySubtitle;
        [SerializeField] private TMP_Text entryBody;

        private readonly List<ArchiveListItem> items = new List<ArchiveListItem>();
        private ArchiveBook book;

        public bool IsOpen => window.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this) return; // duplicate Systems, removed by its root
            Instance = this;
            itemTemplate.gameObject.SetActive(false);
            searchField.onValueChanged.AddListener(_ => RefreshList());
            window.SetActive(false);
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        public void Open(ArchiveBook bookToOpen)
        {
            if (IsOpen || bookToOpen == null) return;
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsTravelling) return;

            book = bookToOpen;
            bookTitle.text = book.DisplayName;
            searchField.SetTextWithoutNotify(string.Empty);
            details.SetActive(false);

            window.SetActive(true);
            InputBlocker.Push();
            RefreshList();
            searchField.ActivateInputField();
        }

        public void Close()
        {
            if (!IsOpen) return;
            window.SetActive(false);
            InputBlocker.Pop();
        }

        public void Show(ArchiveEntry entry)
        {
            entryTitle.text = entry.Title;
            entrySubtitle.text = entry.Subtitle;
            entryBody.text = entry.Body;
            photo.sprite = entry.Photo;
            photo.gameObject.SetActive(entry.Photo != null);
            details.SetActive(true);

            if (entry.MarkRead(GameState.Instance))
                RefreshList(); // drop the NEW mark
        }

        private void RefreshList()
        {
            var state = GameState.Instance;
            string query = searchField.text;
            int shown = 0;

            foreach (var entry in book.Entries)
            {
                if (entry == null || !entry.IsAvailable(state)) continue;

                bool read = entry.IsRead(state);
                bool visible = book.ListAllAvailable || read || entry.Matches(query);
                if (!visible) continue;

                GetItem(shown).Setup(entry, this, isNew: !read);
                shown++;
            }

            for (int i = shown; i < items.Count; i++)
                items[i].gameObject.SetActive(false);

            emptyText.gameObject.SetActive(shown == 0);
            emptyText.text = ArchiveEntry.Normalize(query).Length > 0 ? "No records found." : book.SearchHint;
        }

        private ArchiveListItem GetItem(int index)
        {
            while (items.Count <= index)
            {
                var item = Instantiate(itemTemplate, itemTemplate.transform.parent);
                items.Add(item);
            }
            items[index].gameObject.SetActive(true);
            return items[index];
        }
    }
}
