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
        internal const string PLUGIN_VERSION = "20.08.30.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.0.2";

        public void Awake() {
            Extension.Logger.logger = Logger;
            Harmony.CreateAndPatchAll(typeof(Patches));
        }
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
                 int sex = (int)__instance.GetField("sex");
                 ocichar.charInfo.fileParam.sex = (byte)sex;
                 ocichar.optionItemCtrl.oiCharInfo.SetProperty<OICharInfo>("sex", sex);
                 ShapeBodyInfoFemale sib = ocichar.charInfo.sibBody as ShapeBodyInfoFemale;
                 sib.correctHeadSize = sex == 0 ? 0.91f : 1f;
                 sib.correctNeckSize = sex == 0 ? 0.91f : 1f;

                 ocichar.ChangeChara((__instance.GetField("charaFileSort") as CharaFileSort).selectPath);
             });
            return false;
        }

        //讓CharaList的Change按鈕顯示
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

        #region 補齊OCICharMale對比OCICharFemale缺少的Method
        [HarmonyPostfix, HarmonyPatch(typeof(OCICharMale), nameof(OCICharMale.ChangeChara))]
        public static void ChangeCharaPostfix(OCICharMale __instance) {
            if (__instance.charInfo.fileParam.sex == 0) return;

            __instance.charInfo.UpdateBustSoftnessAndGravity();
            __instance.optionItemCtrl.height = __instance.charInfo.fileBody.shapeValueBody[0];
            __instance.charInfo.setAnimatorParamFloat("height", __instance.charInfo.fileBody.shapeValueBody[0]);
            if (__instance.isAnimeMotion) {
                __instance.charInfo.setAnimatorParamFloat("breast", __instance.charInfo.fileBody.shapeValueBody[1]);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(OCICharMale), nameof(OCICharMale.LoadClothesFile))]
        public static void LoadClothesFilePostfix(OCICharMale __instance) => UpdateBustAndSkirt(__instance);

        [HarmonyPostfix, HarmonyPatch(typeof(OCICharMale), nameof(OCICharMale.SetCoordinateInfo))]
        public static void SetCoordinateInfoPostfix(OCICharMale __instance) => UpdateBustAndSkirt(__instance);

        private static void UpdateBustAndSkirt(OCICharMale __instance) {
            if (__instance.charInfo.fileParam.sex == 0) return;

            __instance.charInfo.UpdateBustSoftnessAndGravity();
            __instance.skirtDynamic = AddObjectFemale.GetSkirtDynamic(__instance.charInfo.objClothes);
            __instance.ActiveFK(OIBoneInfo.BoneGroup.Skirt, __instance.oiCharInfo.activeFK[6], __instance.oiCharInfo.enableFK);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(OCIChar), nameof(OCIChar.SetNipStand))]
        public static void SetNipStandPostfix(OCIChar __instance, float _value) {
            if (__instance.charInfo.fileParam.sex == 0 || __instance is OCICharFemale) return;

            __instance.oiCharInfo.nipple = _value;
            __instance.charInfo.ChangeNipRate(_value);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(OCIChar), nameof(OCIChar.GetSiruFlags))]
        public static void GetSiruFlagsPostfix(OCIChar __instance, ChaFileDefine.SiruParts _parts, ref byte __result) {
            if (__instance.charInfo.fileParam.sex == 0 || __instance is OCICharFemale) return;

            __result = __instance.charInfo.GetSiruFlags(_parts);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(OCIChar), nameof(OCIChar.SetSiruFlags))]
        public static void SetSiruFlagsPostfix(OCIChar __instance, ChaFileDefine.SiruParts _parts, byte _state) {
            if (__instance.charInfo.fileParam.sex == 0 || __instance is OCICharFemale) return;

            __instance.charInfo.SetSiruFlags(_parts, _state);
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
