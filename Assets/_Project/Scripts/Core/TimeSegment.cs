using UnityEngine;

namespace QuietWitness.Core
{
    // One part of a day, e.g. "Saturday, morning".
    // Time can move on when the key condition is met.
    [CreateAssetMenu(fileName = "TimeSegment", menuName = "The Quiet Witness/Time Segment")]
    public class TimeSegment : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Tooltip("Key clues/flags that let time move on.")]
        [SerializeField] private Condition keyCondition;
        [Tooltip("Shown before moving on: what will close forever.")]
        [TextArea(2, 5)]
        [SerializeField] private string warning;
        [SerializeField] private TimeSegment next;

        public string Id => id;
        public string DisplayName => displayName;
        public Condition KeyCondition => keyCondition;
        public string Warning => warning;
        public TimeSegment Next => next;
        public string Flag => "time:" + id;
    }
}