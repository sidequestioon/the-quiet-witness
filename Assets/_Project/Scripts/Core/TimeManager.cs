using System;
using UnityEngine;

namespace QuietWitness.Core
{
    // Watches the current segment's key condition.
    // Time moves only when the player agrees (Advance).
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        public event Action<TimeSegment> SegmentChanged;
        // Key clues found: the investigator suggests moving on.
        public event Action<TimeSegment> ReadyToAdvance;

        [SerializeField] private TimeSegment startSegment;

        public TimeSegment Current { get; private set; }
        public bool IsReady { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Current = startSegment;
        }

        private void Start()
        {
            GameState.Instance.SetFlag(Current.Flag);
            GameState.Instance.FlagSet += OnFlagSet;
            CheckReady();
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null) GameState.Instance.FlagSet -= OnFlagSet;
        }

        private void OnFlagSet(string flag) => CheckReady();

        private void CheckReady()
        {
            if (IsReady || Current == null || Current.Next == null) return;
            if (!Current.KeyCondition.IsMet(GameState.Instance)) return;

            IsReady = true;
            ReadyToAdvance?.Invoke(Current);
        }

        // Called when the player agrees to move on.
        public void Advance()
        {
            if (!IsReady)
            {
                Debug.LogWarning("Key clues not found yet.");
                return;
            }
            Current = Current.Next;
            IsReady = false;
            SegmentChanged?.Invoke(Current);
            GameState.Instance.SetFlag(Current.Flag); // also re-checks the new segment
        }
    }
}