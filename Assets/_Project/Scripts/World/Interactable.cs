using UnityEngine;
using UnityEngine.Events;

namespace QuietWitness.World
{
    // Something the investigator can examine or talk to.
    [RequireComponent(typeof(Collider2D))]
    public class Interactable : MonoBehaviour
    {
        [SerializeField] private string prompt = "Examine";
        [TextArea(2, 6)]
        [SerializeField] private string description;
        [SerializeField] private UnityEvent onInteract;

        public string Prompt => prompt;

        public void Interact()
        {
            if (!string.IsNullOrEmpty(description)) Debug.Log(description);
            onInteract?.Invoke();
        }
    }
}