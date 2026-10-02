using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace MVP04
{
    public sealed class WindowVolumeFeature : ScriptableRendererFeature
    {
        public Material material;
        private WindowPass pass;

        public override void Create()
        {
            pass = new WindowPass { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing };
            pass.ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData data)
        {
            if (material == null || data.cameraData.cameraType == CameraType.Preview ||
                data.cameraData.cameraType == CameraType.Reflection) return;
            pass.material = material;
            renderer.EnqueuePass(pass);
        }

        private sealed class WindowPass : ScriptableRenderPass
        {
            public Material material;
            private readonly Vector4[] sideSources = new Vector4[8], sideWindows = new Vector4[8];
            public WindowPass() => requiresIntermediateTexture = true;

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frame)
            {
                var resources = frame.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer) return;
                var lights = frame.Get<UniversalLightData>();
                int count = 0, additionalIndex = 0;
                // URP packs visible additional lights in order, excluding its main directional light.
                // Use these camera-specific indices, rather than object/per-renderer light indices.
                for (int i = 0; i < lights.visibleLights.Length; i++)
                {
                    if (i == lights.mainLightIndex) continue;
                    var light = lights.visibleLights[i].light;
                    if (count < 8 && light != null && light.enabled && light.intensity > 0 &&
                        light.TryGetComponent<SideWindowLight>(out var window) && window.isActiveAndEnabled)
                    {
                        Vector3 p = light.transform.position, w = window.windowCentre;
                        sideSources[count] = new Vector4(p.x, p.y, p.z, additionalIndex);
                        sideWindows[count++] = new Vector4(w.x, w.y, w.z, window.scattering * light.intensity / 220f);
                    }
                    additionalIndex++;
                    if (additionalIndex >= UniversalRenderPipeline.maxVisibleAdditionalLights) break;
                }
                material.SetInt("_SideCount", count);
                material.SetVectorArray("_SideSources", sideSources);
                material.SetVectorArray("_SideWindows", sideWindows);
                var source = resources.activeColorTexture;
                var descriptor = graph.GetTextureDesc(source);
                descriptor.name = "MVP04 window scattering";
                descriptor.clearBuffer = false;
                var destination = graph.CreateTexture(descriptor);
                var parameters = new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0);
                using (var builder = graph.AddBlitPass(parameters, "Chapel windows / bounded volumes", returnBuilder: true))
                {
                    builder.UseTexture(resources.cameraDepthTexture);
                    // Keep the actual sun shadow map alive during the volume integration.
                    if (resources.mainShadowsTexture.IsValid()) builder.UseTexture(resources.mainShadowsTexture);
                    if (resources.additionalShadowsTexture.IsValid()) builder.UseTexture(resources.additionalShadowsTexture);
                }
                resources.cameraColor = destination;
            }
        }
    }
}
