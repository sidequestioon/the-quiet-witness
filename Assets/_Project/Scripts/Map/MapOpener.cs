using UnityEngine;

namespace QuietWitness.Map
{
    // Scene-side link to the map. Call Open() from an Interactable:
    // the map on the office wall or an exit at the edge of a location.
    public class MapOpener : MonoBehaviour
    {
        public void Open() => MapPanel.Instance.Open();
    }
}
