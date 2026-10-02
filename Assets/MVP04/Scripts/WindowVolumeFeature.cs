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
        [Range(1,3)] public int resolutionDivisor=2;
        [Range(16,96)] public int viewSteps=40;
        [Range(1,6)] public int lightSteps=2;
        [Range(0,1)] public float denoiseStrength=.9f;
        private WindowPass pass;
        private Vector3 airOffset;
        private double lastAirTime=-1;
        private bool airWasPlaying;

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
            pass.resolutionDivisor = resolutionDivisor;
            pass.viewSteps = viewSteps;
            pass.lightSteps = lightSteps;
            pass.denoiseStrength = denoiseStrength;
            AdvanceAirFlow();
            pass.airOffset = airOffset;
            renderer.EnqueuePass(pass);
        }

        private void AdvanceAirFlow()
        {
            bool playing=Application.isPlaying;
            double now=playing?Time.timeAsDouble:Time.realtimeSinceStartupAsDouble;
            if(lastAirTime>=0 && playing==airWasPlaying)
            {
                float elapsed=Mathf.Clamp((float)(now-lastAirTime),0,.1f);
                Vector3 direction=material.GetVector("_FlowDirection");
                airOffset+=direction.normalized*(Mathf.Max(0,material.GetFloat("_FlowSpeed"))*elapsed);
            }
            lastAirTime=now;airWasPlaying=playing;
        }

        private sealed class WindowPass : ScriptableRenderPass
        {
            public Material material;
            public int resolutionDivisor,viewSteps,lightSteps;
            public float denoiseStrength;
            public Vector3 airOffset;
            private readonly Vector4[] sources=new Vector4[3];
            private static readonly int VolumeTexture=Shader.PropertyToID("_ChapelVolumeTexture");
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
                    if (count < sources.Length && light != null && light.enabled && light.intensity > 0 &&
                        light.TryGetComponent<ChapelWindowLight>(out var window) && window.isActiveAndEnabled)
                    {
                        Vector3 position=light.transform.position;
                        sources[count++]=new Vector4(position.x,position.y,position.z,additionalIndex);
                    }
                    additionalIndex++;
                    if (additionalIndex >= UniversalRenderPipeline.maxVisibleAdditionalLights) break;
                }
                // Snapshot camera-specific bindings; Scene and Game cameras have different lists.
                var properties = new MaterialPropertyBlock();
                properties.SetInteger("_SourceCount", count);
                properties.SetVectorArray("_Sources", sources);
                properties.SetInteger("_ViewSteps", Mathf.Clamp(viewSteps,16,96));
                properties.SetInteger("_LightSteps", Mathf.Clamp(lightSteps,1,6));
                properties.SetVector("_AirFlowOffset",airOffset);
                properties.SetVector("_RoomMin", ChapelWindowLight.RoomMin);
                properties.SetVector("_RoomMax", ChapelWindowLight.RoomMax);
                var source = resources.activeColorTexture;
                var descriptor = graph.GetTextureDesc(source);
                var camera=frame.Get<UniversalCameraData>();
                int divisor=Mathf.Clamp(resolutionDivisor,1,3);
                int width=Mathf.Max(1,(camera.cameraTargetDescriptor.width+divisor-1)/divisor);
                int height=Mathf.Max(1,(camera.cameraTargetDescriptor.height+divisor-1)/divisor);
                var lowDescriptor=descriptor;
                lowDescriptor.name="MVP04 low resolution scattering";
                lowDescriptor.sizeMode=TextureSizeMode.Explicit;
                lowDescriptor.width=width;lowDescriptor.height=height;
                lowDescriptor.colorFormat=UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;
                lowDescriptor.msaaSamples=MSAASamples.None;
                lowDescriptor.clearBuffer=false;
                var lowVolume=graph.CreateTexture(lowDescriptor);
                descriptor.name = "MVP04 depth aware volume composite";
                descriptor.clearBuffer = false;
                var destination = graph.CreateTexture(descriptor);
                var parameters = new RenderGraphUtils.BlitMaterialParameters(source, lowVolume, material, 0, properties);
                using (var builder = graph.AddBlitPass(parameters, "Chapel / single scattering", returnBuilder: true))
                {
                    builder.UseTexture(resources.cameraDepthTexture);
                    // Includes the RGB cookie atlas, shared with opaque surface lighting.
                    builder.UseAllGlobalTextures(true);
                    if (resources.mainShadowsTexture.IsValid()) builder.UseTexture(resources.mainShadowsTexture);
                    if (resources.additionalShadowsTexture.IsValid()) builder.UseTexture(resources.additionalShadowsTexture);
                    if(denoiseStrength<=0)builder.SetGlobalTextureAfterPass(lowVolume,VolumeTexture);
                }
                // Filter only scattering/transmittance, before upsampling. Depth rejects
                // foreground/background mixing; radiance weights retain coloured shaft edges.
                if(denoiseStrength>0)
                {
                    var filterSource=lowVolume;
                    int iterations=divisor>1?2:1;
                    for(int iteration=0;iteration<iterations;iteration++)
                    {
                        var filterDescriptor=lowDescriptor;
                        filterDescriptor.name="MVP04 filtered scattering "+iteration;
                        var filtered=graph.CreateTexture(filterDescriptor);
                        var filterProperties=new MaterialPropertyBlock();
                        filterProperties.SetVector("_VolumeSize",new Vector4(width,height,1f/width,1f/height));
                        filterProperties.SetFloat("_FilterStride",iteration+1);
                        filterProperties.SetFloat("_DenoiseStrength",Mathf.Clamp01(denoiseStrength));
                        var filter=new RenderGraphUtils.BlitMaterialParameters(filterSource,filtered,material,1,filterProperties);
                        using(var builder=graph.AddBlitPass(filter,"Chapel / edge aware denoise "+iteration,returnBuilder:true))
                        {
                            builder.UseTexture(resources.cameraDepthTexture);
                            if(iteration==iterations-1)builder.SetGlobalTextureAfterPass(filtered,VolumeTexture);
                        }
                        filterSource=filtered;
                    }
                }
                var compositeProperties=new MaterialPropertyBlock();
                compositeProperties.SetVector("_VolumeSize",new Vector4(width,height,1f/width,1f/height));
                var composite=new RenderGraphUtils.BlitMaterialParameters(source,destination,material,2,compositeProperties);
                using(var builder=graph.AddBlitPass(composite,"Chapel / bilateral upsample",returnBuilder:true))
                {
                    builder.UseTexture(resources.cameraDepthTexture);
                    builder.UseGlobalTexture(VolumeTexture);
                }
                resources.cameraColor = destination;
            }
        }
    }
}
