using UnityEngine;

namespace QuietWitness.World
{
    // A layer of the view outside a window: it slides a little when the camera moves, so the far city
    // seems to stay behind while the room passes by. Runs after the camera has moved (no jitter).
    [DefaultExecutionOrder(1000)]
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("How much it follows the camera: 0 = fixed to the room, 1 = moves with the camera (very far away).")]
        [Range(0f, 1f)]
        [SerializeField] private float factor = 0.2f;
        [Tooltip("Camera x at which the layer sits exactly where it was placed (the room's centre).")]
        [SerializeField] private float anchorX;
        [SerializeField] private float pixelsPerUnit = 36f;

        private Vector3 basePosition;
        private Transform cam;

        private void Start()
        {
            basePosition = transform.position;
            if (Camera.main != null) cam = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            float x = basePosition.x + (cam.position.x - anchorX) * factor;
            x = Mathf.Round(x * pixelsPerUnit) / pixelsPerUnit;      // stay on the pixel grid
            transform.position = new Vector3(x, basePosition.y, basePosition.z);
        }
    }
}
