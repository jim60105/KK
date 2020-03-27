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
using BepInEx.Harmony;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using Extension;
using HarmonyLib;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace KK_StudioCharaLightLinkedToCamera {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    public class KK_StudioCharaLightLinkedToCamera : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Chara Light Linked To Camera";
        internal const string GUID = "com.jim60105.kk.studiocharalightlinkedtocamera";
        internal const string PLUGIN_VERSION = "20.03.24.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.1.3";

        /// <summary>
        /// True: 將Quaternion換為俯仰角以填入UI才能儲存，但是此方法在垂直往上/往下看時光源會偏移。
        /// False: 以計算出的Quaternion值旋轉transRoot是為直接操作，這樣在任何視角都會是正確光源，但是無法重整UI儲存數值。
        /// </summary>
        public static ConfigEntry<bool> RefreshUI { get; private set; }

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            var harmony = HarmonyWrapper.PatchAll(typeof(Patches));

            {
                var toAlter = AccessTools.Inner(typeof(Studio.CameraLightCtrl), "LightCalc")
                    .GetMethod("OnValueChangeAxis", AccessTools.all);
                var postfix = typeof(Patches).GetMethod(nameof(Patches.OnValueChangeAxisPostfix), AccessTools.all);
                harmony.Patch(toAlter, null, new HarmonyMethod(postfix));
            }

            {
                var toAlter = AccessTools.Inner(typeof(Studio.CameraLightCtrl), "LightCalc")
                    .GetMethod("OnEndEditAxis", AccessTools.all);
                var postfix = typeof(Patches).GetMethod(nameof(Patches.OnEndEditAxisPostfix), AccessTools.all);
                harmony.Patch(toAlter, null, new HarmonyMethod(postfix));
            }

            {
                var toAlter = AccessTools.Inner(typeof(Studio.CameraLightCtrl), "LightCalc")
                    .GetMethod("OnClickAxis", AccessTools.all);
                var postfix = typeof(Patches).GetMethod(nameof(Patches.OnClickAxisPostfix), AccessTools.all);
                harmony.Patch(toAlter, null, new HarmonyMethod(postfix));
            }

            RefreshUI = Config.Bind<bool>("Config", "Refresh UI", true, "Due to KK's character lighting design, if the light is forced to lock accurately the UI cannot be refreshed and the setting will NOT be stored in vanilla SceneData. Use this feature at your own risk.");
        }
    }

    class Patches {
        private static readonly ManualLogSource Logger = KK_StudioCharaLightLinkedToCamera.Logger;
        public static bool locked = false;
        private static readonly object studioLightCalc = Singleton<Studio.Studio>.Instance.cameraLightCtrl.GetField("lightChara");
        //private static Transform transRoot;
        //private static Quaternion angleDiff = Quaternion.Euler(0, 0, 0);
        private static Studio.CameraControl cameraControl;

        #region View
        //static private Selectable[] interactableGroup;
        static internal GameObject LockBtn;

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraLightCtrl), "Init")]
        public static void InitPostfix() {
            if (null != GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Chara Light Lock Btn")) {
                return;
            }
            GameObject original = GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Button Axis X");
            GameObject parent = original.transform.parent.gameObject;

            //interactableGroup = new Selectable[] {
            //    GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Slider Axis X").GetComponent<Slider>(),
            //    GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/InputField Axis X").GetComponent<InputField>(),
            //    GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Button Axis X").GetComponent<Button>(),
            //    GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Slider Axis Y").GetComponent<Slider>(),
            //    GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/InputField Axis Y").GetComponent<InputField>(),
            //    GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Button Axis Y").GetComponent<Button>(),
            //};

            LockBtn = UnityEngine.Object.Instantiate(original, parent.transform);
            LockBtn.name = "Chara Light Lock Btn";
            LockBtn.transform.localPosition = new Vector3(157.5f, -59f, 0);
            LockBtn.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(157.5f, -82f), new Vector2(180.5f, -59));
            LockBtn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaLightLinkedToCamera.Resources.lock_open.png", 36, 36);
            LockBtn.GetComponent<Button>().onClick.RemoveAllListeners();
            LockBtn.GetComponent<Button>().interactable = true;

            LockBtn.GetComponent<Button>().onClick.AddListener(delegate { ToggleLocked(); });

            //transRoot = (studioLightCalc.GetField("transRoot") as Transform);
            //transRoot.localRotation = Quaternion.Euler(0, 0, 0);
            RegisterSaveEvent();
        }

        public static void ToggleLocked(bool? b = null) {
            cameraControl = Object.FindObjectOfType<Studio.CameraControl>();
            Logger.LogDebug($"Get CameraControl: {cameraControl.gameObject.name}");

            //angleDiff = Quaternion.Inverse(Quaternion.Euler(cameraControl.cameraAngle)) * (studioLightCalc.GetField("transRoot") as Transform).localRotation;

            if (null == b) {
                locked = !locked;
            } else {
                locked = (bool)b;
            }

            if (locked) {
                LockBtn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaLightLinkedToCamera.Resources.lock.png", 36, 36);
                
                var chaLight = Singleton<Studio.Studio>.Instance.sceneInfo.charaLight;
                var ligLocal = (studioLightCalc.GetField("light") as Light).transform.localEulerAngles;
                computeAngle = new ComputeAngle(
                    ligLocal,
                    new Vector2(chaLight.rot[0], chaLight.rot[1]),
                    cameraControl.cameraAngle
                );
            } else {
                LockBtn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaLightLinkedToCamera.Resources.lock_open.png", 36, 36);
            }

            //foreach (Selectable sel in interactableGroup) {
            //    sel.interactable = !locked;
            //}
            Logger.LogDebug("Locked status: " + locked);
        }
        #endregion

        #region SaveAndLoad
        public static void RegisterSaveEvent() {
            ExtendedSave.SceneBeingSaved += path => {
                ExtendedSave.SetSceneExtendedDataById(KK_StudioCharaLightLinkedToCamera.GUID, new PluginData() {
                    data = new System.Collections.Generic.Dictionary<string, object> {
                        { "locked", locked ? "true" : "false" },
                        { "attachedLightAngle", new System.Collections.Generic.Dictionary<string,float>{
                            {"x", computeAngle.AttachedLightEuler.x },
                            {"y", computeAngle.AttachedLightEuler.y }
                        }},
                        { "attachedCameraAngle", new System.Collections.Generic.Dictionary<string,float>{
                            {"x", computeAngle.AttachedCameraEuler.x },
                            {"y", computeAngle.AttachedCameraEuler.y }
                        }}
                    },
                    version = 3
                });
                Logger.LogDebug("Scene Saved");
            };
            ExtendedSave.SceneBeingLoaded += path => {
                PluginData pd = ExtendedSave.GetSceneExtendedDataById(KK_StudioCharaLightLinkedToCamera.GUID);
                if (null != pd && pd.version == 3 &&
                    pd.data.TryGetValue("locked", out object l) && l is string boolstring &&
                    pd.data.TryGetValue("attachedLightAngle", out object lig) && 
                    pd.data.TryGetValue("attachedCameraAngle", out object cam)) {

                    System.Collections.Generic.Dictionary<string, float> ligAngle = lig.ToDictionary<string, float>();
                    System.Collections.Generic.Dictionary<string, float> camAngle = cam.ToDictionary<string, float>();

                    ToggleLocked(boolstring == "true");
                    
                    if (computeAngle != null)
                    {
                        computeAngle.AttachedLightEuler = new Vector2(ligAngle["x"], ligAngle["y"]);
                        computeAngle.AttachedCameraEuler = new Vector2(camAngle["x"], camAngle["y"]);
                    }

                    Logger.LogDebug("Scene Load PluginData");
                } else {
                    ToggleLocked(false);
                }
            };
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.SceneLoadScene), "OnClickLoad")]
        public static void OnClickLoadPrefix() {
            //LoadScene以前先解除Locked狀態
            ToggleLocked(false);
        }

        #endregion

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), "CameraUpdate")]
        public static void CameraUpdatePostfix(Studio.CameraControl __instance) {
            if (!locked || __instance != cameraControl) return;
            //Quaternion q = Quaternion.Euler(__instance.cameraAngle) * angleDiff;

            ////這裡分成兩種方式操作
            //// True: 將Quaternion換為俯仰角以填入UI才能儲存，但是此方法在垂直往上/往下看時光源會偏移。
            //// False: 以計算出的Quaternion值旋轉transRoot是為直接操作，這樣在任何視角都會是正確光源，但是無法重整UI儲存數值。
            //if (KK_StudioCharaLightLinkedToCamera.RefreshUI.Value) {
            //    //finding pitch_roll_yaw from Quaternions - Unity Answers
            //    //https://answers.unity.com/questions/416169/finding-pitchrollyaw-from-quaternions.html

            //    float roll = Mathf.Atan2(2 * q.y * q.w - 2 * q.x * q.z, 1 - 2 * q.y * q.y - 2 * q.z * q.z) * Mathf.Rad2Deg;
            //    float pitch = Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z) * Mathf.Rad2Deg;
            //    //float yaw = Mathf.Asin(2 * x * y + 2 * z * w) * Mathf.Rad2Deg;

            //    roll = (roll >= 0) ? roll : roll + 360;
            //    pitch = (pitch >= 0) ? pitch : pitch + 360;

            //    studioLightCalc.Invoke("OnValueChangeAxis", new object[] { pitch, 0 });
            //    studioLightCalc.Invoke("OnValueChangeAxis", new object[] { roll, 1 });
            //    studioLightCalc.Invoke("UpdateUI");
            //    //Logger.LogDebug($" {pitch}, {roll}");
            //} else {
            //    transRoot.localRotation = q;
            //}
            ////Logger.LogDebug($"CameraAngle: {__instance.cameraAngle[0]}, {__instance.cameraAngle[1]} / LightAngle: {(Quaternion.Euler(Singleton<Studio.CameraControl>.Instance.cameraAngle) * angleDiff).eulerAngles.ToString()}");

            var vec = computeAngle.GetLightEuler(__instance.cameraAngle.x, __instance.cameraAngle.y);
            var chaLight = Singleton<Studio.Studio>.Instance.sceneInfo.charaLight;
            chaLight.rot[0] = vec.x;
            chaLight.rot[1] = vec.y;

            studioLightCalc.Invoke("UpdateUI");
            studioLightCalc.Invoke("Reflect");
        }
        
        private static ComputeAngle computeAngle;

        private static void ReflectAngle()
        {
            if (locked)
            {
                var chaLight = Singleton<Studio.Studio>.Instance.sceneInfo.charaLight;
                computeAngle.AttachedLightEuler = new Vector2(chaLight.rot[0], chaLight.rot[1]);
                var camAngle = Singleton<Studio.CameraControl>.Instance.cameraAngle;
                computeAngle.AttachedCameraEuler = new Vector2(camAngle.x, camAngle.y);
            }
        }

        public static void OnValueChangeAxisPostfix() => ReflectAngle();

        public static void OnEndEditAxisPostfix() => ReflectAngle();

        public static void OnClickAxisPostfix() => ReflectAngle();
    }
    
    public class ComputeAngle
    {
        private float alpha, beta, alphaR, betaR;
        private float cosr, agls;

        private int situ;
        private bool isInited;

        private Vector2 lightLocalEuler;

        private Vector2 preLigEuler;
        public Vector2 AttachedLightEuler
        {
            get => preLigEuler;
            set
            {
                preLigEuler = value;
                isInited = false;
            }
        }

        private Vector2 preCamEuler;
        public Vector2 AttachedCameraEuler
        {
            get => preCamEuler;
            set
            {
                preCamEuler = value;
                isInited = false;
            }
        }

        public ComputeAngle(Vector2 lightLocalEuler, Vector2 lightEuler, Vector2 cameraEuler)
        {
            this.lightLocalEuler = lightLocalEuler;
            preLigEuler = lightEuler;
            preCamEuler = cameraEuler;

            situ = Init();
        }

        private int Init()
        {
            isInited = true;

            var y = preLigEuler.y + lightLocalEuler.y - preCamEuler.y;
            alpha = (-180f <= y && y <= 180f) ? y : (y < 0 ? (y - 180f) % 360f + 180f : (y + 180f) % 360f - 180f);

            var x = lightLocalEuler.x - preLigEuler.x;
            beta = (-90f <= x && x <= 90f) ? x : (x < 0 ? (x - 90f) % 180f + 90f : (x + 90f) % 180f - 90f);

            if (alpha == 0f) return 1;
            if (alpha == 180f || alpha == -180f) return 2;
            if (beta == 90f || beta == -90f) return 3;

            alphaR = alpha * Mathf.Deg2Rad;
            betaR = beta * Mathf.Deg2Rad;

            var sc = Mathf.Sin(alphaR) * Mathf.Cos(betaR);
            cosr = Mathf.Sqrt(1f - Mathf.Pow(sc, 2f));
            if (alpha < -90f || alpha > 90f) cosr = -cosr;
            agls = cosr == 0f ? 0f : Mathf.Asin(Mathf.Sin(betaR) / cosr);

            return 0;
        }

        private Vector2 GetLightEulerOffset(float curCamEulerX, float curCamEulerY)
        {
            var x = curCamEulerX - preCamEuler.x;
            var y = curCamEulerY - preCamEuler.y;
            if (situ == 1) return new Vector2(x, y);
            if (situ == 2) return new Vector2(-x, y);
            if (situ == 3)
            {
                if (alpha <= 90f || alpha >= -90f) return new Vector2(x, y - alpha);
                if (alpha > 90f) return new Vector2(-x, y + 180f - alpha);
                if (alpha < -90f) return new Vector2(-x, y - 180f - alpha);
            }
            return Vector2.zero;
        }

        public Vector2 GetLightEuler(float curCamEulerX, float curCamEulerY)
        {
            if (!isInited) situ = Init();
            if (situ > 0)
            {
                var vec = GetLightEulerOffset(curCamEulerX, curCamEulerY);
                return GetFixedAngle(preLigEuler.x - vec.x, preLigEuler.y + vec.y);
            }

            var m = agls + (curCamEulerX - preCamEuler.x) * Mathf.Deg2Rad;
            var bx = Mathf.Asin(Mathf.Sin(m) * cosr);
            var cbx = Mathf.Cos(bx);
            var ay = cbx == 0f ? 0f : Mathf.Acos(Mathf.Cos(m) * cosr / cbx);
            if (alpha < 0f) ay = -ay;

            var x = bx * Mathf.Rad2Deg - beta;
            var y = curCamEulerY - preCamEuler.y + ay * Mathf.Rad2Deg - alpha;

            // todo: Given different local euler preset of light, returns the correct result.
            //       (The light has its own local rotation preset (40, 180, 0) while
            //         its direction follows the rotation of "transRoot".)
            return GetFixedAngle(preLigEuler.x - x, preLigEuler.y + y);
        }

        private Vector2 GetFixedAngle(float px, float py)
        {
            return new Vector2()
            {
                x = px < 0f ? px % 360f + 360f : px % 360f,
                y = py < 0f ? py % 360f + 360f : py % 360f
            };
        }
    }
}
