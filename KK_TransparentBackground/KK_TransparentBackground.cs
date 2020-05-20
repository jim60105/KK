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
        internal const string PLUGIN_VERSION = "20.05.20.0";
        internal const string PLUGIN_RELEASE_VERSION = "0.1.0";

        internal static new ManualLogSource Logger;
        public static ConfigEntry<KeyboardShortcut> Hotkey { get; set; }
        public static ConfigEntry<bool> ClickThrough { get; set; }
        //public static ConfigEntry<float> TransparentOnCamera { get; set; }
        public static ConfigEntry<float> AlphaOnUI { get; set; }

        private WindowController Window;
        //private TransparentCameraController MainCameraTC;

        public void Awake() {
            Logger = base.Logger;
            Hotkey = Config.Bind<KeyboardShortcut>("Config", "Enable", new KeyboardShortcut(KeyCode.None));
            ClickThrough = Config.Bind<bool>("Config", "Click Through", true, "If you don’t want to click to the back, set to False");
            //TransparentOnCamera = Config.Bind<float>("Config", "Transparency on main camera", 0f, new ConfigDescription("0 = opaque. This is not the correct Transparent, but the effect of blending with a black background and Alpha.", new AcceptableValueRange<float>(0, 1f)));
            AlphaOnUI = Config.Bind<float>("Config", "Alpha transparent on UI display", 0.8f, new ConfigDescription("0% = Transparent to 100% = Opaque", new AcceptableValueRange<float>(0, 1f)));

            ClickThrough.SettingChanged += delegate { if (null != Window) Window.blockClickThrough = !ClickThrough.Value; };
            //TransparentOnCamera.SettingChanged += delegate { if (null != MainCameraTC) MainCameraTC.Transparency = TransparentOnCamera.Value; };
            AlphaOnUI.SettingChanged += delegate {  TransparentUI.Alpha = AlphaOnUI.Value; };
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

            //MainCameraTC = Camera.main.gameObject.GetComponent<TransparentCameraController>() ?? Camera.main.gameObject.AddComponent<TransparentCameraController>();
            //MainCameraTC.Transparency = TransparentOnCamera.Value;
            TransparentUI.BuildCamvasGroup();   //Force rebuild
            TransparentUI.Alpha = AlphaOnUI.Value;

            Window = Camera.main.gameObject.GetComponent<WindowController>();
            if (null == Window) {
                Window = Camera.main.gameObject.AddComponent<WindowController>();
                Window.OnStateChanged += delegate {
                    //if (null != MainCameraTC) MainCameraTC.Enable = Window.isTransparent;
                    TransparentUI.Enable = Window.isTransparent;
                };
                Window.blockClickThrough = !ClickThrough.Value;
            }

            Window.isTopmost ^= true;
            Window.isTransparent ^= true;

            yield break;
        }
    }
}

