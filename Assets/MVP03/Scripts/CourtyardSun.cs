using UnityEngine;

namespace MVP03
{
    // Asset-backed settings keep adjustments made during Play after returning to the Editor.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Light))]
    public sealed class CourtyardSun : MonoBehaviour
    {
        public CourtyardSunSettings settings;
        private Light source;
        public Light Source => source != null ? source : (source = GetComponent<Light>());

        private void OnEnable() => Apply();
        private void Update() => Apply();

        public void Apply()
        {
            if (settings == null) return;
            var light = Source;
            Quaternion orientation = Quaternion.Euler(settings.rotation);
            if (Quaternion.Angle(transform.rotation, orientation) > .001f) transform.rotation = orientation;
            if (light.type != LightType.Directional) light.type = LightType.Directional;
            if (light.enabled != settings.sourceEnabled) light.enabled = settings.sourceEnabled;
            if (light.color != settings.colour) light.color = settings.colour;
            float intensity = Mathf.Max(0, settings.intensity);
            if (light.intensity != intensity) light.intensity = intensity;
            float strength = Mathf.Clamp01(settings.shadowStrength);
            if (light.shadowStrength != strength) light.shadowStrength = strength;
            if (light.shadows != LightShadows.Soft) light.shadows = LightShadows.Soft;
        }
    }
}
