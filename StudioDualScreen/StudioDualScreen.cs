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
using HarmonyLib;
using Manager;
using Studio;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace StudioDualScreen
{
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class StudioDualScreen : BaseUnityPlugin
    {
        internal const string PLUGIN_NAME = "Studio Dual Screen";
        internal const string GUID = "com.jim60105.kks.studiodualscreen";
        internal const string PLUGIN_VERSION = "21.10.04.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.2.0";

        public static ConfigEntry<KeyboardShortcut> Hotkey { get; set; }
        public static ConfigEntry<KeyboardShortcut> LockHotkey { get; set; }
        internal static new ManualLogSource Logger;

        public void Start()
        {
            Logger = base.Logger;
            Extension.Logger.logger = Logger;
            _ = Harmony.CreateAndPatchAll(typeof(Patches));

            Hotkey = Config.Bind("Hotkey", "Enable Key", new KeyboardShortcut(KeyCode.None), "You must have two monitors to make it work.");
            LockHotkey = Config.Bind("Hotkey", "Lock Key", new KeyboardShortcut(KeyCode.None), "Trigger this to lock/unlock the sub camera.");
        }

        public void Update() => Patches.Update();
    }

    class Patches
    {
        private static Camera mainCamera;
        private static Camera cloneCamera;
        private static GameObject cloneCanvas;
        private static float? backRateAddSpeed;
        private static bool isLocked { get => null != backRateAddSpeed; }

        public static void Update()
        {
            //監聽滑鼠按下
            if (StudioDualScreen.Hotkey.Value.IsDown())
            {
                if (null == mainCamera)
                {
                    mainCamera = GameObject.Find("StudioScene/Camera/Main Camera").GetComponent<Camera>();
                }
                Enable();
                StudioDualScreen.Logger.LogWarning("After enabling dual screens, F9 screenshots will cause no response, please use F11 instead");
                StudioDualScreen.Logger.LogMessage("Enable/Reload second screen.");
            }

            //鎖定Camera
            if (StudioDualScreen.LockHotkey.Value.IsDown())
            {
                SetLock(!isLocked);
                StudioDualScreen.Logger.LogMessage("Lock second screen camera: " + isLocked);
            }

            if (null != cloneCamera && null != mainCamera && mainCamera.transform.hasChanged && !isLocked)
            {
                mainCamera.transform.hasChanged = false;

                cloneCamera.GetComponent<Studio.CameraControl>().Import(mainCamera.GetComponent<Studio.CameraControl>().Export());
            }
        }

        /// <summary>
        /// 鎖定視角
        /// </summary>
        /// <param name="lock">鎖定/解鎖視角</param>
        private static void SetLock(bool @lock)
        {
            Studio.CameraControl camCtrl = cloneCamera.GetComponent<Studio.CameraControl>();
            if (@lock)
            {
                backRateAddSpeed = (float)camCtrl.GetField("rateAddSpeed");
                _ = camCtrl.SetField("rateAddSpeed", 0);
            }
            else
            {
                _ = camCtrl.SetField("rateAddSpeed", backRateAddSpeed);
                backRateAddSpeed = null;
            }
            StudioDualScreen.Logger.LogDebug("Lock second screen camera: " + isLocked);
        }

        public static void Enable()
        {
            if (Display.displays.Length > 1)
            {
                if (isLocked)
                {
                    SetLock(false);
                }

                //Clean CloneCamera
                if (null != cloneCamera)
                {
                    Object.Destroy(cloneCamera.gameObject);
                    cloneCamera = null;
                }

                //Create CloneCamera
                cloneCamera = Object.Instantiate(mainCamera);
                Object.Destroy(cloneCamera.GetComponent(typeof(Rigidbody)));
                Studio.CameraControl camCtrl = cloneCamera.GetComponent<Studio.CameraControl>();
                camCtrl.ReflectOption();
                camCtrl.isOutsideTargetTex = false;
                camCtrl.subCamera.gameObject.SetActive(false);
                _ = camCtrl.SetField("isInit", false);

                cloneCamera.name = "Main Camera(Clone)";
                cloneCamera.CopyFrom(mainCamera);
                cloneCamera.transform.SetParent(mainCamera.transform.parent.transform);
                cloneCamera.targetDisplay = 1;
                _ = camCtrl.GetField("cameraData")
                           .SetField("rotate", mainCamera.GetComponent<Studio.CameraControl>()
                                                         .GetField("cameraData")
                                                         .GetField("rotate"));

                //Hide Specter guide object, it binds on the camera.
                Transform SpecterMove = cloneCamera.GetComponentsInChildren<Transform>().Where(x => x.gameObject.layer == 5).FirstOrDefault();
                if (null != SpecterMove)
                {
                    SpecterMove.gameObject.SetActive(false);
                }

                //Create Frame
                if (null == cloneCanvas)
                {
                    Camera cameraUI = GameObject.Find("StudioScene/Camera/Camera UI").GetComponent<Camera>();
                    Camera cloneCameraUI = Object.Instantiate(cameraUI);
                    cloneCameraUI.name = "Camera UI Clone";
                    cloneCameraUI.CopyFrom(cameraUI);
                    cloneCameraUI.targetDisplay = 1;
                    cloneCameraUI.transform.SetParent(cameraUI.transform.parent.transform);

                    GameObject canvas = GameObject.Find("StudioScene/Canvas Frame Cap");
                    cloneCanvas = Object.Instantiate(canvas);
                    cloneCanvas.GetComponent<Canvas>().worldCamera = cloneCameraUI;
                    _ = cloneCanvas.GetComponent<FrameCtrl>().SetField("cameraUI", cloneCameraUI);
                    cloneCanvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(Display.displays[1].systemWidth, Display.displays[1].systemHeight);
                    cloneCanvas.transform.SetParent(canvas.transform.parent.transform);
                }

                //Set Neck Look & Eye Look
                Character.GetCharaList(0).Concat(Character.GetCharaList(1)).ToList().ForEach((ChaControl chaCtrl) =>
                {
                    chaCtrl.neckLookCtrl.target = cloneCamera.transform;
                    if (chaCtrl.fileStatus.eyesLookPtn == 1)
                    {
                        chaCtrl.eyeLookCtrl.target = cloneCamera.transform;
                    }
                });

                //Reset VMD
                try
                {
                    string path = KoikatuHelper.TryGetPluginInstance("KKVMDPlayPlugin.KKVMDPlayPlugin")?.Info.Location;
                    Assembly ass = Assembly.LoadFrom(path);
                    System.Type VMDCamMgrType = ass.GetType("KKVMDPlayPlugin.VMDCameraMgr");
                    if (null != VMDCamMgrType)
                    {
                        object VMDCamMgr = VMDCamMgrType.GetFieldStatic("_instance");
                        _ = (VMDCamMgr?.SetField("cameraControl", cloneCamera.GetComponent<Studio.CameraControl>()));
                    }
                    else
                    {
                        throw new System.Exception("Load assembly FAILED: VMDPlayPlugin");
                    }
                    //KK_StudioDualScreen.Logger.LogDebug("Reset VMD");
                }
                catch (System.Exception)
                {
                    StudioDualScreen.Logger.LogDebug("No KKVMDPlayPlugin found.");
                }

                //Active Display
                if (!Display.displays[1].active)
                {
                    Screen.SetResolution(Display.displays[0].renderingWidth, Display.displays[0].renderingHeight, Screen.fullScreen);
                    //Display.displays[0].Activate();
                    Display.displays[1].SetRenderingResolution(Display.displays[1].renderingWidth, Display.displays[1].renderingHeight);
                    Display.displays[1].Activate();
                }
            }
        }

        //Renable Display on Scene Load
        private static bool isLoading = false;
        private static SceneLoadScene sceneLoadScene;

        [HarmonyPrefix, HarmonyPatch(typeof(SceneLoadScene), "OnClickLoad")]
        public static void OnClickLoadPrefix(SceneLoadScene __instance)
        {
            isLoading = true;
            sceneLoadScene = __instance;
        }
        [HarmonyPostfix, HarmonyPatch(typeof(Scene), "LoadStart")]
        public static void LoadStartPostfix(Scene.Data data)
        {
            if (isLoading && data.levelName == "StudioNotification")
            {
                _ = sceneLoadScene.StartCoroutine(EnableCamera());
                isLoading = false;
            }
        }
        private static IEnumerator EnableCamera()
        {
            // 原本的彈窗動畫是1秒，多加0.5秒以確保它回到LoadScene
            yield return new WaitForSeconds(1.5f);
            if (null != cloneCamera)
            {
                SetLock(false);
                Enable();
            }
        }

        //Frame change hook
        [HarmonyPostfix, HarmonyPatch(typeof(FrameList), "OnClickSelect")]
        public static void OnClickSelectPostfix(FrameList __instance, int _idx)
        {
            if (null != cloneCanvas)
            {
                _ = cloneCanvas.GetComponent<FrameCtrl>().Load(((int)__instance.GetField("select") == -1) ? string.Empty : __instance.GetField("listPath").ToList<string>()[_idx]);
            }
        }
    }
}
