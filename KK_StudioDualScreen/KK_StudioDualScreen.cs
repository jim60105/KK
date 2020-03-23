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
using Extension;
using UnityEngine;

namespace KK_StudioDualScreen {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioDualScreen : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Dual Screen";
        internal const string GUID = "com.jim60105.kk.studiodualscreen";
        internal const string PLUGIN_VERSION = "20.03.24.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.0.0";

        public static ConfigEntry<KeyboardShortcut> Hotkey { get; set; }
        internal static new ManualLogSource Logger;
        private static GameObject mainCamera;
        private static Camera cloneCamera;

        public void Start() {
            Logger = base.Logger;
            Hotkey = Config.Bind<KeyboardShortcut>("Hotkey", "Active Key", new KeyboardShortcut(KeyCode.None), "You must have two monitors to make it work.");
        }

        public void Update() {
            //監聽滑鼠按下
            if (KK_StudioDualScreen.Hotkey.Value.IsDown()) {
                if (null == mainCamera) {
                    mainCamera = GameObject.Find("StudioScene/Camera/Main Camera");
                }

                if (null != cloneCamera) {
                    cloneCamera.GetComponent<Studio.CameraControl>().Import(mainCamera.GetComponentInChildren<Camera>().GetComponent<Studio.CameraControl>().Export());
                } else if (Display.displays.Length > 1) {
                    cloneCamera = UnityEngine.Object.Instantiate(mainCamera.GetComponentInChildren<Camera>());
                    cloneCamera.GetComponent<Studio.CameraControl>().ReflectOption();
                    cloneCamera.GetComponent<Studio.CameraControl>().isOutsideTargetTex = false;
                    cloneCamera.GetComponent<Studio.CameraControl>().SetField("isInit", false);

                    cloneCamera.name = "Camera Clone";
                    cloneCamera.CopyFrom(mainCamera.GetComponent<Camera>());
                    cloneCamera.targetDisplay = 1;
                    cloneCamera.transform.SetParent(mainCamera.transform.parent.transform);
                    Display.displays[1].SetRenderingResolution(Display.displays[1].renderingWidth, Display.displays[1].renderingHeight);
                    Display.displays[1].Activate();
                    Display.displays[0].SetRenderingResolution(Display.displays[0].renderingWidth, Display.displays[0].renderingHeight);
                    Display.displays[0].Activate();
                }
            }
        }
    }
}
