using UnityEngine;

namespace MVP03
{
    public sealed class MagicImpact : MonoBehaviour
    {
        [SerializeField] private Transform flash;
        [SerializeField] private Transform ring;
        [SerializeField] private Light glowLight;
        [SerializeField] private float duration = .48f;
        private Renderer[] surfaces;
        private MaterialPropertyBlock properties;
        private float age;
        private static readonly int Opacity = Shader.PropertyToID("_Opacity");

        public void Configure(Transform sphere, Transform halo, Light pointLight)
        { flash = sphere; ring = halo; glowLight = pointLight; }

        private void Awake()
        {
            surfaces = GetComponentsInChildren<MeshRenderer>();
            properties = new MaterialPropertyBlock();
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);
            float fade = (1f - t) * (1f - t);
            if (flash != null) flash.localScale = Vector3.one * Mathf.Lerp(.28f, 1.4f, t);
            if (ring != null) ring.localScale = Vector3.one * Mathf.Lerp(.4f, 2.8f, t);
            properties.SetFloat(Opacity, fade);
            foreach (Renderer surface in surfaces) surface.SetPropertyBlock(properties);
            if (glowLight != null) glowLight.intensity = 9f * fade;
            // Give the detached burst enough time to finish its final particles.
            if (age >= duration + .25f) Destroy(gameObject);
        }
    }
}
