using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShaderCode
{
    [System.Serializable]
    [VolumeComponentMenu("Post-processing/Low Poly Effect")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public class LowPolyVolumeComponent : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Enable/disable the effect")]
        public BoolParameter enabled = new BoolParameter(false);
    
        [Tooltip("Number of color levels (lower = more posterized). 8-16 looks good for low-poly.")]
        public ClampedIntParameter colorSteps = new ClampedIntParameter(12, 2, 64);
    
        [Tooltip("Pixelation amount (1 = none, higher = more pixelated). 1-3 is subtle.")]
        public ClampedFloatParameter pixelation = new ClampedFloatParameter(1f, 1f, 10f);
    
        [Tooltip("Edge detection intensity (0 = off, 1 = full). Adds outlines.")]
        public ClampedFloatParameter edgeIntensity = new ClampedFloatParameter(0f, 0f, 1f);
    
        [Tooltip("Edge thickness")]
        public ClampedFloatParameter edgeThickness = new ClampedFloatParameter(1f, 0.5f, 3f);
    
        [Tooltip("Edge color (usually black or dark)")]
        public ColorParameter edgeColor = new ColorParameter(Color.black, true, false, true);

        public bool IsActive() => enabled.value && (colorSteps.value < 64 || pixelation.value > 1f || edgeIntensity.value > 0f);
        public bool IsTileCompatible() => false;
    }
}