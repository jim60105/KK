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
using BepInEx.Harmony;
using BepInEx.Logging;
using Extension;
using HarmonyLib;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace KK_StudioCharaLightLinkedToCamera {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioCharaLightLinkedToCamera : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Chara Light Linked To Camera";
        internal const string GUID = "com.jim60105.kk.studiocharalightlinkedtocamera";
        internal const string PLUGIN_VERSION = "20.03.11.2";
        internal const string PLUGIN_RELEASE_VERSION = "1.0.0";

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            HarmonyWrapper.PatchAll(typeof(Patches));
        }
    }

    class Patches {
        private static bool locked = false;
        private static readonly float[] angleDiff = new float[] { 0, 0 };
        private static Studio.CameraLightCtrl.LightInfo charaLight = Singleton<Studio.Studio>.Instance.sceneInfo.charaLight;
        private static readonly object studioLightCalc = Singleton<Studio.Studio>.Instance.cameraLightCtrl.GetField("lightChara");

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), "CameraUpdate")]
        public static void CameraUpdatePostfix(Studio.CameraControl __instance) {
            if (!locked) return;
            float x = (__instance.cameraAngle.x + angleDiff[0]) % 360;
            float y = (__instance.cameraAngle.y + angleDiff[1]) % 360;
            x = (x >= 0) ? x : x + 360;
            y = (y >= 0) ? y : y + 360;
            //KK_StudioCharaLightLinkedToCamera.Logger.LogDebug($"x: {x}, y: {y}");
            studioLightCalc.Invoke("OnValueChangeAxis", new object[] { x, 0 });
            studioLightCalc.Invoke("OnValueChangeAxis", new object[] { y, 1 });
            studioLightCalc.Invoke("UpdateUI");

            //KK_StudioCharaLightLinkedToCamera.Logger.LogDebug($"CameraAngle: {__instance.cameraAngle[0]}, {__instance.cameraAngle[1]} / LightAngle: {charaLight.rot[0]}, {charaLight.rot[1]}");
        }

        static private Selectable[] interactableGroup;
        static private GameObject LockBtn;
        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraLightCtrl), "Init")]
        public static void InitPostfix(Studio.CameraLightCtrl __instance) {
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

            LockBtn.GetComponent<Button>().onClick.AddListener(() => {
                charaLight = Singleton<Studio.Studio>.Instance.sceneInfo.charaLight;
                angleDiff[0] = charaLight.rot[0] - Singleton<Studio.CameraControl>.Instance.cameraAngle.x;
                angleDiff[1] = charaLight.rot[1] - Singleton<Studio.CameraControl>.Instance.cameraAngle.y;

                locked = !locked;
                if (locked) {
                    LockBtn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaLightLinkedToCamera.Resources.lock.png", 36, 36);
                } else {
                    LockBtn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaLightLinkedToCamera.Resources.lock_open.png", 36, 36);
                }

                foreach (Selectable sel in interactableGroup) {
                    sel.interactable = !locked;
                }
            });

            //KK_StudioCharaLightLinkedToCamera.Logger.LogWarning("Draw Button Finish");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.SceneLoadScene), "OnClickLoad")]
        public static void OnClickLoadPrefix() {
            if (locked) {
                LockBtn.GetComponentInChildren<Button>().onClick.Invoke();
            }
        }
    }
}
