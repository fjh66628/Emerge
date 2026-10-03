using UnityEngine;
using UnityEngine.InputSystem;

namespace MVP03
{
    // Follow after character movement, before sprite billboards and optical focus update.
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Camera))]
    public sealed class PixelFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform subject;
        [SerializeField] private Vector3 offset;
        [SerializeField] private float focusHeight = 1f;
        [Header("Distance zoom (Q / E and mouse wheel)")]
        [SerializeField] private bool allowZoom;
        [SerializeField, Min(1f)] private float minimumDistance = 5f;
        [SerializeField, Min(1f)] private float maximumDistance = 15f;
        [SerializeField, Min(.1f)] private float keyboardZoomSpeed = 8f;
        [SerializeField, Min(.1f)] private float wheelZoomStep = 1.4f;
        [SerializeField, Min(.01f)] private float zoomSmoothTime = .12f;

        private Vector3 viewDirection;
        private float currentDistance;
        private float targetDistance;
        private float zoomVelocity;

        public float Distance => currentDistance;
        public float TargetDistance => targetDistance;
        public float Pitch => Mathf.Asin(Mathf.Clamp(viewDirection.y, -1f, 1f)) * Mathf.Rad2Deg;

        public void SetPitch(float degrees)
        {
            if (float.IsNaN(degrees) || float.IsInfinity(degrees)) return;
            if (viewDirection.sqrMagnitude < .0001f) InitializeZoom();
            Vector3 horizontal = Vector3.ProjectOnPlane(viewDirection, Vector3.up).normalized;
            if (horizontal.sqrMagnitude < .0001f) horizontal = Vector3.back;
            float angle = Mathf.Clamp(degrees, 0, 75) * Mathf.Deg2Rad;
            viewDirection = horizontal * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);
            offset = Vector3.up * focusHeight + viewDirection * currentDistance;
            // Preserve zoom target/velocity, subject framing and the existing horizontal heading.
            Follow();
        }

        private void Awake() => InitializeZoom();

        public void Configure(Transform target)
        {
            subject = target;
            Vector3 focus = subject.position + Vector3.up * focusHeight;
            float distance = Mathf.Max(2f, Vector3.Dot(focus - transform.position, transform.forward));
            // Retain the established viewing angle while putting the character at screen centre.
            offset = Vector3.up * focusHeight - transform.forward * distance;
            InitializeZoom();
            Follow();
        }

        public void ConfigureZoom(float nearDistance = 5f, float farDistance = 15f)
        {
            allowZoom = true;
            minimumDistance = Mathf.Max(1f, nearDistance);
            maximumDistance = Mathf.Max(minimumDistance, farDistance);
            InitializeZoom();
            Follow();
        }

        private void InitializeZoom()
        {
            Vector3 displacement = offset - Vector3.up * focusHeight;
            currentDistance = Mathf.Max(.01f, displacement.magnitude);
            viewDirection = displacement.sqrMagnitude > .0001f ? displacement.normalized : -transform.forward;
            if (allowZoom) currentDistance = Mathf.Clamp(currentDistance, minimumDistance, maximumDistance);
            targetDistance = currentDistance;
            zoomVelocity = 0f;
        }

        private void Update()
        {
            if (!allowZoom || subject == null || Time.timeScale == 0f) return;
            Keyboard keys = PlayerKeyboard.Current;
            float axis = keys == null ? 0f : (keys.eKey.isPressed ? 1f : 0f) - (keys.qKey.isPressed ? 1f : 0f);
            float wheel = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // Current Input System defaults to normalized ticks; legacy Windows input uses 120 units per tick.
            if (InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange)
                wheel /= 120f;
#endif
            targetDistance = Mathf.Clamp(targetDistance + axis * keyboardZoomSpeed * Time.deltaTime - wheel * wheelZoomStep,
                minimumDistance, maximumDistance);
        }

        private void LateUpdate()
        {
            if (allowZoom)
                currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);
            Follow();
        }

        private void Follow()
        {
            if (subject == null) return;
            Vector3 followOffset = allowZoom ? Vector3.up * focusHeight + viewDirection * currentDistance : offset;
            transform.SetPositionAndRotation(subject.position + followOffset,
                Quaternion.LookRotation(Vector3.up * focusHeight - followOffset, Vector3.up));
        }
    }
}
