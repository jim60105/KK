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
        internal const string PLUGIN_VERSION = "20.03.18.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.1.2";

        /// <summary>
        /// True: 將Quaternion換為俯仰角以填入UI才能儲存，但是此方法在垂直往上/往下看時光源會偏移。
        /// False: 以計算出的Quaternion值旋轉transRoot是為直接操作，這樣在任何視角都會是正確光源，但是無法重整UI儲存數值。
        /// </summary>
        public static ConfigEntry<bool> RefreshUI { get; private set; }

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            HarmonyWrapper.PatchAll(typeof(Patches));

            RefreshUI = Config.Bind<bool>("Config", "Refresh UI", true, "Due to KK's character lighting design, if the light is forced to lock accurately the UI cannot be refreshed and the setting will NOT be stored in vanilla SceneData. Use this feature at your own risk.");
        }
    }

    class Patches {
        private static readonly ManualLogSource Logger = KK_StudioCharaLightLinkedToCamera.Logger;
        public static bool locked = false;
        private static readonly object studioLightCalc = Singleton<Studio.Studio>.Instance.cameraLightCtrl.GetField("lightChara");
        private static Transform transRoot;
        private static Quaternion angleDiff = Quaternion.Euler(0, 0, 0);

        #region View
        static private Selectable[] interactableGroup;
        static internal GameObject LockBtn;

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraLightCtrl), "Init")]
        public static void InitPostfix() {
            if (null != GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Chara Light Lock Btn")) {
                return;
            }
            GameObject original = GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Button Axis X");
            GameObject parent = original.transform.parent.gameObject;

            interactableGroup = new Selectable[] {
                GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Slider Axis X").GetComponent<Slider>(),
                GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/InputField Axis X").GetComponent<InputField>(),
                GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Button Axis X").GetComponent<Button>(),
                GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Slider Axis Y").GetComponent<Slider>(),
                GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/InputField Axis Y").GetComponent<InputField>(),
                GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Button Axis Y").GetComponent<Button>(),
            };

            LockBtn = UnityEngine.Object.Instantiate(original, parent.transform);
            LockBtn.name = "Chara Light Lock Btn";
            LockBtn.transform.localPosition = new Vector3(157.5f, -59f, 0);
            LockBtn.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(157.5f, -82f), new Vector2(180.5f, -59));
            LockBtn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaLightLinkedToCamera.Resources.lock_open.png", 36, 36);
            LockBtn.GetComponent<Button>().onClick.RemoveAllListeners();
            LockBtn.GetComponent<Button>().interactable = true;

            LockBtn.GetComponent<Button>().onClick.AddListener(delegate { ToggleLocked(); });

            transRoot = (studioLightCalc.GetField("transRoot") as Transform);
            //transRoot.localRotation = Quaternion.Euler(0, 0, 0);
            RegisterSaveEvent();
        }

        public static void ToggleLocked(bool? b = null) {
            angleDiff = Quaternion.Inverse(Quaternion.Euler(Singleton<Studio.CameraControl>.Instance.cameraAngle)) * (studioLightCalc.GetField("transRoot") as Transform).localRotation;

            if (null == b) {
                locked = !locked;
            } else {
                locked = (bool)b;
            }

            if (locked) {
                LockBtn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaLightLinkedToCamera.Resources.lock.png", 36, 36);
            } else {
                LockBtn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaLightLinkedToCamera.Resources.lock_open.png", 36, 36);
            }

            foreach (Selectable sel in interactableGroup) {
                sel.interactable = !locked;
            }
            Logger.LogDebug("Locked status: " + locked);
        }
        #endregion

        #region SaveAndLoad
        public static void RegisterSaveEvent() {
            ExtendedSave.SceneBeingSaved += path => {
                ExtendedSave.SetSceneExtendedDataById(KK_StudioCharaLightLinkedToCamera.GUID, new PluginData() {
                    data = new System.Collections.Generic.Dictionary<string, object> {
                        { "locked", locked ? "true" : "false" },
                        { "angleDiff", new System.Collections.Generic.Dictionary<string,float>{
                            {"x",angleDiff.x },
                            {"y",angleDiff.y },
                            {"z",angleDiff.z },
                            {"w",angleDiff.w }
                        }}
                    },
                    version = 2
                });
                Logger.LogDebug("Scene Saved");
            };
            ExtendedSave.SceneBeingLoaded += path => {
                PluginData pd = ExtendedSave.GetSceneExtendedDataById(KK_StudioCharaLightLinkedToCamera.GUID);
                if (null != pd && pd.version == 2 &&
                    pd.data.TryGetValue("locked", out object l) && l is string boolstring &&
                    pd.data.TryGetValue("angleDiff", out object obj)) {

                    System.Collections.Generic.Dictionary<string, float> d = obj.ToDictionary<string, float>();
                    ToggleLocked(boolstring == "true");
                    angleDiff = new Quaternion(d["x"], d["y"], d["z"], d["w"]);
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
        public static void CameraUpdatePostfix() {
            if (!locked) return;
            Quaternion q = Quaternion.Euler(Singleton<Studio.CameraControl>.Instance.cameraAngle) * angleDiff;

            //這裡分成兩種方式操作
            // True: 將Quaternion換為俯仰角以填入UI才能儲存，但是此方法在垂直往上/往下看時光源會偏移。
            // False: 以計算出的Quaternion值旋轉transRoot是為直接操作，這樣在任何視角都會是正確光源，但是無法重整UI儲存數值。
            if (KK_StudioCharaLightLinkedToCamera.RefreshUI.Value) {
                //finding pitch_roll_yaw from Quaternions - Unity Answers
                //https://answers.unity.com/questions/416169/finding-pitchrollyaw-from-quaternions.html

                float roll = Mathf.Atan2(2 * q.y * q.w - 2 * q.x * q.z, 1 - 2 * q.y * q.y - 2 * q.z * q.z) * Mathf.Rad2Deg;
                float pitch = Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z) * Mathf.Rad2Deg;
                //float yaw = Mathf.Asin(2 * x * y + 2 * z * w) * Mathf.Rad2Deg;

                roll = (roll >= 0) ? roll : roll + 360;
                pitch = (pitch >= 0) ? pitch : pitch + 360;

                studioLightCalc.Invoke("OnValueChangeAxis", new object[] { pitch, 0 });
                studioLightCalc.Invoke("OnValueChangeAxis", new object[] { roll, 1 });
                studioLightCalc.Invoke("UpdateUI");
                //Logger.LogDebug($" {pitch}, {roll}");
            } else {
                transRoot.localRotation = q;
            }
            //Logger.LogDebug($"CameraAngle: {__instance.cameraAngle[0]}, {__instance.cameraAngle[1]} / LightAngle: {(Quaternion.Euler(Singleton<Studio.CameraControl>.Instance.cameraAngle) * angleDiff).eulerAngles.ToString()}");
        }
    }
}
