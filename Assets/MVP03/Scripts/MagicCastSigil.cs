using UnityEngine;

namespace MVP03
{
    public sealed class MagicCastSigil : MonoBehaviour
    {
        [SerializeField] private MeshRenderer pattern;
        [SerializeField] private Light castLight;
        [SerializeField] private float diameter = 1.8f;
        [SerializeField] private float forwardOffset = .62f;
        [SerializeField] private float fadeDuration = .3f;
        private Transform owner;
        private Vector3 direction;
        private float height, duration, started, releasedAt;
        private bool released, begun;
        private MaterialPropertyBlock properties;

        public bool IsReady => begun && !released && Time.time - started >= duration;
        public Vector3 CastOrigin => owner != null ? owner.position + Vector3.up * height : transform.position;
        public Vector3 LaunchPosition => CastOrigin + direction * forwardOffset;

        public void Configure(MeshRenderer ornament, Light light)
        { pattern = ornament; castLight = light; }

        public void SetDirection(Vector3 heading)
        {
            if (!released && heading.sqrMagnitude > .0001f) direction = heading.normalized;
        }

        public void Begin(Transform caster, Vector3 heading, float castHeight, float chargeDuration)
        {
            owner = caster; direction = heading.normalized; height = castHeight;
            duration = Mathf.Max(.05f, chargeDuration); started = Time.time;
            begun = true; released = false;
            properties = new MaterialPropertyBlock();
            Animate();
        }

        public void Release()
        {
            transform.position = LaunchPosition;
            released = true; releasedAt = Time.time;
            Animate();
        }

        private void LateUpdate()
        {
            if (!begun) return;
            if (!released && owner == null) { Destroy(gameObject); return; }
            if (released && Time.time - releasedAt >= fadeDuration) { Destroy(gameObject); return; }
            Animate();
        }

        private void Animate()
        {
            float t = Mathf.Clamp01((Time.time - started) / duration);
            float fade = released ? 1 - Mathf.Clamp01((Time.time - releasedAt) / fadeDuration) : 1;
            float reveal = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .8f));
            if (!released) transform.position = LaunchPosition;
            if (pattern != null)
            {
                // The ornament is a vertical plane perpendicular to the cast direction.
                pattern.transform.rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0, 0, 12 * t);
                float scale = diameter * Mathf.Lerp(.2f, 1, 1 - Mathf.Pow(1 - t, 3));
                pattern.transform.localScale = Vector3.one * scale * (1 + (1 - fade) * .16f);
                properties.SetFloat("_Reveal", reveal);
                properties.SetFloat("_Opacity", Mathf.SmoothStep(0, 1, t * 5) * fade * fade);
                pattern.SetPropertyBlock(properties);
            }
            if (castLight != null) castLight.intensity = 2.2f * reveal * fade;
        }
    }
}
