using UnityEngine;
using UnityEngine.InputSystem;

namespace MVP03
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PixelPilgrim : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private SpriteRenderer portrait;
        [SerializeField] private float moveSpeed = 3.4f;

        private CharacterController controller;
        private PixelWalkAnimation walkAnimation;
        private Vector3 portraitRestPosition;
        private float walkTime;
        private bool preferVertical;
        private bool heldForward, heldBackward, heldLeft, heldRight;
        private Vector3 facingDirection;

        public Vector3 FacingDirection => facingDirection.sqrMagnitude > .01f
            ? facingDirection
            : Vector3.ProjectOnPlane(worldCamera != null ? worldCamera.transform.forward : Vector3.forward, Vector3.up).normalized;

        public void Configure(Camera camera, SpriteRenderer sprite)
        {
            worldCamera = camera;
            portrait = sprite;
            portraitRestPosition = sprite.transform.localPosition;
            walkAnimation = sprite.GetComponent<PixelWalkAnimation>();
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (portrait != null)
            {
                portraitRestPosition = portrait.transform.localPosition;
                walkAnimation = portrait.GetComponent<PixelWalkAnimation>();
            }
        }

        private void Update()
        {
            Keyboard keys = PlayerKeyboard.Current;
            if (worldCamera == null) return;

            Vector2 input = ReadCardinalInput(keys);
            Move(input);
        }

        private void Move(Vector2 input)
        {
            Vector3 forward = Vector3.ProjectOnPlane(worldCamera.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(worldCamera.transform.right, Vector3.up).normalized;
            Vector3 movement = (forward * input.y + right * input.x) * moveSpeed;
            if (movement.sqrMagnitude > .01f) facingDirection = movement.normalized;
            Vector3 before = transform.position;
            controller.SimpleMove(movement);

            if (portrait == null) return;
            if (walkAnimation != null && walkAnimation.isActiveAndEnabled && walkAnimation.IsConfigured)
            {
                float distance = Vector3.ProjectOnPlane(transform.position - before, Vector3.up).magnitude;
                walkAnimation.Advance(input, distance);
                portrait.transform.localPosition = portraitRestPosition;
                return;
            }
            if (input.sqrMagnitude > 0.01f)
            {
                walkTime += Time.deltaTime * 12f;
                if (Mathf.Abs(input.x) > 0.1f) portrait.flipX = input.x < 0;
            }
            else walkTime = 0f;
            portrait.transform.localPosition = portraitRestPosition +
                Vector3.up * (walkTime == 0 ? 0 : Mathf.Abs(Mathf.Sin(walkTime)) * 0.045f);
        }

        private Vector2 ReadCardinalInput(Keyboard keys)
        {
            if (keys == null)
            {
                heldForward = heldBackward = heldLeft = heldRight = false;
                return Vector2.zero;
            }
            bool forward = keys.wKey.isPressed || keys.upArrowKey.isPressed;
            bool backward = keys.sKey.isPressed || keys.downArrowKey.isPressed;
            bool left = keys.aKey.isPressed || keys.leftArrowKey.isPressed;
            bool right = keys.dKey.isPressed || keys.rightArrowKey.isPressed;
            int horizontal = (right ? 1 : 0) - (left ? 1 : 0);
            int vertical = (forward ? 1 : 0) - (backward ? 1 : 0);

            if ((forward && !heldForward) || (backward && !heldBackward))
                preferVertical = true;
            if ((left && !heldLeft) || (right && !heldRight))
                preferVertical = false;
            heldForward = forward;
            heldBackward = backward;
            heldLeft = left;
            heldRight = right;

            // A newly pressed axis wins; releasing it resumes the other held direction.
            // Opposite keys on the same axis cancel, and diagonals are never emitted.
            if (horizontal != 0 && vertical != 0)
                return preferVertical ? new Vector2(0, vertical) : new Vector2(horizontal, 0);
            return new Vector2(horizontal, vertical);
        }

        private void LateUpdate()
        {
            if (portrait == null || worldCamera == null) return;
            // Matching the camera basis cancels its view rotation, including pitch.
            // Rotate the foot-pivot visual only; the controller remains upright.
            // Retain the legacy portrait's reversed horizontal atlas orientation.
            bool directional = walkAnimation != null && walkAnimation.IsConfigured;
            portrait.transform.rotation = worldCamera.transform.rotation *
                (directional ? Quaternion.identity : Quaternion.Euler(0, 180, 0));
        }
    }
}
