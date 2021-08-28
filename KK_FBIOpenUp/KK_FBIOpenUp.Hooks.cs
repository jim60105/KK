using ChaCustom;
using Extension;
using HarmonyLib;
using Studio;
using System;
using System.Linq;

namespace KK_FBIOpenUp {
    internal static class Hooks {
        #region 繪製圖標
        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitCharaList")]
        public static void InitCharaListPostfix(CharaList __instance) {
            bool flag = string.Equals(__instance.name, "00_Female") == (SampleChara.chaFile.parameter.sex == 1);
            if (KoikatuHelper.TryGetPluginInstance("com.jim60105.kk.studioallgirlsplugin")) { flag = true; }
            if (flag && null == UnityEngine.GameObject.Find($"StudioScene/Canvas Main Menu/01_Add/{__instance.name}/redBagBtn")) {
                KK_FBIOpenUp.nowGameMode = KK_FBIOpenUp.GameMode.Studio;
                UnityStuff.DrawRedBagBtn(KK_FBIOpenUp.GameMode.Studio, __instance);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CustomControl), "Start")]
        public static void StartPostfix_Maker() {
            UnityStuff.DrawRedBagBtn(KK_FBIOpenUp.GameMode.Maker);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ActionScene), "Start")]
        public static void StartPostfix_MainGame() {
            UnityStuff.DrawRedBagBtn(KK_FBIOpenUp.GameMode.MainGame);
        }

        internal static HSceneProc hSceneProc;
        [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "Start")]
        public static void StartPostfix_FreeH(HSceneProc __instance) {
            hSceneProc = __instance;
            UnityStuff.DrawRedBagBtn(KK_FBIOpenUp.GameMode.FreeH, 1);
            UnityStuff.DrawRedBagBtn(KK_FBIOpenUp.GameMode.FreeH, 2);
        }
        #endregion

        #region Patch換人
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
        public static void LoadFileLimitedPostfix(ChaFileControl __instance) {
            if (KK_FBIOpenUp._isenabled) {
                if (KK_FBIOpenUp.nowGameMode == KK_FBIOpenUp.GameMode.Studio) {
                    ChaControl chaCtrl = Singleton<Manager.Character>.Instance.dictEntryChara.Where((x) => x.Value.chaFile == __instance).Single().Value;
                    if (null != chaCtrl) Patches.ChangeChara(chaCtrl);
                } else {
                    Patches.ChangeChara(Singleton<CustomBase>.Instance.chaCtrl);
                }
            }
        }
        #endregion

        #region 暫時屏蔽運作，用在讀取人物清單之類的地方
        public static bool BlockChanging { get; set; } = false;

        [HarmonyPrefix, HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        public static void InitFemaleListPrefix() {
            SetInitFlag(true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        public static void InitFemaleListPostfix() {
            SetInitFlag(false);
        }

        //[HarmonyPrefix, HarmonyPatch(typeof(SceneLoadScene), "Awake")]
        //public static void AwakePrefix() {
        //    SetInitFlag(true);
        //}

        //[HarmonyPostfix, HarmonyPatch(typeof(SceneLoadScene), "OnClickClose")]
        //public static void OnClickClosePostfix() {
        //    SetInitFlag(false);
        //}

        public static void SetInitFlag(bool flag) {
            BlockChanging = flag;
            if (flag) {
                Patches.chaFileCustomDict.Clear();
            }
            KK_FBIOpenUp.Logger.LogDebug($"Set Init: {flag}");
        }
        #endregion
    }
}
