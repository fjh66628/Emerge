using UnityEngine;

namespace MVP03
{
    public sealed class PixelBillboard : MonoBehaviour
    {
        [SerializeField] Camera view;
        [SerializeField] SpriteRenderer sprite;

        public void Configure(Camera camera, SpriteRenderer renderer)
        {
            view = camera;
            sprite = renderer;
        }

        void LateUpdate()
        {
            if (view == null || sprite == null) return;
            // Match camera pitch while preserving this legacy portrait's horizontal orientation.
            sprite.transform.rotation = view.transform.rotation * Quaternion.Euler(0, 180, 0);
        }
    }
}
