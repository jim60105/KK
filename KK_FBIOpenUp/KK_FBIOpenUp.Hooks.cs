using ChaCustom;
using Extension;
using HarmonyLib;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KK_FBIOpenUp {
    internal static class Hooks {
        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitCharaList")]
        public static void InitCharaListPostfix(CharaList __instance) {
            //屏蔽男的，未來再看跨性別運作(?
            bool flag = string.Equals(__instance.name, "00_Female");
            if (SampleChara.chaFile.parameter.sex != 1) { flag = !flag; }
            if (flag) {
                KK_FBIOpenUp.nowGameMode = KK_FBIOpenUp.GameMode.Studio;
                UnityStuff.DrawRedBagBtn(KK_FBIOpenUp.GameMode.Studio, __instance);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CustomControl), "Start")]
        public static void StartPostfix_Maker() {
            KK_FBIOpenUp._isenabled = false;
            UnityStuff.DrawRedBagBtn(KK_FBIOpenUp.GameMode.Maker);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ActionScene), "Start")]
        public static void StartPostfix_MainGame() {
            KK_FBIOpenUp._isenabled = false;
            UnityStuff.DrawRedBagBtn(KK_FBIOpenUp.GameMode.MainGame);
        }

        internal static HSceneProc hSceneProc;
        [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "Start")]
        public static void StartPostfix_FreeH(HSceneProc __instance) {
            KK_FBIOpenUp._isenabled = false;
            hSceneProc = __instance;
            UnityStuff.DrawRedBagBtn(KK_FBIOpenUp.GameMode.FreeH);
        }


        [HarmonyPostfix, HarmonyPatch(typeof(AddObjectFemale), "Add", new Type[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
        public static void AddPostfix(ChaControl _female, OICharInfo _info) {
            if (KK_FBIOpenUp._isenabled) {
                Patches.ChangeChara(_female);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(OCIChar), "ChangeChara")]
        public static void ChangeCharaPostfix(OCIChar __instance) {
            if (KK_FBIOpenUp._isenabled) {
                Patches.ChangeChara(__instance.charInfo);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaFileControl), "LoadFileLimited", new Type[] { typeof(string), typeof(byte), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
        public static void LoadFileLimitedPostfix() {
            if (KK_FBIOpenUp._isenabled) {
                Patches.ChangeChara(Singleton<CustomBase>.Instance.chaCtrl);
            }
        }

        public static bool BlockChanging { get; set; } = false;

        [HarmonyPrefix, HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        public static void InitFemaleListPrefix() {
            SetInitFlag(true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        public static void InitFemaleListPostfix() {
            SetInitFlag(false);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SceneLoadScene), "Awake")]
        public static void AwakePrefix() {
            SetInitFlag(true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(SceneLoadScene), "OnClickClose")]
        public static void OnClickClosePostfix() {
            SetInitFlag(false);
        }

        public static void SetInitFlag(bool flag) {
            BlockChanging = flag;
            if (flag) {
                Patches.chaFileCustomDict.Clear();
            }
            KK_FBIOpenUp.Logger.LogDebug($"Set Init: {flag}");
        }
    }
}
