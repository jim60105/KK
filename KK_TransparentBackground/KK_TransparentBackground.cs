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
    public class KK_TransparentBackground : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Transparent Background";
        internal const string GUID = "com.jim60105.kk.transparentbackground";
        internal const string PLUGIN_VERSION = "20.05.19.1";
        internal const string PLUGIN_RELEASE_VERSION = "0.0.2";

        internal static new ManualLogSource Logger;
        public static ConfigEntry<KeyboardShortcut> Hotkey { get; set; }
        public static ConfigEntry<bool> ClickThrough { get; set; }
        private static Material material;
        private WindowController Window;
        public void Awake() {
            Logger = base.Logger;
            Hotkey = Config.Bind<KeyboardShortcut>("Config", "Enable", new KeyboardShortcut(KeyCode.None));
            ClickThrough = Config.Bind<bool>("Config", "Click Through", true, "If you don’t want to click to the back, set to False");

            ClickThrough.SettingChanged += ClickThrough_SettingChanged;

            //TransparentMaterial
            if (AssetBundle.LoadFromMemory(Properties.Resources.transparent) is AssetBundle assetBundle) {
                material = assetBundle.LoadAsset<Material>("TransparentWindowMaterial");
                material.color = new Color(255, 255, 255, 255);

                assetBundle.Unload(false);
            } else {
                Logger.LogError("Load assetBundle faild");
            }
        }

        private void ClickThrough_SettingChanged(object sender, System.EventArgs e) {
            if (null != Window) Window.blockClickThrough = !ClickThrough.Value;
        }

        public void Update() {
            if (Hotkey.Value.IsDown()) {
                if (null == Camera.main) return;
                StartCoroutine(TransparentCoroutine());
            }
        }

        private IEnumerator TransparentCoroutine() {
            while (Screen.fullScreen) {
                Screen.fullScreen = false;
                //Wait one fram for fullScreen to change
                yield return null;
            }

            Window = Camera.main.gameObject.GetComponent<WindowController>() ?? Camera.main.gameObject.AddComponent<WindowController>();
            Window.TransparentMaterial = material;
            Window.blockClickThrough = !ClickThrough.Value;
            Window.isTopmost ^= true;
            Window.isTransparent ^= true;

            yield break;
        }
    }
}

