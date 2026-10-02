using UnityEngine;

namespace MVP04
{
    [DisallowMultipleComponent]
    public sealed class CandleSway : MonoBehaviour
    {
        [SerializeField] private Light glowLight;
        [SerializeField] private Renderer flameRenderer;
        [Header("Candle motion")]
        [SerializeField, Range(0, .6f), Tooltip("Maximum fractional brightness variation around the saved light intensity.")]
        private float intensityVariation = .3f;
        [SerializeField, Range(0, .08f), Tooltip("Horizontal light-source sway in local metres.")]
        private float swayDistance = .025f;
        [SerializeField, Range(0, .5f)] private float emissionVariation = .24f;
        [SerializeField, Range(.1f, 4)] private float motionSpeed = 1.6f;
        [SerializeField] private float noisePhase;

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock properties, originalProperties;
        private Vector3 restPosition;
        private float restIntensity;
        private Color restEmission;
        private bool initialized, hasEmission;

        public void Configure(Light light, Renderer flames, float phase)
        {
            Restore();
            glowLight = light;
            flameRenderer = flames;
            noisePhase = phase;
            if (Application.isPlaying) Capture();
        }

        private void OnEnable()
        {
            if (Application.isPlaying) Capture();
        }

        private void Capture()
        {
            if (initialized) return;
            if (glowLight != null)
            {
                restPosition = glowLight.transform.localPosition;
                restIntensity = glowLight.intensity;
            }
            hasEmission = flameRenderer != null && flameRenderer.sharedMaterial != null &&
                          flameRenderer.sharedMaterial.HasProperty(EmissionColor);
            if (hasEmission)
            {
                properties ??= new MaterialPropertyBlock();
                originalProperties ??= new MaterialPropertyBlock();
                flameRenderer.GetPropertyBlock(properties);
                flameRenderer.GetPropertyBlock(originalProperties);
                restEmission = flameRenderer.sharedMaterial.GetColor(EmissionColor);
            }
            initialized = true;
        }

        private float Noise(float time, float channel) =>
            Mathf.Clamp(Mathf.PerlinNoise(noisePhase + channel, time) * 2 - 1, -1, 1);

        private void Update()
        {
            if (!initialized) return;
            float time = Time.time * motionSpeed;
            // Correlated drift plus weaker fast flutter; no independent random jumps.
            float flicker = .65f * Noise(time * .9f, 0) +
                            .25f * Noise(time * 2.7f, 17.3f) +
                            .10f * Noise(time * 7.1f, 43.7f);
            if (glowLight != null)
            {
                glowLight.intensity = restIntensity * (1 + intensityVariation * flicker);
                Vector3 drift = new Vector3(Noise(time * .8f, 73.1f),
                    .3f * Noise(time * 1.1f, 101.9f), Noise(time * .67f, 131.7f));
                glowLight.transform.localPosition = restPosition + drift * swayDistance;
            }
            if (hasEmission)
            {
                Color emission = restEmission * (1 + emissionVariation * flicker);
                emission.a = restEmission.a;
                properties.SetColor(EmissionColor, emission);
                flameRenderer.SetPropertyBlock(properties);
            }
        }

        private void OnDisable() => Restore();

        private void Restore()
        {
            if (!initialized) return;
            if (glowLight != null)
            {
                glowLight.intensity = restIntensity;
                glowLight.transform.localPosition = restPosition;
            }
            if (hasEmission && flameRenderer != null) flameRenderer.SetPropertyBlock(originalProperties);
            initialized = false;
        }
    }
}
