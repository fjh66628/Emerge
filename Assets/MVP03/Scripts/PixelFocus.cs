using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MVP03
{
    public sealed class PixelFocus : MonoBehaviour
    {
        [SerializeField] Camera view;
        [SerializeField] Transform subject;
        [SerializeField] Volume atmosphere;
        DepthOfField focus;

        public void Configure(Camera camera, Transform target, Volume volume)
        {
            view = camera;
            subject = target;
            atmosphere = volume;
        }

        void LateUpdate()
        {
            if (view == null || subject == null || atmosphere == null) return;
            if (focus == null && !atmosphere.profile.TryGet(out focus)) return;
            float distance = Vector3.Dot(subject.position + Vector3.up - view.transform.position,
                view.transform.forward);
            focus.focusDistance.Override(Mathf.Max(2f, distance));
        }
    }
}
