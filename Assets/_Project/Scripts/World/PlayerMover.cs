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

        // Animator parameter: true while walking, so idle (breathing) plays only when standing.
        private static readonly int MovingParameter = Animator.StringToHash("Moving");

        private SpriteRenderer sprite;
        private Animator animator;

        // The picture may sit on a child ("Visual") with its pivot at the feet.
        private void Awake()
        {
            sprite = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            float input = 0f;
            if (CanMove && !InputBlocker.IsBlocked && Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input += 1f;
            }

            Vector3 pos = transform.position;
            float x = Mathf.Clamp(pos.x + input * speed * Time.deltaTime, minX, maxX);
            bool moving = input != 0f && !Mathf.Approximately(x, pos.x); // standing at a wall counts as standing
            SetMoving(moving);

            // Standing he faces the player (front view), so the picture is never mirrored then:
            // the hand in the pocket and the hair parting stay on their side.
            // Walking is drawn facing right and mirrored for walking left.
            if (sprite != null) sprite.flipX = moving && input < 0f;
            if (input == 0f) return;

            pos.x = x;
            transform.position = pos;
        }

        private void SetMoving(bool moving)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetBool(MovingParameter, moving);
        }
    }
}
