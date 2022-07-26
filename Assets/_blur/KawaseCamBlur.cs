using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Iterated on from https://github.com/Unity-Technologies/UniversalRenderingExamples
public class KawaseCamBlur : ScriptableRendererFeature
{
    [System.Serializable]
    public class KawaseCamBlurSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material blurMaterial = null;
        public RenderTexture toRenderTexture;

        [Range(2,15)]
        public int NumPasses = 1;

        [Range(0f,30f)]
        public float KernelSize = 0.5f;

        [Range(1,4)]
        public float DownSample = 1;

        public bool DisableAfterRender = false;
        public ScriptableRendererFeature Feature;
    }

    public KawaseCamBlurSettings settings = new KawaseCamBlurSettings();

    class CustomRenderPass : ScriptableRenderPass
    {
        // Two textures are used to swap in and out during the multiple passes
        private int _tmpId1 = Shader.PropertyToID("tmpCamKawase_RT1");
        private int _tmpId2  = Shader.PropertyToID("tmpCamKawase_RT2");
        private readonly int KERNEL_SIZE_ID = Shader.PropertyToID("_kernelSize");

        private readonly string _profilerTag;

        private RenderTargetIdentifier _tmpRT1;
        private RenderTargetIdentifier _tmpRT2;

        private RenderTargetIdentifier _cameraColorTexture;

        public KawaseCamBlurSettings Settings;

        public CustomRenderPass(string profilerTag, KawaseCamBlurSettings settings)
        {
            Settings = settings;
            _profilerTag = profilerTag;
        }

        private static int ceratedRTCount = 0;
        private bool created = false;

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (Settings.toRenderTexture != null)
            {
                if (Settings.toRenderTexture.width != Screen.width)
                {
                    Settings.toRenderTexture.Release();
                    Settings.toRenderTexture.width = Screen.width;
                    Settings.toRenderTexture.height = Screen.height;
                    Settings.toRenderTexture.Create();
                }
            }

            // We're using this blur on multiuple cameras, using static int count to ensure that
            // the RT identifiers are unique for each render pass
            if (!created)
            {
                _tmpId1 = Shader.PropertyToID("tmpCamKawase_RT1" + ceratedRTCount.ToString());
                _tmpId2  = Shader.PropertyToID("tmpCamKawase_RT2" + ceratedRTCount.ToString());
                created = true;
                ceratedRTCount++;
            }

            int width = Mathf.FloorToInt((float)cameraTextureDescriptor.width / Settings.DownSample);
            int height = Mathf.FloorToInt((float)cameraTextureDescriptor.height / Settings.DownSample);

            cmd.GetTemporaryRT(_tmpId1, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(_tmpId2, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            _tmpRT1 = new RenderTargetIdentifier(_tmpId1);
            _tmpRT2 = new RenderTargetIdentifier(_tmpId2);
            
            ConfigureTarget(_tmpRT1);
            ConfigureTarget(_tmpRT2);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            _cameraColorTexture = renderingData.cameraData.renderer.cameraColorTarget;
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

            // MATT: Not entirely sure why this is doing this and how necessary it is...
            // could it break future screen space effects if we lose the depth buffer?
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            // first pass
            cmd.SetGlobalFloat(KERNEL_SIZE_ID, Settings.KernelSize);
            cmd.Blit(_cameraColorTexture, _tmpRT1, Settings.blurMaterial);

            for (var i=1; i<Settings.NumPasses-1; i++)
            {
                cmd.SetGlobalFloat(KERNEL_SIZE_ID, Settings.KernelSize * i);
                cmd.Blit(_tmpRT1, _tmpRT2, Settings.blurMaterial);

                // swap RT references
                (_tmpRT1, _tmpRT2) = (_tmpRT2, _tmpRT1);
            }

            // Final pass
            if (Settings.toRenderTexture != null)
            {
                cmd.Blit(_tmpRT1, Settings.toRenderTexture, Settings.blurMaterial);
            }
            else
            {
                cmd.Blit(_tmpRT1, _cameraColorTexture, Settings.blurMaterial);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (Settings.DisableAfterRender)
            {
                Settings.Feature.SetActive(false);
            }
        }
    }

    private CustomRenderPass _scriptablePass;


    public override void Create()
    {
        _scriptablePass = new CustomRenderPass(this.name, settings)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents
        };
    }

    public void UpdateSettings(int numPasses, float downSample, float kernelSize)
    {
        _scriptablePass.Settings.NumPasses = numPasses;
        _scriptablePass.Settings.DownSample = downSample;
        _scriptablePass.Settings.KernelSize = kernelSize;

        _scriptablePass.renderPassEvent = _scriptablePass.Settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _scriptablePass.Settings.Feature = this;
        renderer.EnqueuePass(_scriptablePass);
    }
}