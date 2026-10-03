using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace MVP03
{
    // One Visual Effect Graph instance per elevation, born from the merged GPU footprint.
    // Graph particles own their 3D velocity, curl turbulence and lifetime on the GPU.
    internal sealed class GroundFire : System.IDisposable
    {
        private const int MaximumLights = 4;
        private readonly Material material;
        private readonly Vector4 atlas;
        private readonly VisualEffectAsset asset;
        private VisualEffect spare;
        private readonly Dictionary<ClickPuddles.Patch, VisualEffect> fields = new Dictionary<ClickPuddles.Patch, VisualEffect>();
        private readonly List<Vector3> anchors = new List<Vector3>();
        private readonly Light[] lights = new Light[MaximumLights];
        private readonly List<Vector3> selected = new List<Vector3>(MaximumLights);
        private float nextSelection;
        public int LightCount { get; private set; }
        public int EffectCount => fields.Count;

        public GroundFire(Material fireMaterial, Vector4 worldAtlas, VisualEffectAsset graph)
        {
            material = fireMaterial; atlas = worldAtlas; asset = graph;
            spare = CreateEffect();
            for (int i = 0; i < lights.Length; i++)
            {
                var root = new GameObject("Ground fire / pooled warm light " + i) { hideFlags = HideFlags.DontSave };
                var light = root.AddComponent<Light>();
                light.type = LightType.Point; light.shadows = LightShadows.None;
                light.color = new Color(1, .62f, .055f); light.enabled = false;
                lights[i] = light;
            }
        }

        private VisualEffect CreateEffect()
        {
            var root = new GameObject("Ground fire / GPU turbulence") { hideFlags = HideFlags.DontSave };
            root.SetActive(false);
            var effect = root.AddComponent<VisualEffect>(); effect.visualEffectAsset = asset;
            effect.SetTexture("Coverage", Texture2D.blackTexture);
            effect.SetFloat("SpawnRate", 0);
            effect.SetVector4("Atlas", atlas);
            effect.SetVector3("BoundsCenter", new Vector3(atlas.x + atlas.z * .5f, 2, atlas.y + atlas.w * .5f));
            effect.SetVector3("BoundsSize", new Vector3(atlas.z, 6, atlas.w));
            root.SetActive(true); effect.Reinit();
            return effect;
        }

        public void Sync(ClickPuddles.Patch patch)
        {
            bool created = !fields.TryGetValue(patch, out var effect);
            if (created) { effect = spare != null ? spare : CreateEffect(); spare = null; fields.Add(patch,effect); }
            Bounds ground = patch.renderer.bounds;
            effect.SetTexture("Coverage", patch.support);
            effect.SetVector4("Atlas", atlas);
            effect.SetVector4("Region", new Vector4(ground.min.x,ground.min.z,ground.size.x,ground.size.z));
            effect.SetFloat("Elevation", patch.height);
            effect.SetVector3("BoundsCenter", new Vector3(ground.center.x, patch.height + 2, ground.center.z));
            effect.SetVector3("BoundsSize", new Vector3(ground.size.x + 4, 6, ground.size.z + 4));
            ApplySettings(effect, ground.size.x * ground.size.z);
            if (created) { effect.gameObject.SetActive(true); effect.Reinit(); effect.Play(); }
        }

        private void ApplySettings(VisualEffect effect, float area)
        {
            effect.SetFloat("SpawnRate", Mathf.Clamp(area * 90 * material.GetFloat("_FlameDensity"), 50, 12000));
            effect.SetFloat("FlameHeight", material.GetFloat("_FlameHeight"));
            effect.SetFloat("FlameWidth", material.GetFloat("_FlameWidth"));
            effect.SetVector3("ParticlePivot", material.GetVector("_ParticlePivot"));
            effect.SetFloat("RiseSpeed", material.GetFloat("_FlameSpeed"));
            effect.SetFloat("AnimationFPS", material.GetFloat("_AnimationFPS"));
            effect.SetFloat("Brightness", material.GetFloat("_Emission"));
            effect.SetFloat("Threshold", material.GetFloat("_FireThreshold"));
            effect.SetFloat("EdgeSoftness", material.GetFloat("_EdgeSoftness"));
            effect.SetFloat("Turbulence", material.GetFloat("_Turbulence"));
            effect.SetFloat("TurbulenceFrequency", material.GetFloat("_TurbulenceFrequency"));
        }

        public void AddLightAnchor(Vector3 position)
        {
            // Lights are representative samples of the region, not one light per click.
            foreach (var anchor in anchors) if ((anchor - position).sqrMagnitude < 2.5f * 2.5f) return;
            anchors.Add(position); nextSelection = 0;
        }

        public void Tick(Camera camera)
        {
            foreach (var pair in fields)
            {
                Bounds bounds = pair.Key.renderer.bounds;
                ApplySettings(pair.Value, bounds.size.x * bounds.size.z);
            }
            if (Time.unscaledTime >= nextSelection)
            {
                nextSelection = Time.unscaledTime + .25f; selected.Clear();
                if (camera != null && fields.Count != 0)
                    for (int slot = 0; slot < MaximumLights; slot++)
                    {
                        float best = float.PositiveInfinity; Vector3 point = default; bool found = false;
                        foreach (var anchor in anchors)
                        {
                            if (selected.Contains(anchor)) continue;
                            float distance = (camera.transform.position - anchor).sqrMagnitude;
                            if (distance < best) { best = distance; point = anchor; found = true; }
                        }
                        if (!found) break;
                        selected.Add(point);
                    }
            }
            LightCount = 0;
            float intensity = Mathf.Max(0, material.GetFloat("_LightIntensity"));
            for (int i = 0; i < lights.Length; i++)
            {
                var light = lights[i]; light.enabled = i < selected.Count && intensity > .001f;
                if (!light.enabled) continue;
                LightCount++;
                light.transform.position = selected[i] + Vector3.up * .65f;
                light.range = material.GetFloat("_LightRange");
                light.intensity = intensity * (.82f + Mathf.PerlinNoise(i * 7.1f, Time.time * 2.4f) * .36f);
            }
        }

        public void Remove(ClickPuddles.Patch patch)
        {
            if (!fields.TryGetValue(patch, out var renderer)) return;
            renderer.Stop(); renderer.gameObject.SetActive(false);
            if (spare == null) spare = renderer; else Object.Destroy(renderer.gameObject);
            fields.Remove(patch);
        }
        public void Clear()
        {
            foreach (var effect in fields.Values)
            {
                effect.Stop(); effect.gameObject.SetActive(false);
                if (spare == null) spare = effect; else Object.Destroy(effect.gameObject);
            }
            fields.Clear(); anchors.Clear(); selected.Clear(); LightCount = 0; nextSelection = 0;
            foreach (var light in lights) if (light != null) light.enabled = false;
        }
        public void Dispose()
        {
            Clear(); foreach (var light in lights) if (light != null) Object.Destroy(light.gameObject);
            if (spare != null) Object.Destroy(spare.gameObject); spare = null;
        }
    }
}
