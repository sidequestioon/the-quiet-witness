using System.Collections.Generic;
using UnityEngine;

namespace QuietWitness.Core
{
    // The target exists only in the listed segments:
    // a shop that closes, a person who leaves town.
    public class AvailableInSegments : MonoBehaviour
    {
        [SerializeField] private List<TimeSegment> segments = new List<TimeSegment>();
        [SerializeField] private GameObject target;

        private void Start()
        {
            TimeManager.Instance.SegmentChanged += OnSegmentChanged;
            OnSegmentChanged(TimeManager.Instance.Current);
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.SegmentChanged -= OnSegmentChanged;
        }

        private void OnSegmentChanged(TimeSegment current)
        {
            target.SetActive(segments.Contains(current));
        }
    }
}