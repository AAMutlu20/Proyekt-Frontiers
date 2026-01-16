using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShaderCode
{
    [Serializable]
    public class LowPolyRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader shader;
        private Material _material;
        private LowPolyRenderPass _renderPass;

        public override void Create()
        {
            if (!shader)
            {
                Debug.LogError("Low Poly Effect: Shader not assigned!");
                return;
            }

            _material = CoreUtils.CreateEngineMaterial(shader);
            _renderPass = new LowPolyRenderPass(_material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_renderPass == null || !_material)
                return;

            _renderPass.Setup();
            renderer.EnqueuePass(_renderPass);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;
            _renderPass?.Dispose();
            CoreUtils.Destroy(_material);
        }
    }
}