using System.Collections.Generic;
using QuietWitness.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuietWitness.World
{
    // Picks the nearest interactable in reach and uses it on E.
    // Hotspots with an InteractionIcon show it while in reach; the one E would use is highlighted.
    public class PlayerInteractor : MonoBehaviour
    {
        private readonly List<Interactable> inReach = new List<Interactable>();
        private bool iconsShown = true;

        public Interactable Current { get; private set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Interactable item) && !inReach.Contains(item))
            {
                inReach.Add(item);
                if (iconsShown && item.TryGetComponent(out InteractionIcon icon)) icon.SetVisible(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out Interactable item))
            {
                inReach.Remove(item);
                if (item.TryGetComponent(out InteractionIcon icon)) icon.SetVisible(false);
            }
        }

        private void Update()
        {
            // Icons hide while a dialogue, the map or an inspect screen is open.
            bool showIcons = !InputBlocker.IsBlocked;
            if (showIcons != iconsShown)
            {
                iconsShown = showIcons;
                foreach (var item in inReach)
                    if (item != null && item.TryGetComponent(out InteractionIcon icon)) icon.SetVisible(showIcons);
                if (showIcons) Highlight(Current, true);
                UpdatePrompt();
            }

            if (InputBlocker.IsBlocked) return;

            UpdateCurrent();

            if (Current != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                var prompt = InteractionPrompt.Instance;
                if (prompt == null || !prompt.Execute()) Use(Current);
            }

            // A click on a dot opens that hotspot's list too.
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse != null && cam != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
                foreach (var icon in InteractionIcon.Shown)
                    if (icon.Contains(world) && icon.TryGetComponent(out Interactable item))
                    {
                        Use(item);
                        break;
                    }
            }
        }

        // Backbone-style icon + name over the current hotspot (if the scene has an InteractionPrompt).
        private void UpdatePrompt()
        {
            var prompt = InteractionPrompt.Instance;
            if (prompt == null) return;
            if (!iconsShown || Current == null || !Current.TryGetComponent(out HotspotActions actions)) prompt.Hide();
            else prompt.Show(actions);
        }

        // Hotspots with a list of actions open it; the old ones just do their thing.
        private static void Use(Interactable item)
        {
            if (item.TryGetComponent(out HotspotActions actions) && actions.Actions.Count > 0)
            {
                item.TryGetComponent(out InteractionIcon icon);
                Vector2 top = icon != null ? icon.Centre + new Vector2(0f, icon.HalfHeight) : (Vector2)item.transform.position;
                ActionMenu.Open(actions, top, icon != null ? icon.Material : null);
            }
            else item.Interact();
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
                Highlight(Current, false);
                Current = nearest;
                Highlight(Current, true);
                UpdatePrompt();
                if (Current != null) Debug.Log($"[E] {Current.Prompt}");
            }
        }

        private static void Highlight(Interactable item, bool on)
        {
            if (item != null && item.TryGetComponent(out InteractionIcon icon)) icon.SetHighlighted(on);
        }
    }
}