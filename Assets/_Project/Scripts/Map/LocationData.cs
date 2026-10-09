using QuietWitness.Core;
using UnityEngine;

namespace QuietWitness.Map
{
    // One place in Millbrook.
    [CreateAssetMenu(fileName = "Location", menuName = "The Quiet Witness/Location")]
    public class LocationData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Tooltip("Exact scene name. The scene must be in Build Profiles > Scene List.")]
        [SerializeField] private string sceneName;
        [Tooltip("When this location appears on the map. Empty = always.")]
        [SerializeField] private Condition unlockCondition;

        public string Id => id;
        public string DisplayName => displayName;
        public string SceneName => sceneName;
        public string VisitedFlag => "visited:" + id;

        public bool IsUnlocked(GameState state) => unlockCondition.IsMet(state);
    }
}
