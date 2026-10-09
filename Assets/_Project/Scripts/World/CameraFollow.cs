using UnityEngine;

namespace QuietWitness.World
{
    // Smoothly follows the investigator, staying inside the location.
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float minX = -4f;
        [SerializeField] private float maxX = 4f;
        [SerializeField] private float smooth = 5f;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 pos = transform.position;
            float x = Mathf.Clamp(target.position.x, minX, maxX);
            pos.x = Mathf.Lerp(pos.x, x, smooth * Time.deltaTime);
            transform.position = pos;
        }
    }
}