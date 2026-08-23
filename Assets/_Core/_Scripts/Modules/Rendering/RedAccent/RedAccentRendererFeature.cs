using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Modules.Rendering.RedAccent {
    /// <summary>
    /// Applies the game's monochrome-with-red-accent look as a final camera pass.
    /// It operates on the rendered frame, leaving scene materials and textures unchanged.
    /// </summary>
    public sealed class RedAccentRendererFeature : ScriptableRendererFeature {
        [System.Serializable]
        private sealed class Settings {
            [Tooltip("Max hue distance from pure red (0-1 circle). ~0.04 ≈ 14°. Lower = stricter true-red only.")]
            [Range(0.01f, 0.15f)] public float redThreshold = 0.04f;
            [Tooltip("Soft falloff past the hue threshold. Keep small to avoid orange bleed.")]
            [Range(0.001f, 0.1f)] public float redFeather = 0.02f;
            [Range(0f, 2f)] public float redSaturation = 1.3f;
            [Range(0.5f, 2.5f)] public float grayscaleContrast = 1.3f;
            [Range(-2f, 2f)] public float grayscaleExposure = -0.25f;
            [Range(0f, 1f)] public float vignetteStrength = 0.28f;
        }

        [SerializeField] private Settings _settings = new();

        private Material _material;
        private RedAccentPass _pass;

        public override void Create() {
            var shader = Resources.Load<Shader>("Shaders/RedAccentPostProcess");
            if (shader == null) {
                Debug.LogError("Red Accent post-process shader was not found in Resources/Shaders.");
                return;
            }

            _material = CoreUtils.CreateEngineMaterial(shader);
            _pass = new RedAccentPass(_material, _settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (_pass == null
                || !Application.isPlaying
                || renderingData.cameraData.cameraType != CameraType.Game) {
                return;
            }

            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing) {
            CoreUtils.Destroy(_material);
        }

        private sealed class RedAccentPass : ScriptableRenderPass {
            private static readonly int RedThresholdId = Shader.PropertyToID("_RedThreshold");
            private static readonly int RedFeatherId = Shader.PropertyToID("_RedFeather");
            private static readonly int RedSaturationId = Shader.PropertyToID("_RedSaturation");
            private static readonly int GrayscaleContrastId = Shader.PropertyToID("_GrayscaleContrast");
            private static readonly int GrayscaleExposureId = Shader.PropertyToID("_GrayscaleExposure");
            private static readonly int VignetteStrengthId = Shader.PropertyToID("_VignetteStrength");

            private readonly Material _material;
            private readonly Settings _settings;

            public RedAccentPass(Material material, Settings settings) {
                _material = material;
                _settings = settings;
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
                var resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer) {
                    return;
                }

                ApplySettings();
                var source = resourceData.activeColorTexture;
                var descriptor = renderGraph.GetTextureDesc(source);
                descriptor.name = "Red Accent Post Process";
                descriptor.clearBuffer = false;
                var destination = renderGraph.CreateTexture(descriptor);

                RenderGraphUtils.BlitMaterialParameters parameters = new(source, destination, _material, 0);
                renderGraph.AddBlitPass(parameters, "Red Accent Post Process");
                resourceData.cameraColor = destination;
            }

            private void ApplySettings() {
                _material.SetFloat(RedThresholdId, _settings.redThreshold);
                _material.SetFloat(RedFeatherId, _settings.redFeather);
                _material.SetFloat(RedSaturationId, _settings.redSaturation);
                _material.SetFloat(GrayscaleContrastId, _settings.grayscaleContrast);
                _material.SetFloat(GrayscaleExposureId, _settings.grayscaleExposure);
                _material.SetFloat(VignetteStrengthId, _settings.vignetteStrength);
            }
        }
    }
}
