using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Cauldron.Exodus.Service
{
    public class UIBlurService : MonoBehaviour
    {
        public GraphicsFormat gfxFormat = GraphicsFormat.B10G11R11_UFloatPack32;
        public RawImage RawImage;
        public Camera Camera;

        [NonSerialized]
        public RenderTexture ScreenOverlayBlurTexture;


        public void DoBlur()
        {
            ScreenOverlayBlurTexture = new RenderTexture(Screen.width, Screen.height, 16, gfxFormat);
            RawImage.texture = ScreenOverlayBlurTexture;

            KawaseCamBlur blurFeature = GetKawaseBlurRenderFeature(Camera);
            blurFeature.settings.toRenderTexture = ScreenOverlayBlurTexture;
            blurFeature.SetActive(true);
        }

        private KawaseCamBlur GetKawaseBlurRenderFeature(Camera cam)
        {
            ScriptableRenderer scriptableRenderer = cam.GetUniversalAdditionalCameraData().scriptableRenderer;
            var property = typeof(ScriptableRenderer).GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
            List<ScriptableRendererFeature> features = property.GetValue(scriptableRenderer) as List<ScriptableRendererFeature>;
            KawaseCamBlur blurFeature = null;

            foreach (ScriptableRendererFeature feature in features)
            {
                if (feature is KawaseCamBlur blur)
                {
                    blurFeature = blur;
                }
            }

            return blurFeature;
        }
    }
}