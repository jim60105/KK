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
using UnityEngine.SceneManagement;

namespace KK_TransparentBackground {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class KK_TransparentBackground : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Transparent Background";
        internal const string GUID = "com.jim60105.kk.transparentbackground";
        internal const string PLUGIN_VERSION = "20.05.21.0";
        internal const string PLUGIN_RELEASE_VERSION = "0.1.3";

        internal static new ManualLogSource Logger;
        public static ConfigEntry<KeyboardShortcut> Hotkey { get; set; }
        public static ConfigEntry<bool> ClickThrough { get; set; }
        public static ConfigEntry<float> AlphaOnUI { get; set; }

        private WindowController Window;
        private bool isOriginalFullScreen = false;
        private bool isTransparent = false; //為了做Window Destroy後的全螢幕處理
        private string SceneName ="";   //SceneUnloaded時比對用

        public void Awake() {
            Logger = base.Logger;
            Hotkey = Config.Bind<KeyboardShortcut>("Config", "Enable", new KeyboardShortcut(KeyCode.None));
            ClickThrough = Config.Bind<bool>("Config", "Click Through", true, "If you don’t want to click through to the back, set to False");
            AlphaOnUI = Config.Bind<float>("Config", "Alpha transparent on UI display", 0.8f, new ConfigDescription("0% = Transparent to 100% = Opaque", new AcceptableValueRange<float>(0, 1f)));
            TransparentUI.Alpha = AlphaOnUI.Value;

            ClickThrough.SettingChanged += delegate { if (null != Window) Window.blockClickThrough = !ClickThrough.Value; };
            AlphaOnUI.SettingChanged += delegate { TransparentUI.Alpha = AlphaOnUI.Value; };
        }

        public void Start() {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public void Update() {
            if (Hotkey.Value.IsDown()) {
                if (null == Camera.main) return;
                TransparentUI.BuildCamvasGroup();   //Force rebuild
                StartCoroutine(TransparentCoroutine());
            }
        }

        public void OnSceneUnloaded(Scene scene) {
            if (!isTransparent || scene.name != SceneName) return;

            if (isTransparent) {
                SceneName = "";
                if (null != Window) {
                    StartCoroutine(TransparentCoroutine());
                } else {
                    if (isOriginalFullScreen) {
                        SetFullScreen(true);
                        isOriginalFullScreen = false;
                    }
                }
                Logger.LogDebug($"Scene unload: {scene.name}");
            }
        }

        void OnGUI() {
            if (null == Window) return;
            float buttonWidth = 140f;
            float buttonHeight = 40f;
            float margin = 20f;
            if (
                GUI.Button(
                    new Rect(
                        Screen.width - buttonWidth - margin,
                        Screen.height - buttonHeight - margin,
                        buttonWidth,
                        buttonHeight),
                    "Toggle transparency"
                    )
                ) {
                if (null == Camera.main) return;
                StartCoroutine(TransparentCoroutine());
            }
        }

        private IEnumerator TransparentCoroutine() {
            //GetOrCreateWindowController
            if (null == Window) {
                Window = Camera.main.gameObject.GetComponent<WindowController>() ?? Camera.main.gameObject.AddComponent<WindowController>();
                Window.blockClickThrough = !ClickThrough.Value;
                Window.OnStateChanged -= onStateChange; //保險
                Window.OnStateChanged += onStateChange;

                SceneName = SceneManager.GetActiveScene().name;
                //Logger.LogDebug($"WindowController created");

                //不用delegate是為了能做保險性Unload
                void onStateChange() { TransparentUI.Enable = Window.isTransparent; }
            }

            //每次透明化前都保存CameraSetting
            if (!Window.isTransparent) Window.StoreOriginalCameraSetting();

            while (Screen.fullScreen) {
                isOriginalFullScreen = true;
                Screen.fullScreen = false;
                //Wait one fram for fullScreen to change
                yield return null;
            }

            Window.isTransparent ^= true;
            isTransparent = Window.isTransparent;
            Window.isTopmost = isTransparent;

            if (isOriginalFullScreen) {
                SetFullScreen(!isTransparent);
            }
            yield break;
        }

        void SetFullScreen(bool fullScreen) {
            Window.isMaximized = !fullScreen;
            Screen.fullScreen = fullScreen;
        }
    }
}

