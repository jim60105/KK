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

        private static GameObject[] btn = new GameObject[3];
        private static void InitBtn(MPCharCtrl __instance)
        {
            //Copy this btn
            var original = GameObject.Find("StudioScene/Canvas Main Menu/01_Add/00_Female/Button Change");
            //btn[0]: Neck
            var parent = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/03_Neck");
            btn[0] = UnityEngine.Object.Instantiate(original, parent.transform);
            //btn[1]: LeftHand, btn[2]: RightHand
            parent = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/06_Hand");
            btn[1] = UnityEngine.Object.Instantiate(original, parent.transform);
            btn[2] = UnityEngine.Object.Instantiate(original, parent.transform);

            btn[0].name = "Copy FK Neck";
            btn[1].name = "Copy FK Left Hand";
            btn[2].name = "Copy FK Right Hand";

            btn[0].transform.localPosition = new Vector3(0,-95, 0);
            btn[0].transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -116), new Vector2(190, -95));
            btn[0].GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioReflectFKFix.Resources.CopyFKNeck.png", 183, 20);

            btn[1].transform.localPosition = new Vector3(0, -95, 0);
            btn[1].transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -116), new Vector2(92.5f, -95));
            btn[1].GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioReflectFKFix.Resources.CopyFKLeftHand.png", 82, 20);

            btn[2].transform.localPosition = new Vector3(97.5f, -95, 0);
            btn[2].transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(97.5f, -116), new Vector2(190, -95));
            btn[2].GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioReflectFKFix.Resources.CopyFKRightHand.png", 82, 20);

            foreach (var b in btn)
            {
                b.GetComponent<Button>().onClick.RemoveAllListeners();
                b.GetComponent<Button>().onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
                b.GetComponent<Button>().interactable = true;
            }

            btn[0].GetComponent<Button>().onClick.AddListener(() =>
            {
                typeof(MPCharCtrl).InvokeMember("CopyBoneFK", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, __instance, new object[] { (OIBoneInfo.BoneGroup)256 });
            });

            btn[1].GetComponent<Button>().onClick.AddListener(() =>
            {
                typeof(MPCharCtrl).InvokeMember("CopyBoneFK", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, __instance, new object[] { (OIBoneInfo.BoneGroup)64 });
            });

            btn[2].GetComponent<Button>().onClick.AddListener(() =>
            {
                typeof(MPCharCtrl).InvokeMember("CopyBoneFK", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, __instance, new object[] { (OIBoneInfo.BoneGroup)32 });
            });
            Logger.Log(LogLevel.Debug, "[KK_SRFF] Draw Button Finish");
        }
    }
}
