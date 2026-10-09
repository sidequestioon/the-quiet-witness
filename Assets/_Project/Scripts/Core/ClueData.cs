using UnityEngine;

namespace QuietWitness.Core
{
    // One clue = one asset file. Fill it in the Inspector, no code needed.
    [CreateAssetMenu(fileName = "Clue", menuName = "The Quiet Witness/Clue")]
    public class ClueData : ScriptableObject
    {
        [Tooltip("Unique id, e.g. bottle_no_blood. Don't change it later: saves use it.")]
        [SerializeField] private string id;
        [SerializeField] private string title;
        [TextArea(3, 8)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [Tooltip("Key clues move time forward.")]
        [SerializeField] private bool isKey;

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public Sprite Icon => icon;
        public bool IsKey => isKey;

        // The flag this clue sets in GameState when found.
        public string Flag => "clue:" + id;
    }
}