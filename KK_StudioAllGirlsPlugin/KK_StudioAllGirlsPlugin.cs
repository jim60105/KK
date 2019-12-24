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
using Extension;
using HarmonyLib;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UniRx;
using UnityEngine.UI;

namespace KK_StudioAllGirlsPlugin {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioAllGirlsPlugin : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio All Girls Plugin";
        internal const string GUID = "com.jim60105.kk.studioallgirlsplugin";
        internal const string PLUGIN_VERSION = "19.11.02.0";
		internal const string PLUGIN_RELEASE_VERSION = "1.3.2";

        public static ConfigEntry<bool> Enable { get; private set; }

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            Enable = Config.AddSetting("Config", "Enable", true, "Load all boys as girls.\n(Restart the game to make this effect)");
            if (Enable.Value) {
                HarmonyWrapper.PatchAll(typeof(Patches));
            }
        }

    }

    class Patches {
        //PauseCtrl(PoseCtrl), 取消姿勢讀取的性別檢核
        [HarmonyTranspiler, HarmonyPatch(typeof(PauseCtrl), "CheckIdentifyingCode")]
        public static IEnumerable<CodeInstruction> CheckIdentifyingCodeTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Beq) //Line 23 
                {
                    i++; //Line 24 
                    if (codes[i].opcode == OpCodes.Ldc_I4_0) //Double Check
                    {
                        codes.RemoveRange(i, 3);
                        break;
                    }
                }
            }
            return codes.AsEnumerable();
        }

        //PauseCtrl(PoseCtrl), 修正姿勢讀取的FK bone not found Error
        [HarmonyPrefix, HarmonyPatch(typeof(PauseCtrl.FileInfo), "Apply")]
        public static bool ApplyPrefix(OCIChar _char, PauseCtrl.FileInfo __instance) {
            _char.LoadAnime(__instance.group, __instance.category, __instance.no, __instance.normalizedTime);
            for (int i = 0; i < __instance.activeIK.Length; i++) {
                _char.ActiveIK((OIBoneInfo.BoneGroup)(1 << i), __instance.activeIK[i], false);
            }
            _char.ActiveKinematicMode(OICharInfo.KinematicMode.IK, __instance.enableIK, true);
            foreach (KeyValuePair<int, ChangeAmount> keyValuePair in __instance.dicIK) {
                _char.oiCharInfo.ikTarget[keyValuePair.Key].changeAmount.Copy(keyValuePair.Value, true, true, true);
            }
            for (int j = 0; j < __instance.activeFK.Length; j++) {
                _char.ActiveFK(FKCtrl.parts[j], __instance.activeFK[j], false);
            }
            _char.ActiveKinematicMode(OICharInfo.KinematicMode.FK, __instance.enableFK, true);
            foreach (KeyValuePair<int, ChangeAmount> keyValuePair2 in __instance.dicFK) {
                _char.oiCharInfo.bones.TryGetValue(keyValuePair2.Key, out var value);
                value?.changeAmount.Copy(keyValuePair2.Value, true, true, true);
            }
            for (int k = 0; k < __instance.expression.Length; k++) {
                _char.EnableExpressionCategory(k, __instance.expression[k]);
            }
            return false;
        }

        //將男女的變更角色讀取都導向這裡，取消讀取的性別檢核
        [HarmonyPrefix, HarmonyPatch(typeof(CharaList), "ChangeCharaFemale")]
        public static bool ChangeCharaFemale(object __instance) {
            return ChangeCharaPrefix(__instance);
        }
        [HarmonyPrefix, HarmonyPatch(typeof(CharaList), "ChangeCharaMale")]
        public static bool ChangeCharaMale(object __instance) {
            return ChangeCharaPrefix(__instance);
        }
        public static bool ChangeCharaPrefix(object __instance) {
            OCIChar[] array = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                               select Studio.Studio.GetCtrlInfo(v) as OCIChar into v
                               where v != null
                               select v).ToArray();
            int num = array.Length;
            for (int i = 0; i < num; i++) {
                array[i].ChangeChara((__instance.GetField("charaFileSort") as CharaFileSort).selectPath);
                if ((int)((CharaList)__instance).GetField("sex") == 0) {
                    KK_StudioAllGirlsPlugin.Logger.LogInfo($"{array[i].charInfo.name} is a girl now!");
                }
            }
            return false;
        }

        //左側角色清單的OnSelect
        [HarmonyTranspiler, HarmonyPatch(typeof(CharaList), "OnSelectChara")]
        public static IEnumerable<CodeInstruction> OnSelectCharaTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Brfalse) //Line 25
                {
                    i++; //Line 26
                    codes.Insert(i, new CodeInstruction(OpCodes.Ldc_I4_1)); //Insert at Line 26

                    for (int j = i; j < codes.Count; j++) {
                        if (codes[j].opcode == OpCodes.Ceq) {
                            j++;
                            codes.RemoveRange(i + 1, j - i - 1);  //Remove from Line 26+1(ldloc.1) to Line 31+1(ceq)
                            break;
                        }
                    }
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        //右側物品清單的OnSelect
        [HarmonyTranspiler, HarmonyPatch(typeof(CharaList), "OnSelect")]
        public static IEnumerable<CodeInstruction> OnSelectTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Stloc_1 && codes[i - 1].opcode == OpCodes.Isinst) //Line 30
                {
                    codes.RemoveRange(i, 8);
                    codes[i].opcode = OpCodes.Brtrue_S;
                }
            }
            return codes.AsEnumerable();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CharaList), "OnDeselect")]
        public static bool OnDeselectPrefix(object __instance, TreeNodeObject _node) {
            if (_node == null) {
                return false;
            }
            if (!Singleton<Studio.Studio>.IsInstance()) {
                return false;
            }
            OCIChar[] self = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                              select Studio.Studio.GetCtrlInfo(v) as OCIChar into v
                              where v != null
                              select v).ToArray<OCIChar>();
            ((Button)__instance.GetField("buttonChange")).interactable = !self.IsNullOrEmpty<OCIChar>();
            return false;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(CharaList), "OnDelete")]
        public static IEnumerable<CodeInstruction> OnDeleteTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Stloc_0 && codes[i - 1].opcode == OpCodes.Isinst) //Line 30
                {
                    codes.RemoveRange(i, 8);
                    codes[i].opcode = OpCodes.Brtrue_S;
                }
            }
            return codes.AsEnumerable();
        }

        //將所有男性讀取為女性
        //將AddMale重定向至AddFemale
        [HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), "AddMale")]
        public static bool AddMalePrefix(string _path) {
            Singleton<Studio.Studio>.Instance.AddFemale(_path);
            KK_StudioAllGirlsPlugin.Logger.LogInfo($"{_path} is a girl now!");
            return false;
        }

        //Studio.AddObjectAssist處的角色讀取，將性別檢核拿掉，只以女性讀取
        [HarmonyPrefix, HarmonyPatch(typeof(AddObjectAssist), "LoadChild", new Type[] { typeof(ObjectInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject) })]
        public static bool LoadChildPrefix(ObjectInfo _child, ObjectCtrlInfo _parent = null, TreeNodeObject _parentNode = null) {
            if (_child.kind == 0) {
                OICharInfo oicharInfo = _child as OICharInfo;
                var female = AddObjectFemale.Load(oicharInfo, _parent, _parentNode);

                if (oicharInfo.sex == 0) {
                    KK_StudioAllGirlsPlugin.Logger.LogInfo($"{female.charInfo.name} is a girl now!");
                    oicharInfo.SetProperty("sex", 1);
                }
                return false;
            }
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "SetParameterBytes")]
        public static void SetParameterBytesPostfix(ChaFile __instance, byte[] data) {
            ChaFileParameter chaFileParameter = MessagePackSerializer.Deserialize<ChaFileParameter>(data);

            //There's no exType before EC_Yoyaku
            if (null != typeof(ChaFileParameter).GetProperty("exType")) {
                chaFileParameter.SetProperty("exType", 0);
            }
            chaFileParameter.sex = 1;

            __instance.parameter.Copy(chaFileParameter);
            return;
        }
    }
}
