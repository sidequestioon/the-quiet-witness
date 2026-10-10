using UnityEngine;

namespace QuietWitness.World
{
    // Follows the investigator along the room and never shows past the room's edges.
    // Works with any camera size: it reads how much the camera shows each frame
    // (the Pixel Perfect Camera shows a little more on screens that are not an exact multiple).
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Tooltip("World x of the room's left and right edges (the edges of the background).")]
        [SerializeField] private float roomLeft = -10f;
        [SerializeField] private float roomRight = 10f;

        [Tooltip("World y of the bottom edge of the view. Lower = more floor, higher = feet cut off like Backbone.")]
        [SerializeField] private float viewBottom = -2.5f;

        [SerializeField] private float smooth = 5f;

        private Camera cam;

        private void Awake() => cam = GetComponent<Camera>();

        // Start on the investigator, not halfway across the room.
        private void Start()
        {
            if (target != null) transform.position = Goal();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 goal = Goal();
            Vector3 pos = transform.position;
            pos.x = Mathf.Lerp(pos.x, goal.x, smooth * Time.deltaTime);
            pos.y = goal.y;
            transform.position = pos;
        }

        private Vector3 Goal()
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            // A room narrower than the view just stays centered.
            float x = roomRight - roomLeft <= halfWidth * 2f
                ? (roomLeft + roomRight) * 0.5f
                : Mathf.Clamp(target.position.x, roomLeft + halfWidth, roomRight - halfWidth);

            return new Vector3(x, viewBottom + halfHeight, transform.position.z);
        }
    }
}
