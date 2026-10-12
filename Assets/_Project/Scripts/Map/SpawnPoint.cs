using QuietWitness.World;
using UnityEngine;

namespace QuietWitness.Map
{
    // Where the hero appears when he comes in through a door (LocationTravel > Arrive At = this id).
    // Only the x position is used: he stays on the floor line.
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private string id;

        // Set by LocationTravel just before the scene changes.
        public static string Next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Next = null;

        private void Awake()
        {
            if (string.IsNullOrEmpty(Next) || Next != id) return;
            Next = null;
            var hero = Object.FindFirstObjectByType<PlayerMover>();
            if (hero == null) return;
            var p = hero.transform.position;
            hero.transform.position = new Vector3(transform.position.x, p.y, p.z);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.5f);
            Gizmos.DrawLine(transform.position + Vector3.down, transform.position + Vector3.up);
        }
    }
}
