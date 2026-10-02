using UnityEngine;

namespace MVP04
{
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class ChapelPropPusher : MonoBehaviour
    {
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!enabled || Time.deltaTime <= 0) return;
            var body = hit.rigidbody;
            if (body == null || !body.TryGetComponent<KnockableCandleStand>(out var stand)) return;
            Vector3 horizontal = Vector3.ProjectOnPlane(hit.moveDirection, Vector3.up);
            // Ground contacts and gravity alone must not knock a prop over.
            if (horizontal.sqrMagnitude < .01f || Vector3.Dot(horizontal.normalized, hit.normal) > -.15f) return;
            stand.TryPush(horizontal, hit.point, hit.moveLength * horizontal.magnitude / Time.deltaTime);
        }
    }
}
