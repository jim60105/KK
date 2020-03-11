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
        internal const string PLUGIN_VERSION = "20.03.11.0";
        internal const string PLUGIN_RELEASE_VERSION = "0.0.0";

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            HarmonyWrapper.PatchAll(typeof(Patches));
        }
    }

    class Patches {
        private static bool locked = false;
        private static readonly float[] angleDiff = new float[] { 0, 0 };
        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), "CameraUpdate")]
        public static void CameraUpdatePostfix(Studio.CameraControl __instance) {
            if (!locked) return;
            object studioLightCalc = Singleton<Studio.Studio>.Instance.cameraLightCtrl.GetField("lightChara");
            float x = (__instance.cameraAngle.x + angleDiff[0]) % 360;
            float y = (__instance.cameraAngle.y + angleDiff[1]) % 360;
            x = x > 0 ? x : x + 360;
            y = y > 0 ? y : y + 360;
            studioLightCalc.Invoke("OnValueChangeAxis", new object[] { x, 0 });
            studioLightCalc.Invoke("OnValueChangeAxis", new object[] { y, 1 });
            studioLightCalc.Invoke("UpdateUI");

            //KK_StudioCharaLightLinkedToCamera.Logger.LogDebug($"CameraAngle: {__instance.cameraAngle[0]},{__instance.cameraAngle[1]},{__instance.cameraAngle[2]}");
            var charaLight = Singleton<Studio.Studio>.Instance.sceneInfo.charaLight;
            //KK_StudioCharaLightLinkedToCamera.Logger.LogDebug($"LightAngle: {charaLight.rot[0]},{charaLight.rot[1]}");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraLightCtrl), "Init")]
        public static void InitPostfix(Studio.CameraLightCtrl __instance) {
            if (null != GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Chara Light Lock Btn")) {
                return;
            }
            var original = GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Button Axis X");
            var parent = original.transform.parent.gameObject;
            GameObject btn = UnityEngine.Object.Instantiate(original, parent.transform);
            btn.name = "Chara Light Lock Btn";
            btn.transform.localPosition = new Vector3(157.5f, -59f, 0);
            btn.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(157.5f, -82f), new Vector2(180.5f, -59));
            btn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaLightLinkedToCamera.Resources.lock_open.png", 36, 36);
            btn.GetComponent<Button>().onClick.RemoveAllListeners();
            //btn.GetComponent<Button>().onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
            btn.GetComponent<Button>().interactable = true;

            btn.GetComponent<Button>().onClick.AddListener(() => {
                var charaLight = Singleton<Studio.Studio>.Instance.sceneInfo.charaLight;
                angleDiff[0] = charaLight.rot[0] - Singleton<Studio.CameraControl>.Instance.cameraAngle.x;
                angleDiff[1] = charaLight.rot[1] - Singleton<Studio.CameraControl>.Instance.cameraAngle.y;

                locked = !locked;
                if (locked) {
                    btn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaLightLinkedToCamera.Resources.lock.png", 36, 36);
                } else {
                    btn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaLightLinkedToCamera.Resources.lock_open.png", 36, 36);
                }
                GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Slider Axis X").GetComponent<Slider>().interactable = !locked;
                GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/InputField Axis X").GetComponent<InputField>().interactable = !locked;
                GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Button Axis X").GetComponent<Button>().interactable = !locked;
                GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Slider Axis Y").GetComponent<Slider>().interactable = !locked;
                GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/InputField Axis Y").GetComponent<InputField>().interactable = !locked;
                GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Button Axis Y").GetComponent<Button>().interactable = !locked;
            });

            //KK_StudioCharaLightLinkedToCamera.Logger.LogWarning("Draw Button Finish");
        }
    }
}
