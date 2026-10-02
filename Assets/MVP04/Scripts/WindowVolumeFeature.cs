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
            public WindowPass() => requiresIntermediateTexture = true;

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frame)
            {
                var resources = frame.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer) return;
                var lights = frame.Get<UniversalLightData>();
                int sourceIndex = -1, additionalIndex = 0;
                Vector3 sourcePosition = Vector3.zero;
                // URP packs visible additional lights in order, excluding its main directional light.
                // Use these camera-specific indices, rather than object/per-renderer light indices.
                for (int i = 0; i < lights.visibleLights.Length; i++)
                {
                    if (i == lights.mainLightIndex) continue;
                    var light = lights.visibleLights[i].light;
                    if (light != null && light.enabled && light.intensity > 0 &&
                        light.TryGetComponent<ChapelWindowLight>(out var window) && window.isActiveAndEnabled)
                    {
                        sourceIndex = additionalIndex;
                        sourcePosition = light.transform.position;
                        break;
                    }
                    additionalIndex++;
                    if (additionalIndex >= UniversalRenderPipeline.maxVisibleAdditionalLights) break;
                }
                // Snapshot camera-specific bindings; Scene and Game cameras have different lists.
                var properties = new MaterialPropertyBlock();
                properties.SetInteger("_SourceIndex", sourceIndex);
                properties.SetVector("_SourcePosition", sourcePosition);
                properties.SetVector("_RoomMin", ChapelWindowLight.RoomMin);
                properties.SetVector("_RoomMax", ChapelWindowLight.RoomMax);
                var source = resources.activeColorTexture;
                var descriptor = graph.GetTextureDesc(source);
                descriptor.name = "MVP04 window scattering";
                descriptor.clearBuffer = false;
                var destination = graph.CreateTexture(descriptor);
                var parameters = new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0, properties);
                using (var builder = graph.AddBlitPass(parameters, "Chapel / single scattering", returnBuilder: true))
                {
                    builder.UseTexture(resources.cameraDepthTexture);
                    // Includes the RGB cookie atlas, shared with opaque surface lighting.
                    builder.UseAllGlobalTextures(true);
                    if (resources.mainShadowsTexture.IsValid()) builder.UseTexture(resources.mainShadowsTexture);
                    if (resources.additionalShadowsTexture.IsValid()) builder.UseTexture(resources.additionalShadowsTexture);
                }
                resources.cameraColor = destination;
            }
        }
    }
}
