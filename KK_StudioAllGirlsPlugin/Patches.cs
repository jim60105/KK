using BepInEx.Logging;
using Extension;
using Harmony;
using Manager;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace KK_StudioAllGirlsPlugin
{
    class Patches
    {
        internal static void InitPatch(HarmonyInstance harmony)
        {
            harmony.Patch(typeof(CharaList).GetMethod("InitCharaList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), null, new HarmonyMethod(typeof(Patches), nameof(InitCharaListPostfix), null), null);
            harmony.Patch(typeof(CharaList).GetMethod("OnSelect", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(OnSelectPrefix), null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("OnDelete", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(OnDeletePrefix), null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("OnDeselect", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(OnDeselectPrefix), null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("OnSelectChara", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(OnSelectCharaPrefix), null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("ChangeCharaFemale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(ChangeCharaPrefix), null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("ChangeCharaMale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(ChangeCharaPrefix), null), null, null);
            harmony.Patch(typeof(PauseCtrl).GetMethod("CheckIdentifyingCode", AccessTools.all), new HarmonyMethod(typeof(Patches), nameof(CheckIdentifyingCodePrefix), null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("LoadCharaMale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(Patches), nameof(LoadCharaMalePrefix), null), null, null);
            harmony.Patch(typeof(AddObjectAssist).GetMethod("LoadChild", new Type[] { typeof(ObjectInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject) }), new HarmonyMethod(typeof(Patches), nameof(LoadChildPrefix), null), null, null);
            harmony.Patch(typeof(ChaFile).GetMethod("SetParameterBytes", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(SetParameterBytesPostfix), null), null);
            harmony.Patch(typeof(MPCharCtrl).GetNestedType("OtherInfo", BindingFlags.Public).GetMethod("UpdateInfo", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(UpdateInfoPostfix), null), null);
            harmony.Patch(typeof(PauseCtrl).GetNestedType("FileInfo", BindingFlags.Public).GetMethod("Apply", AccessTools.all), new HarmonyMethod(typeof(Patches), nameof(ApplyPrefix), null), null, null);
        }

        private class CharaListArrayObj
        {
            public Button buttonChange;
            public Button buttonLoad;
            public CharaFileSort charaFileSort;
        }
        private static List<CharaListArrayObj> charaListArray = new List<CharaListArrayObj>() { new CharaListArrayObj(), new CharaListArrayObj() };

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
        private static bool ApplyPrefix(OCIChar _char,PauseCtrl.FileInfo __instance)
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

        //Get CharaList filesort and button on the left
        public static void InitCharaListPostfix(object __instance)
        {
            var charaList = (CharaList)__instance;
            CharaListArrayObj charaListObject = charaListArray[(int)charaList.GetPrivate("sex")];
            charaListObject.buttonChange = (Button)charaList.GetPrivate("buttonChange");
            charaListObject.buttonLoad = (Button)charaList.GetPrivate("buttonLoad");
            charaListObject.charaFileSort = (CharaFileSort)charaList.GetPrivate("charaFileSort");
            if (null == charaListObject.buttonChange || null == charaListObject.buttonLoad || null == charaListObject.charaFileSort)
            {
                Logger.Log(LogLevel.Error, "[KK_SAGP] Get button FAILED");
            }
        }

        //Work when change button clicked
        public static bool ChangeCharaPrefix(object __instance)
        {
            int sex = (int)__instance.GetPrivate("sex");
            CharaListArrayObj charaListObject = charaListArray[sex];
            OCIChar[] array = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                               select Studio.Studio.GetCtrlInfo(v) as OCIChar into v
                               where v != null
                               select v).ToArray();
            int num = array.Length;
            for (int i = 0; i < num; i++)
            {
                array[i].ChangeChara(charaListObject.charaFileSort.selectPath);
            }

            return false;
        }

        //OnSelect at the left filesort
        private static bool OnSelectCharaPrefix(object __instance, int _idx)
        {
            CharaListArrayObj charaListObject = charaListArray[(int)__instance.GetPrivate("sex")];

            if (charaListObject.charaFileSort.select == _idx || _idx < 0)
            {
                return false;
            }
            charaListObject.charaFileSort.select = _idx;
            charaListObject.buttonLoad.interactable = true;
            ObjectCtrlInfo ctrlInfo = Studio.Studio.GetCtrlInfo(Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNode);
            OCIChar ocichar = ctrlInfo as OCIChar;
            charaListObject.buttonChange.interactable = (null != ocichar && ocichar.sex > -1 && ctrlInfo.kind == 0);
            __instance.SetPrivate("isDelay", true);
            Observable.Timer(TimeSpan.FromMilliseconds(250.0)).Subscribe(delegate (long _)
            {
                __instance.SetPrivate("isDelay", false);
            }).AddTo((CharaList)__instance);
            __instance.SetPrivate("charaFileSort", charaListObject.charaFileSort);
            return false;
        }

        //OnSelect at the right panel
        private static bool OnSelectPrefix(object __instance, TreeNodeObject _node)
        {
            CharaListArrayObj charaListObject = charaListArray[(int)__instance.GetPrivate("sex")];

            if (null == charaListObject.buttonChange || null == charaListObject.charaFileSort)
            {
                Logger.Log(LogLevel.Error, "[KK_SAGP] Get instance FAILED");
                return true;
            }
            if (_node == null)
            {
                charaListObject.buttonChange.interactable = false;
                return false;
            }
            if (!Singleton<Studio.Studio>.IsInstance())
            {
                charaListObject.buttonChange.interactable = false;
                return false;
            }
            if (!Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(_node, out ObjectCtrlInfo objectCtrlInfo))
            {
                charaListObject.buttonChange.interactable = false;
                return false;
            }
            if (null == objectCtrlInfo || objectCtrlInfo.kind != 0)
            {
                charaListObject.buttonChange.interactable = false;
                return false;
            }
            if (charaListObject.charaFileSort.select != -1)
            {
                charaListObject.buttonChange.interactable = true;

            }
            return false;
        }

        private static bool OnDeselectPrefix(object __instance, TreeNodeObject _node)
        {
            CharaListArrayObj charaListObject = charaListArray[(int)__instance.GetPrivate("sex")];
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
            charaListObject.buttonChange.interactable = !self.IsNullOrEmpty<OCIChar>();
            return false;
        }

        private static bool OnDeletePrefix(object __instance, ObjectCtrlInfo _info)
        {
            CharaListArrayObj charaListObject = charaListArray[(int)__instance.GetPrivate("sex")];
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
            if (charaListObject.charaFileSort.select != -1)
            {
                charaListObject.buttonChange.interactable = false;
            }
            return false;
        }

        //Load all the male as female
        public static bool LoadCharaMalePrefix(CharaList __instance)
        {

            Singleton<Studio.Studio>.Instance.AddFemale(charaListArray[0].charaFileSort.selectPath);
            return false;
        }

        public static bool LoadChildPrefix(ObjectInfo _child, ObjectCtrlInfo _parent = null, TreeNodeObject _parentNode = null)
        {
            if (_child.kind == 0)
            {
                OICharInfo oicharInfo = _child as OICharInfo;
                AddObjectFemale.Load(oicharInfo, _parent, _parentNode);
                return false;
            }
            return true;
        }

        public static void SetParameterBytesPostfix(ChaFile __instance, byte[] data)
        {
            ChaFileParameter chaFileParameter = MessagePackSerializer.Deserialize<ChaFileParameter>(data);
            chaFileParameter.sex = 1;

            if (null != typeof(ChaFileParameter).GetProperty("exType")){
                chaFileParameter.SetPrivateProperty("exType", 0);
            }
            __instance.parameter.Copy(chaFileParameter);
            //Logger.Log(LogLevel.Debug, "[KK_SAGP] Set Sex: " + chaFileParameter.sex);
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
