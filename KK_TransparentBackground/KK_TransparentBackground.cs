/*
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMM               MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM    M7    MZ    MMO    MMMMM
MMM               MMMMMMMMMMMMM   MMM     MMMMMMMMMM    M$    MZ    MMO    MMMMM
MMM               MMMMMMMMMM       ?M     MMMMMMMMMM    M$    MZ    MMO    MMMMM
MMMMMMMMMMMM8     MMMMMMMM       ~MMM.    MMMMMMMMMM    M$    MZ    MMO    MMMMM
MMMMMMMMMMMMM     MMMMM        MMM                 M    M$    MZ    MMO    MMMMM
MMMMMMMMMMMMM     MM.         ZMMMMMM     MMMM     MMMMMMMMMMMMZ    MMO    MMMMM
MMMMMMMMMMMMM     MM      .   ZMMMMMM     MMMM     MMMMMMMMMMMM?    MMO    MMMMM
MMMMMMMMMMMMM     MMMMMMMM    $MMMMMM     MMMM     MMMMMMMMMMMM?    MM8    MMMMM
MMMMMMMMMMMMM     MMMMMMMM    7MMMMMM     MMMM     MMMMMMMMMMMMI    MM8    MMMMM
MMM               MMMMMMMM    7MMMMMM     MMMM    .MMMMMMMMMMMM.    MMMM?ZMMMMMM
MMM               MMMMMMMM.   ?MMMMMM     MMMM     MMMMMMMMMM ,:MMMMMM?    MMMMM
MMM           ..MMMMMMMMMM    =MMMMMM     MMMM     M$ MM$M7M $MOM MMMM     ?MMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM .+Z: M   :M M  MM   ?MMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
*/

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Kirurobo;
using System.Collections;
using UnityEngine;

namespace KK_TransparentBackground {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("Koikatu")]
    public class KK_TransparentBackground : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Transparent Background";
        internal const string GUID = "com.jim60105.kk.transparentbackground";
        internal const string PLUGIN_VERSION = "20.05.19.0";
        internal const string PLUGIN_RELEASE_VERSION = "0.0.1";

        public static ConfigEntry<KeyboardShortcut> Hotkey { get; set; }
        internal static new ManualLogSource Logger;
        private static Material m_Material;
        public void Awake() {
            Logger = base.Logger;
            Hotkey = Config.Bind<KeyboardShortcut>("Config", "Enable", new KeyboardShortcut(KeyCode.None));

            //TransparentMaterial
            if (AssetBundle.LoadFromMemory(Properties.Resources.transparent) is AssetBundle assetBundle) {
                m_Material = assetBundle.LoadAsset<Material>("TransparentWindowMaterial");
                m_Material.color = new Color(255, 255, 255, 255);

                assetBundle.Unload(false);
            } else {
                Logger.LogError("Load assetBundle faild");
            }
        }

        private WindowController Window;

        public void Update() {
            if (Hotkey.Value.IsDown()) {
                StartCoroutine(TransparentCoroutine());
            }
        }

        private IEnumerator TransparentCoroutine() {
            while (Screen.fullScreen) {
                Screen.fullScreen = false;

                yield return null;
            }

            Window = Camera.main.gameObject.GetComponent<WindowController>() ?? Camera.main.gameObject.AddComponent<WindowController>();
            Window.TransparentMaterial = m_Material;
            Window.isTopmost ^= true;
            Window.isTransparent ^= true;

            yield break;
        }
    }
}

