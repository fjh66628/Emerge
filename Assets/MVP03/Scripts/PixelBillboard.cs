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
            Vector3 facing = view.transform.position - sprite.transform.position;
            facing.y = 0;
            sprite.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
        }
    }
}
