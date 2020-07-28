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
using Extension;
using HarmonyLib;
using Studio;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UniRx;
using UnityEngine.UI;

namespace KK_StudioTransgenderLoading {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    [BepInIncompatibility("com.jim60105.kk.studioallgirlsplugin")]
    public class KK_StudioTransgenderLoading : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Transgender Loading";
        internal const string GUID = "com.jim60105.kk.studiotransgenderloading";
        internal const string PLUGIN_VERSION = "20.07.27.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.0.0";

        public void Awake() => Harmony.CreateAndPatchAll(typeof(Patches));
    }

    class Patches {
        #region Pose
        //PauseCtrl(PoseCtrl), 取消姿勢讀取的性別檢核
        [HarmonyTranspiler, HarmonyPatch(typeof(PauseCtrl), "CheckIdentifyingCode")]
        public static IEnumerable<CodeInstruction> CheckIdentifyingCodeTranspiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
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
                _char.oiCharInfo.bones.TryGetValue(keyValuePair2.Key, out OIBoneInfo value);
                value?.changeAmount.Copy(keyValuePair2.Value, true, true, true);
            }
            for (int k = 0; k < __instance.expression.Length; k++) {
                _char.EnableExpressionCategory(k, __instance.expression[k]);
            }
            return false;
        }
        #endregion

        #region CharaList
        //將男女的變更角色讀取都導向這裡，取消讀取的性別檢核
        [HarmonyPrefix, HarmonyPatch(typeof(CharaList), "ChangeCharaFemale")]
        public static bool ChangeCharaFemale(CharaList __instance) => ChangeChara(__instance);
        [HarmonyPrefix, HarmonyPatch(typeof(CharaList), "ChangeCharaMale")]
        public static bool ChangeCharaMale(CharaList __instance) => ChangeChara(__instance);
        public static bool ChangeChara(CharaList __instance) {
            (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
             select Studio.Studio.GetCtrlInfo(v) as OCIChar into v
             where v != null
             select v).ToList().ForEach((OCIChar ocichar) => {
                 ocichar.charInfo.fileParam.sex = (byte)(int)__instance.GetField("sex");
                 ocichar.ChangeChara((__instance.GetField("charaFileSort") as CharaFileSort).selectPath);
             });
            return false;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(CharaList), "OnSelectChara")]
        public static IEnumerable<CodeInstruction> OnSelectCharaTranspiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
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
        #endregion

        //右側物品清單的OnSelect
        #region Workspace
        [HarmonyTranspiler, HarmonyPatch(typeof(CharaList), "OnSelect")]
        public static IEnumerable<CodeInstruction> OnSelectTranspiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
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
            ((Button)__instance.GetField("buttonChange")).interactable = !(from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                                                                           select (Studio.Studio.GetCtrlInfo(v) as OCIChar) into v
                                                                           where v != null
                                                                           select v).ToArray().IsNullOrEmpty();
            return false;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(CharaList), "OnDelete")]
        public static IEnumerable<CodeInstruction> OnDeleteTranspiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Stloc_0 && codes[i - 1].opcode == OpCodes.Isinst) //Line 30
                {
                    codes.RemoveRange(i, 8);
                    codes[i].opcode = OpCodes.Brtrue_S;
                }
            }
            return codes.AsEnumerable();
        }
        #endregion
    }
}
