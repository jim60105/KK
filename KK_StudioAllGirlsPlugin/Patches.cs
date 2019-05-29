using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using BepInEx.Logging;

using Extension;

using Harmony;

using MessagePack;

using Studio;

using UniRx;

using UnityEngine.UI;

using Logger = BepInEx.Logger;

namespace KK_StudioAllGirlsPlugin
{
    class Patches
    {
        internal static void InitPatch(HarmonyInstance harmony)
        {
            harmony.Patch(typeof(PauseCtrl).GetMethod("CheckIdentifyingCode", AccessTools.all), null, null, new HarmonyMethod(typeof(Patches), nameof(CheckIdentifyingCodePrefixTranspiler), null));
            harmony.Patch(typeof(PauseCtrl).GetNestedType("FileInfo", BindingFlags.Public).GetMethod("Apply", AccessTools.all), new HarmonyMethod(typeof(Patches), nameof(ApplyPrefix), null), null, null);

            harmony.Patch(typeof(CharaList).GetMethod("ChangeCharaFemale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(ChangeCharaPrefix), null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("ChangeCharaMale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(ChangeCharaPrefix), null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("OnSelectChara", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), null, null, new HarmonyMethod(typeof(Patches), nameof(OnSelectCharaTranspiler), null));
            harmony.Patch(typeof(CharaList).GetMethod("OnSelect", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), null, null, new HarmonyMethod(typeof(Patches), nameof(OnSelectTranspiler), null));
            harmony.Patch(typeof(CharaList).GetMethod("OnDeselect", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(OnDeselectPrefix), null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("OnDelete", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), null, null, new HarmonyMethod(typeof(Patches), nameof(OnDeleteTranspiler), null));

            harmony.Patch(typeof(Studio.Studio).GetMethod("AddMale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(AddMalePrefix), null), null, null);
            harmony.Patch(typeof(AddObjectAssist).GetMethod("LoadChild", new Type[] { typeof(ObjectInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject) }), new HarmonyMethod(typeof(Patches), nameof(LoadChildPrefix), null), null, null);
            harmony.Patch(typeof(ChaFile).GetMethod("SetParameterBytes", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(SetParameterBytesPostfix), null), null);
        }

        //PauseCtrl(PoseCtrl), 取消姿勢讀取的性別檢核
        private static IEnumerable<CodeInstruction> CheckIdentifyingCodePrefixTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Beq) //Line 23 
                {
                    i++; //Line 24 
                    if (codes[i].opcode == OpCodes.Ldc_I4_0) //Double Check
                    {
                        codes.RemoveRange(i, 3);
                    }
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        //PauseCtrl(PoseCtrl), 修正姿勢讀取的FK bone not found Error
        private static bool ApplyPrefix(OCIChar _char, PauseCtrl.FileInfo __instance)
        {
            _char.LoadAnime(__instance.group, __instance.category, __instance.no, __instance.normalizedTime);
            for (int i = 0; i < __instance.activeIK.Length; i++)
            {
                _char.ActiveIK((OIBoneInfo.BoneGroup)(1 << i), __instance.activeIK[i], false);
            }
            _char.ActiveKinematicMode(OICharInfo.KinematicMode.IK, __instance.enableIK, true);
            foreach (KeyValuePair<int, ChangeAmount> keyValuePair in __instance.dicIK)
            {
                _char.oiCharInfo.ikTarget[keyValuePair.Key].changeAmount.Copy(keyValuePair.Value, true, true, true);
            }
            for (int j = 0; j < __instance.activeFK.Length; j++)
            {
                _char.ActiveFK(FKCtrl.parts[j], __instance.activeFK[j], false);
            }
            _char.ActiveKinematicMode(OICharInfo.KinematicMode.FK, __instance.enableFK, true);
            foreach (KeyValuePair<int, ChangeAmount> keyValuePair2 in __instance.dicFK)
            {
                _char.oiCharInfo.bones.TryGetValue(keyValuePair2.Key, out var value);
                value?.changeAmount.Copy(keyValuePair2.Value, true, true, true);
            }
            for (int k = 0; k < __instance.expression.Length; k++)
            {
                _char.EnableExpressionCategory(k, __instance.expression[k]);
            }
            return false;
        }

        //將男女的變更角色讀取都導向這裡，取消讀取的性別檢核
        public static bool ChangeCharaPrefix(object __instance)
        {
            OCIChar[] array = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                               select Studio.Studio.GetCtrlInfo(v) as OCIChar into v
                               where v != null
                               select v).ToArray();
            int num = array.Length;
            for (int i = 0; i < num; i++)
            {
                array[i].ChangeChara((__instance.GetPrivate("charaFileSort") as CharaFileSort).selectPath);
                if ((int)((CharaList)__instance).GetPrivate("sex") == 0)
                {
                    Logger.Log(LogLevel.Info, $"[KK_SAGP] {array[i].charInfo.name} is a girl now!");
                }
            }
            return false;
        }

        //左側角色清單的OnSelect
        private static IEnumerable<CodeInstruction> OnSelectCharaTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Brfalse) //Line 25
                {
                    i++; //Line 26
                    codes.Insert(i, new CodeInstruction(OpCodes.Ldc_I4_1)); //Insert at Line 26

                    for (int j = i; j < codes.Count; j++)
                    {
                        if (codes[j].opcode == OpCodes.Ceq)
                        {
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
        private static IEnumerable<CodeInstruction> OnSelectTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Stloc_1 && codes[i - 1].opcode == OpCodes.Isinst) //Line 30
                {
                    codes.RemoveRange(i, 8);
                    codes[i].opcode = OpCodes.Brtrue_S;
                }
            }
            return codes.AsEnumerable();
        }

        private static bool OnDeselectPrefix(object __instance, TreeNodeObject _node)
        {
            if (_node == null)
            {
                return false;
            }
            if (!Singleton<Studio.Studio>.IsInstance())
            {
                return false;
            }
            OCIChar[] self = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                              select Studio.Studio.GetCtrlInfo(v) as OCIChar into v
                              where v != null
                              select v).ToArray<OCIChar>();
            ((Button)__instance.GetPrivate("buttonChange")).interactable = !self.IsNullOrEmpty<OCIChar>();
            return false;
        }

        private static IEnumerable<CodeInstruction> OnDeleteTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
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
        public static bool AddMalePrefix(string _path)
        {
            Singleton<Studio.Studio>.Instance.AddFemale(_path);
            Logger.Log(LogLevel.Info, $"[KK_SAGP] {_path} is a girl now!");
            return false;
        }

        //Studio.AddObjectAssist處的角色讀取，將性別檢核拿掉，只以女性讀取
        public static bool LoadChildPrefix(ObjectInfo _child, ObjectCtrlInfo _parent = null, TreeNodeObject _parentNode = null)
        {
            if (_child.kind == 0)
            {
                OICharInfo oicharInfo = _child as OICharInfo;
                var female = AddObjectFemale.Load(oicharInfo, _parent, _parentNode);

                if (oicharInfo.sex == 0)
                {
                    Logger.Log(LogLevel.Info, $"[KK_SAGP] {female.charInfo.name} is a girl now!");
                    oicharInfo.SetPrivateProperty("sex", 1);
                }
                return false;
            }
            return true;
        }

        public static void SetParameterBytesPostfix(ChaFile __instance, byte[] data)
        {
            ChaFileParameter chaFileParameter = MessagePackSerializer.Deserialize<ChaFileParameter>(data);

            //There's no exType before EC_Yoyaku
            if (null != typeof(ChaFileParameter).GetProperty("exType"))
            {
                chaFileParameter.SetPrivateProperty("exType", 0);
            }
            chaFileParameter.sex = 1;

            __instance.parameter.Copy(chaFileParameter);
            return;
        }
    }
}
