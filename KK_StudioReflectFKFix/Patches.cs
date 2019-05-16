using System.Reflection;

using BepInEx.Logging;

using Extension;

using Harmony;

using Studio;

using UILib;

using UnityEngine;
using UnityEngine.UI;

using Logger = BepInEx.Logger;

namespace KK_StudioReflectFKFix
{
    class Patches
    {
        internal static void InitPatch(HarmonyInstance harmony)
        {
            harmony.Patch(typeof(MPCharCtrl).GetMethod("Awake", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(AwakePostfix), null), null);
        }

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

        private static void AwakePostfix(MPCharCtrl __instance)
        {
            ((Button)__instance.GetPrivate("ikInfo").GetPrivate("buttonReflectFK")).onClick.RemoveAllListeners();
            ((Button)__instance.GetPrivate("ikInfo").GetPrivate("buttonReflectFK")).onClick.AddListener(delegate ()
            {
                //__instance.CopyBoneFK((OIBoneInfo.BoneGroup)353);
                typeof(MPCharCtrl).InvokeMember("CopyBoneFK", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, __instance, new object[] { (OIBoneInfo.BoneGroup)1 });
            });
            Logger.Log(LogLevel.Debug, "[KK_SRFF] IK->FK Function Rewrite Finish");
            InitBtn(__instance);
        }

        private static GameObject btn;
        private static void InitBtn(MPCharCtrl __instance)
        {
            var original = GameObject.Find("StudioScene/Canvas Main Menu/01_Add/00_Female/Button Change");
            var parent = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/03_Neck");
            btn = UnityEngine.Object.Instantiate(original, parent.transform);
            btn.name = "Copy FK Neck";
            btn.transform.localPosition = new Vector3(0, -95, 0);
            btn.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -116), new Vector2(190, -95));
            btn.GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioReflectFKFix.Resources.CopyFKNeck.png", 183, 20);
            btn.GetComponent<Button>().onClick.RemoveAllListeners();
            btn.GetComponent<Button>().onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
            btn.GetComponent<Button>().interactable = true;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                typeof(MPCharCtrl).InvokeMember("CopyBoneFK", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, __instance, new object[] { OIBoneInfo.BoneGroup.Neck });
            });

            Logger.Log(LogLevel.Debug, "[KK_SRFF] Draw Button Finish");
        }
    }
}
