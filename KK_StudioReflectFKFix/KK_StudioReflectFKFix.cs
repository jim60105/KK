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
using BepInEx.Logging;
using Extension;
using HarmonyLib;
using Studio;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace KK_StudioReflectFKFix {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioReflectFKFix : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Reflect FK Fix";
        internal const string GUID = "com.jim60105.kk.studioreflectfkfix";
        internal const string PLUGIN_VERSION = "20.08.05.0";
		internal const string PLUGIN_RELEASE_VERSION = "1.0.4";

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            Extension.Logger.logger = Logger;
            UIUtility.Init();
            Harmony.CreateAndPatchAll(typeof(Patches));
        }
    }

    class Patches {
        //public enum BoneGroup
        //{
        //    Body = 1,
        //    RightLeg,
        //    LeftLeg = 4,
        //    RightArm = 8,
        //    LeftArm = 16,
        //    RightHand = 32,
        //    LeftHand = 64,
        //    Hair = 128,
        //    Neck = 256,
        //    Breast = 512,
        //    Skirt = 1024
        //}

        [HarmonyPostfix, HarmonyPatch(typeof(MPCharCtrl), "Awake")]
        public static void AwakePostfix(MPCharCtrl __instance) {
            ((Button)__instance.GetField("ikInfo").GetField("buttonReflectFK")).onClick.RemoveAllListeners();
            ((Button)__instance.GetField("ikInfo").GetField("buttonReflectFK")).onClick.AddListener(delegate () {
                //__instance.CopyBoneFK((OIBoneInfo.BoneGroup)353);
                __instance.Invoke("CopyBoneFK", new object[] { OIBoneInfo.BoneGroup.Body });
            });
            ((Button[])__instance.GetField("fkInfo").GetField("buttonAnimeSingle"))[1].onClick.RemoveAllListeners();
            ((Button[])__instance.GetField("fkInfo").GetField("buttonAnimeSingle"))[1].onClick.AddListener(delegate () {
                __instance.Invoke("CopyBoneFK", new object[] { OIBoneInfo.BoneGroup.Neck });
            });
            //KK_StudioReflectFKFix.Logger.LogDebug("FK Fix Finish");
            InitBtn(__instance);
        }

        private static GameObject btn;
        private static void InitBtn(MPCharCtrl __instance) {
            var original = GameObject.Find("StudioScene/Canvas Main Menu/01_Add/00_Female/Button Change");
            var parent = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/03_Neck");
            btn = UnityEngine.Object.Instantiate(original, parent.transform);
            btn.name = "Copy FK Neck";
            btn.transform.localPosition = new Vector3(0, -95, 0);
            btn.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -117), new Vector2(191, -95));
            btn.GetComponent<Button>().onClick.RemoveAllListeners();
            btn.GetComponent<Button>().onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
            btn.GetComponent<Image>().sprite = ImageHelper.LoadNewSprite("KK_StudioReflectFKFix.Resources.CopyFKNeck.png", 191, 22);
            btn.GetComponent<Button>().interactable = true;

            btn.GetComponent<Button>().onClick.AddListener(() => {
                __instance.Invoke("CopyBoneFK", new object[] { OIBoneInfo.BoneGroup.Neck });
            });

            KK_StudioReflectFKFix.Logger.LogDebug("Draw Button Finish");
        }
    }
}
