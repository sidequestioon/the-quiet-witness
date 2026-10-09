using UnityEngine;

namespace QuietWitness.Map
{
    // Put on a door or exit; call Travel() from an Interactable.
    // Finds the SceneLoader at runtime, so the link never breaks between scenes.
    public class LocationTravel : MonoBehaviour
    {
        [SerializeField] private LocationData destination;

        public void Travel() => SceneLoader.Instance.Travel(destination);
    }
}
