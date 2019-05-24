using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            harmony.Patch(typeof(CharaList).GetMethod("OnSelect", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(OnSelectPrefix), null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("OnDelete", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(OnDeletePrefix), null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("OnDeselect", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(OnDeselectPrefix), null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("OnSelectChara", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), null, new HarmonyMethod(typeof(Patches), nameof(OnSelectCharaPostfix), null), null);
            harmony.Patch(typeof(CharaList).GetMethod("ChangeCharaFemale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(ChangeCharaPrefix), null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("ChangeCharaMale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(ChangeCharaPrefix), null), null, null);
            harmony.Patch(typeof(PauseCtrl).GetMethod("CheckIdentifyingCode", AccessTools.all), new HarmonyMethod(typeof(Patches), nameof(CheckIdentifyingCodePrefix), null), null, null);
            harmony.Patch(typeof(Studio.Studio).GetMethod("AddMale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(AddMalePrefix), null), null, null);
            harmony.Patch(typeof(AddObjectAssist).GetMethod("LoadChild", new Type[] { typeof(ObjectInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject) }), new HarmonyMethod(typeof(Patches), nameof(LoadChildPrefix), null), null, null);
            harmony.Patch(typeof(ChaFile).GetMethod("SetParameterBytes", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(SetParameterBytesPostfix), null), null);
            harmony.Patch(typeof(MPCharCtrl).GetNestedType("OtherInfo", BindingFlags.Public).GetMethod("UpdateInfo", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(UpdateInfoPostfix), null), null);
            harmony.Patch(typeof(PauseCtrl).GetNestedType("FileInfo", BindingFlags.Public).GetMethod("Apply", AccessTools.all), new HarmonyMethod(typeof(Patches), nameof(ApplyPrefix), null), null, null);
        }

        //PauseCtrl(PoseCtrl), Cancel gender restrictions of pose reading
        public static bool CheckIdentifyingCodePrefix(ref bool __result, string _path, int _sex)
        {
            using (FileStream fileStream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    if (string.Compare(binaryReader.ReadString(), "【pose】") != 0)
                    {
                        __result = false;
                        return false;
                    }
                    binaryReader.ReadInt32();
                }
            }
            __result = true;
            return false;
        }

        //Fix Pose Loading FK bone not found Error
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

        //Work when change button clicked
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

        //OnSelect at the left filesort
        private static void OnSelectCharaPostfix(object __instance, int _idx)
        {
            if ((__instance.GetPrivate("buttonChange") as Button).interactable != true)
            {
                if (_idx < 0)
                {
                    return;
                }
                ObjectCtrlInfo ctrlInfo = Studio.Studio.GetCtrlInfo(Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNode);
                ((Button)__instance.GetPrivate("buttonChange")).interactable = ctrlInfo is OCIChar ocichar
                                                                               && ocichar.sex > -1
                                                                               && ctrlInfo.kind == 0;
                __instance.SetPrivate("isDelay", true);
                Observable.Timer(TimeSpan.FromMilliseconds(250.0)).Subscribe(delegate (long _)
                {
                    __instance.SetPrivate("isDelay", false);
                }).AddTo((CharaList)__instance);
                return;
            }
        }

        //OnSelect at the right panel
        private static bool OnSelectPrefix(object __instance, TreeNodeObject _node)
        {
            var buttonChange = (Button)__instance.GetPrivate("buttonChange");
            if (null == buttonChange || null == __instance.GetPrivate("charaFileSort"))
            {
                Logger.Log(LogLevel.Error, "[KK_SAGP] Get instance FAILED");
                return true;
            }
            if (_node == null ||
                !Singleton<Studio.Studio>.IsInstance() ||
                !Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(_node, out ObjectCtrlInfo objectCtrlInfo) ||
                null == objectCtrlInfo ||
                objectCtrlInfo.kind != 0)
            {
                buttonChange.interactable = false;
                return false;
            }
            if ((__instance.GetPrivate("charaFileSort") as CharaFileSort).select != -1)
            {
                buttonChange.interactable = true;
            }
            return false;
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

        private static bool OnDeletePrefix(object __instance, ObjectCtrlInfo _info)
        {
            if (_info == null)
            {
                return false;
            }
            if (_info.kind != 0)
            {
                return false;
            }
            if (!(_info is OCIChar))
            {
                return false;
            }
            if ((__instance.GetPrivate("charaFileSort") as CharaFileSort).select != -1)
            {
                ((Button)__instance.GetPrivate("buttonChange")).interactable = false;
            }
            return false;
        }

        //Load all the male as female
        //Redirect AddMale to AddFemale
        public static bool AddMalePrefix(string _path)
        {
            Singleton<Studio.Studio>.Instance.AddFemale(_path);
            Logger.Log(LogLevel.Info, $"[KK_SAGP] {_path} is a girl now!");
            return false;
        }

        public static bool LoadChildPrefix(ObjectInfo _child, ObjectCtrlInfo _parent = null, TreeNodeObject _parentNode = null)
        {
            if (_child.kind == 0)
            {
                OICharInfo oicharInfo = _child as OICharInfo;
                var female = AddObjectFemale.Load(oicharInfo, _parent, _parentNode);

                if (oicharInfo.sex == 0)
                {
                    Logger.Log(LogLevel.Info, $"[KK_SAGP] {female.charInfo.name} is a girl now!");
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
            //Logger.Log(LogLevel.Debug, "[KK_SAGP] Set Sex: " + chaFileParameter.sex);

            __instance.parameter.Copy(chaFileParameter);
            return;
        }

        //Active man->female's nipple slider
        public static void UpdateInfoPostfix(MPCharCtrl.OtherInfo __instance, OCIChar _char)
        {
            FieldInfo[] fieldInfo = __instance.GetType().GetFields();
            foreach (var fi in fieldInfo)
            {
                //Logger.Log(LogLevel.Debug, "[KK_SSCOG] Name: " + fi.Name);
                //Logger.Log(LogLevel.Debug, "[KK_SSCOG] FieldType: " + fi.FieldType);
                try
                {
                    if (fi.Name == "nipple")
                    {
                        var o = (MPCharCtrl.StateSliderInfo)fi.GetValue(__instance);
                        o.active = true;
                        o.slider.value = _char.oiCharInfo.nipple;
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Error, "[KK_SAGP] Exception: " + e);
                    Logger.Log(LogLevel.Error, "[KK_SAGP] Exception: " + e.Message);
                }
            }
            Logger.Log(LogLevel.Debug, "[KK_SAGP] Chara Status Info Updated");
        }
    }
}
