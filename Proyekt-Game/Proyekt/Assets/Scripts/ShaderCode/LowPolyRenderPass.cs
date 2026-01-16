using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ShaderCode
{
    public class LowPolyRenderPass : ScriptableRenderPass
    {
        private readonly Material _material;
        private LowPolyVolumeComponent _volumeComponent;

        private static readonly int ColorStepsID = Shader.PropertyToID("_ColorSteps");
        private static readonly int PixelationID = Shader.PropertyToID("_Pixelation");
        private static readonly int EdgeThicknessID = Shader.PropertyToID("_EdgeThickness");
        private static readonly int EdgeColorID = Shader.PropertyToID("_EdgeColor");
        private static readonly int EdgeIntensityID = Shader.PropertyToID("_EdgeIntensity");

        private class PassData
        {
            internal Material Material;
            internal TextureHandle Source;
            internal LowPolyVolumeComponent VolumeComponent;
        }

        public LowPolyRenderPass(Material mat)
        {
            _material = mat;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public void Setup()
        {
            var stack = VolumeManager.instance.stack;
            _volumeComponent = stack.GetComponent<LowPolyVolumeComponent>();
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!_material || !_volumeComponent || !_volumeComponent.IsActive())
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            // Skip if no valid camera color texture
            if (!resourceData.isActiveTargetBackBuffer)
                return;

            var source = resourceData.activeColorTexture;
            if (!source.IsValid())
                return;

            var descriptor = cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;

            var destination = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, 
                descriptor, 
                "_LowPolyTempRT", 
                false
            );

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Low Poly Effect", out var passData))
            {
                passData.Material = _material;
                passData.Source = source;
                passData.VolumeComponent = _volumeComponent;

                builder.UseTexture(source);
                builder.SetRenderAttachment(destination, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    // Set shader parameters
                    data.Material.SetFloat(ColorStepsID, data.VolumeComponent.colorSteps.value);
                    data.Material.SetFloat(PixelationID, data.VolumeComponent.pixelation.value);
                    data.Material.SetFloat(EdgeThicknessID, data.VolumeComponent.edgeThickness.value);
                    data.Material.SetColor(EdgeColorID, data.VolumeComponent.edgeColor.value);
                    data.Material.SetFloat(EdgeIntensityID, data.VolumeComponent.edgeIntensity.value);

                    Blitter.BlitTexture(context.cmd, data.Source, new Vector4(1, 1, 0, 0), data.Material, 0);
                });
            }

            // Copy back to camera target
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Low Poly Copy Back", out var passData))
            {
                passData.Material = _material;
                passData.Source = destination;

                builder.UseTexture(destination);
                builder.SetRenderAttachment(source, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.Source, new Vector4(1, 1, 0, 0), data.Material, 0);
                });
            }
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}