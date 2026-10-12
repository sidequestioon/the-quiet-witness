using UnityEngine;

namespace QuietWitness.Map
{
    // Put on a door or exit; call Travel() from an Interactable.
    // Finds the SceneLoader at runtime, so the link never breaks between scenes.
    public class LocationTravel : MonoBehaviour
    {
        [SerializeField] private LocationData destination;
        [Tooltip("Id of a Spawn Point in the destination scene: the hero appears there (e.g. by the door). " +
                 "Empty = where he stands in that scene.")]
        [SerializeField] private string arriveAt;
        [Tooltip("On: a door to the next room - a short fade, no title card. Off: a trip across town.")]
        [SerializeField] private bool quick = true;

        public void Travel()
        {
            SpawnPoint.Next = arriveAt;
            SceneLoader.Instance.Travel(destination, quick);
        }
    }
}
