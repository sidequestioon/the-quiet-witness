using QuietWitness.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuietWitness.World
{
    // Walks the investigator left and right inside a location.
    public class PlayerMover : MonoBehaviour
    {
        [SerializeField] private float speed = 3f;
        [Tooltip("Walkable area edges in world units.")]
        [SerializeField] private float minX = -8f;
        [SerializeField] private float maxX = 8f;

        // Turned off during dialogue and cutscenes.
        public bool CanMove { get; set; } = true;

        private SpriteRenderer sprite;

        private void Awake() => sprite = GetComponent<SpriteRenderer>();

        private void Update()
        {
            if (!CanMove || InputBlocker.IsBlocked || Keyboard.current == null) return;

            float input = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input += 1f;
            if (input == 0f) return;

            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x + input * speed * Time.deltaTime, minX, maxX);
            transform.position = pos;

            if (sprite != null) sprite.flipX = input < 0f; // face the walking direction
        }
    }
}