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
        public RawImage RawImage;
        public GraphicsFormat GraphicsFormat = GraphicsFormat.B10G11R11_UFloatPack32;

        [NonSerialized]
        public RenderTexture ScreenOverlayBlurTexture;

        public Camera Camera;
        public void Start()
        {
            ScreenOverlayBlurTexture = new RenderTexture(Screen.width, Screen.height, 16, GraphicsFormat.B10G11R11_UFloatPack32);
            RawImage.texture = ScreenOverlayBlurTexture;
        }

        public void DoBlur()
        {
            ScreenOverlayBlurTexture = new RenderTexture(Screen.width, Screen.height, 16, GraphicsFormat.B10G11R11_UFloatPack32);
            RawImage.texture = ScreenOverlayBlurTexture;
            KawaseCamBlur blurFeature = GetKawaseBlur(Camera);
            blurFeature.settings.toRenderTexture = ScreenOverlayBlurTexture;
          //  blurFeature.settings.DisableAfterRender = true;
            blurFeature.SetActive(true);
            RawImage.texture = ScreenOverlayBlurTexture;
        }

        private KawaseCamBlur GetKawaseBlur(Camera cam)
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