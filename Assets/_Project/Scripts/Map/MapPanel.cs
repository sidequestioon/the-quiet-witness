using UnityEngine;

namespace QuietWitness.Map
{
    // Where everything that opens the map calls (the cork board in the office, Map Opener, close-ups):
    // it opens the town map (Setup > Town map).
    public class MapPanel : MonoBehaviour
    {
        public static MapPanel Instance { get; private set; }

        public bool IsOpen => TownMap.IsOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this) return; // duplicate Systems, removed by its root
            Instance = this;
            // The old prototype map window, if Setup > Cleanup hasn't removed it yet: never shown.
            var old = transform.Find("Map");
            if (old != null) old.gameObject.SetActive(false);
        }

        public void Open()
        {
            if (TownMap.Instance != null) TownMap.Instance.Open();
            else Debug.LogWarning("[Map] No town map on Systems: run Setup > Town map.");
        }

        public void Close()
        {
            if (TownMap.Instance != null && TownMap.IsOpen) TownMap.Instance.Close();
        }
    }
}
