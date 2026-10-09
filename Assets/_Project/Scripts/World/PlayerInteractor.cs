using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuietWitness.World
{
    // Picks the nearest interactable in reach and uses it on E.
    public class PlayerInteractor : MonoBehaviour
    {
        private readonly List<Interactable> inReach = new List<Interactable>();

        public Interactable Current { get; private set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Interactable item) && !inReach.Contains(item))
                inReach.Add(item);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out Interactable item))
                inReach.Remove(item);
        }

        private void Update()
        {
            UpdateCurrent();

            if (Current != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                Current.Interact();
        }

        private void UpdateCurrent()
        {
            Interactable nearest = null;
            float best = float.MaxValue;

            foreach (var item in inReach)
            {
                if (item == null || !item.isActiveAndEnabled) continue;
                float distance = Mathf.Abs(item.transform.position.x - transform.position.x);
                if (distance < best)
                {
                    best = distance;
                    nearest = item;
                }
            }

            if (nearest != Current)
            {
                Current = nearest;
                if (Current != null) Debug.Log($"[E] {Current.Prompt}");
            }
        }
    }
}