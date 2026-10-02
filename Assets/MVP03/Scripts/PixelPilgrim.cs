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
        private Vector3 portraitRestPosition;
        private float walkTime;

        public void Configure(Camera camera, SpriteRenderer sprite)
        {
            worldCamera = camera;
            portrait = sprite;
            portraitRestPosition = sprite.transform.localPosition;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (portrait != null) portraitRestPosition = portrait.transform.localPosition;
        }

        private void Update()
        {
            Keyboard keys = Keyboard.current;
            if (keys == null || worldCamera == null) return;

            Vector2 input = Vector2.zero;
            if (keys.wKey.isPressed || keys.upArrowKey.isPressed) input.y += 1;
            if (keys.sKey.isPressed || keys.downArrowKey.isPressed) input.y -= 1;
            if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) input.x -= 1;
            if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) input.x += 1;
            input = Vector2.ClampMagnitude(input, 1);

            Vector3 forward = Vector3.ProjectOnPlane(worldCamera.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(worldCamera.transform.right, Vector3.up).normalized;
            Vector3 movement = (forward * input.y + right * input.x) * moveSpeed;
            controller.SimpleMove(movement);

            if (portrait == null) return;
            if (input.sqrMagnitude > 0.01f)
            {
                walkTime += Time.deltaTime * 12f;
                if (Mathf.Abs(input.x) > 0.1f) portrait.flipX = input.x < 0;
            }
            else walkTime = 0f;
            portrait.transform.localPosition = portraitRestPosition +
                Vector3.up * (walkTime == 0 ? 0 : Mathf.Abs(Mathf.Sin(walkTime)) * 0.045f);
        }

        private void LateUpdate()
        {
            if (portrait == null || worldCamera == null) return;
            Vector3 toCamera = worldCamera.transform.position - portrait.transform.position;
            toCamera.y = 0f;
            portrait.transform.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
        }
    }
}
