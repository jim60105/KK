using System;
using UnityEngine;

namespace KK_TransparentBackground {
    public class TransparentCameraController : MonoBehaviour {
        private Material TransparentMaterial;
        private bool enable = true;
        private float transparency = 1f;

        public float Transparency {
            get => transparency;
            set {
                transparency = value;
                if (null != TransparentMaterial) {
                    TransparentMaterial.SetFloat("_AlphaScale", value);
                    KK_TransparentBackground.Logger.LogDebug($"Set transparency to {TransparentMaterial.GetFloat("_AlphaScale") * 100}%");
                }
            }
        }

        public bool Enable {
            get => enable;
            set {
                enable = value;
                if (null == TransparentMaterial) GetMaterial();
            }
        }

        public void Awake() => GetMaterial();

        void OnRenderImage(RenderTexture from, RenderTexture to) {
            if (enable && null != TransparentMaterial) {
                Graphics.Blit(from, to, TransparentMaterial);
            } else {
                Graphics.Blit(from, to);
            }
        }

        public void GetMaterial() {
            //TransparentMaterial
            try {
                if (AssetBundle.LoadFromMemory(Properties.Resources.transparent) is AssetBundle assetBundle) {
                    TransparentMaterial = assetBundle.LoadAsset<Material>("TransparentWindowMaterial");
                    Transparency = transparency;

                    assetBundle.Unload(false);
                } else {
                    throw new Exception();
                }
            } catch (Exception) {
                KK_TransparentBackground.Logger.LogError("Load AlphaBlend material assetBundle faild");
                enable = false;
            }
        }
    }
}
