using UnityEngine;

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

        public void Configure(Transform target)
        {
            subject = target;
            Vector3 focus = subject.position + Vector3.up * focusHeight;
            float distance = Mathf.Max(2f, Vector3.Dot(focus - transform.position, transform.forward));
            // Retain the established viewing angle while putting the character at screen centre.
            offset = Vector3.up * focusHeight - transform.forward * distance;
            Follow();
        }

        private void LateUpdate() => Follow();

        private void Follow()
        {
            if (subject == null) return;
            transform.SetPositionAndRotation(subject.position + offset,
                Quaternion.LookRotation(Vector3.up * focusHeight - offset, Vector3.up));
        }
    }
}
