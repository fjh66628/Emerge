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
                var source = resources.activeColorTexture;
                var descriptor = graph.GetTextureDesc(source);
                descriptor.name = "MVP04 window scattering";
                descriptor.clearBuffer = false;
                var destination = graph.CreateTexture(descriptor);
                var parameters = new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0);
                using (var builder = graph.AddBlitPass(parameters, "Rose window / bounded volume", returnBuilder: true))
                {
                    builder.UseTexture(resources.cameraDepthTexture);
                    // Keep the actual sun shadow map alive during the volume integration.
                    if (resources.mainShadowsTexture.IsValid()) builder.UseTexture(resources.mainShadowsTexture);
                }
                resources.cameraColor = destination;
            }
        }
    }
}
