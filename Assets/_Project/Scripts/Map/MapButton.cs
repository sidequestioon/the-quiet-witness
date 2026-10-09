using QuietWitness.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QuietWitness.Map
{
    // One place on the map: hidden until unlocked,
    // shows a "NEW" badge until visited, disabled where you already are.
    [RequireComponent(typeof(Button))]
    public class MapButton : MonoBehaviour
    {
        [SerializeField] private LocationData location;
        [Tooltip("Optional small 'NEW' label, shown until the place is visited.")]
        [SerializeField] private GameObject newBadge;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Go);
        }

        public void Refresh()
        {
            if (button == null) button = GetComponent<Button>();
            var state = GameState.Instance;

            bool unlocked = location.IsUnlocked(state);
            gameObject.SetActive(unlocked);
            if (!unlocked) return;

            bool here = SceneManager.GetActiveScene().name == location.SceneName;
            if (here) state.SetFlag(location.VisitedFlag);

            button.interactable = !here;
            if (newBadge != null) newBadge.SetActive(!state.HasFlag(location.VisitedFlag));
        }

        private void Go()
        {
            MapPanel.Instance.Close();
            SceneLoader.Instance.Travel(location);
        }
    }
}
