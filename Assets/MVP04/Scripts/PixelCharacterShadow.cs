using UnityEngine;
using UnityEngine.Rendering;

namespace MVP04
{
    // The shader rotates the shadow card per light, so CPU culling must include
    // every horizontal orientation rather than only the visible billboard plane.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    [DefaultExecutionOrder(100)]
    public sealed class PixelCharacterShadow : MonoBehaviour
    {
        private SpriteRenderer portrait;
        private Sprite lastSprite;

        private void OnEnable()
        {
            portrait = GetComponent<SpriteRenderer>();
            portrait.shadowCastingMode = ShadowCastingMode.TwoSided;
            portrait.receiveShadows = true;
            UpdateBounds();
        }

        private void LateUpdate()
        {
            if (portrait != null && portrait.sprite != lastSprite) UpdateBounds();
        }

        private void UpdateBounds()
        {
            lastSprite = portrait.sprite;
            if (lastSprite == null) { portrait.ResetLocalBounds(); return; }
            Bounds spriteBounds = lastSprite.bounds;
            float radius = Mathf.Max(Mathf.Abs(spriteBounds.min.x), Mathf.Abs(spriteBounds.max.x)) + .02f;
            portrait.localBounds = new Bounds(new Vector3(0, spriteBounds.center.y, 0),
                new Vector3(radius * 2, spriteBounds.size.y + .04f, radius * 2));
        }

        private void OnDisable()
        {
            if (portrait != null) portrait.ResetLocalBounds();
            lastSprite = null;
        }
    }
}
