using QuietWitness.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuietWitness.Map
{
    // The town map: one panel for the whole game.
    // Opened from the office wall or a location exit (see MapOpener).
    public class MapPanel : MonoBehaviour
    {
        public static MapPanel Instance { get; private set; }

        [Tooltip("The map window that is shown and hidden.")]
        [SerializeField] private GameObject panel;

        private MapButton[] buttons;

        public bool IsOpen => panel.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this) return; // duplicate Systems, removed by its root
            Instance = this;
            buttons = panel.GetComponentsInChildren<MapButton>(true);
            panel.SetActive(false);
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        public void Open()
        {
            if (IsOpen) return;
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsTravelling) return;

            foreach (var button in buttons) button.Refresh();
            panel.SetActive(true);
            InputBlocker.Push();
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            InputBlocker.Pop();
        }
    }
}
