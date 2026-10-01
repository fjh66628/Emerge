using UnityEngine;
using UnityEngine.InputSystem;

namespace MVP01
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonWalk : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float walkSpeed = 4.6f;
        [SerializeField] private float sprintSpeed = 7.0f;
        [SerializeField] private float mouseSensitivity = 0.12f;

        private CharacterController controller;
        private float pitch;
        private float verticalSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || mouse == null || viewCamera == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(Vector3.up, look.x, Space.Self);
                pitch = Mathf.Clamp(pitch - look.y, -83f, 83f);
                viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Vector2 move = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            move = Vector2.ClampMagnitude(move, 1f);

            float speed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed
                ? sprintSpeed : walkSpeed;
            Vector3 horizontal = transform.TransformDirection(new Vector3(move.x, 0f, move.y)) * speed;
            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -1f;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            controller.Move((horizontal + Vector3.up * verticalSpeed) * Time.deltaTime);
        }
    }
}
