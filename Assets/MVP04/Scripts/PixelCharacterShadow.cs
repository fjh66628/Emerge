using UnityEngine;
using UnityEngine.Rendering;

namespace MVP04
{
    // The shader rotates the shadow card per light, so CPU culling must include
    // the upright shadow silhouette as well as the camera-tilted visible plane.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    [DefaultExecutionOrder(100)]
    public sealed class PixelCharacterShadow : MonoBehaviour
    {
        private SpriteRenderer portrait;
        private Sprite lastSprite;
        private Quaternion lastRotation;
        private Vector3 lastScale;

        private void OnEnable()
        {
            portrait = GetComponent<SpriteRenderer>();
            portrait.shadowCastingMode = ShadowCastingMode.TwoSided;
            portrait.receiveShadows = true;
            UpdateBounds();
        }

        private void LateUpdate()
        {
            if (portrait != null && (portrait.sprite != lastSprite || transform.rotation != lastRotation ||
                transform.lossyScale != lastScale)) UpdateBounds();
        }

        private void UpdateBounds()
        {
            lastSprite = portrait.sprite;
            lastRotation = transform.rotation;
            lastScale = transform.lossyScale;
            if (lastSprite == null) { portrait.ResetLocalBounds(); return; }
            Bounds spriteBounds = lastSprite.bounds;
            float radius = Mathf.Max(Mathf.Abs(spriteBounds.min.x), Mathf.Abs(spriteBounds.max.x)) * Mathf.Abs(lastScale.x) + .02f;
            float heightScale = Mathf.Abs(lastScale.y);
            // The shader keeps the per-light shadow card vertical in world space.
            // Bring its all-azimuth envelope into the tilted/scaled sprite's local space.
            Matrix4x4 inverse = transform.worldToLocalMatrix;
            Vector3 centre = inverse.MultiplyVector(Vector3.up * (spriteBounds.center.y * heightScale));
            Vector3 extents = Abs(inverse.MultiplyVector(Vector3.right * radius)) +
                Abs(inverse.MultiplyVector(Vector3.up * (spriteBounds.extents.y * heightScale + .02f))) +
                Abs(inverse.MultiplyVector(Vector3.forward * radius));
            var bounds = new Bounds(centre, extents * 2);
            bounds.Encapsulate(spriteBounds.min);
            bounds.Encapsulate(spriteBounds.max);
            portrait.localBounds = bounds;
        }

        private static Vector3 Abs(Vector3 value) => new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

        private void OnDisable()
        {
            if (portrait != null) portrait.ResetLocalBounds();
            lastSprite = null;
        }
    }
}
