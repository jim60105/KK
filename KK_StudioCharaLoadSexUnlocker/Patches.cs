using BepInEx.Logging;
using Extension;
using Harmony;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine.UI;
using UnityEngine;
using Logger = BepInEx.Logger;
using Manager;
using MessagePack;

using System.Reflection;

namespace KK_StudioCharaLoadSexUnlocker
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
            harmony.Patch(typeof(MPCharCtrl).GetNestedType("StateInfo", BindingFlags.NonPublic).GetMethod("OnValueChangedSimple", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(OnValueChangedSimplePostfix), null), null);
            harmony.Patch(typeof(MPCharCtrl).GetNestedType("StateInfo", BindingFlags.NonPublic).GetMethod("OnValueChangeSimpleColor", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(OnValueChangeSimpleColorPostfix), null), null);
            harmony.Patch(typeof(MPCharCtrl).GetNestedType("OtherInfo", BindingFlags.Public).GetMethod("UpdateInfo", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(UpdateInfoPostfix), null), null);
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
                Logger.Log(LogLevel.Error, "[KK_SCLSU] Get button FAILED");
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
                Logger.Log(LogLevel.Error, "[KK_SCLSU] Get instance FAILED");
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
            __instance.parameter.Copy(chaFileParameter);
            //Logger.Log(LogLevel.Debug, "[KK_SCLSU] Set Sex: "+chaFileParameter.sex);
            return;
        }

        private static MPCharCtrl.StateButtonInfo colorBtn;
        private static OCIChar ociChar;

        public static void UpdateInfoPostfix(MPCharCtrl.OtherInfo __instance, OCIChar _char)
        {
            Logger.Log(LogLevel.Debug, "[KK_SCLSU] Info Update start");
            //StateInfoType.SetType(typeof(MPCharCtrl).GetNestedType("StateInfo", BindingFlags.NonPublic));
            FieldInfo[] fieldInfo = __instance.GetType().GetFields();
            foreach(var fi in fieldInfo)
            {
                //Logger.Log(LogLevel.Debug, "[KK_SCLSU] Name: " + fi.Name);
                //Logger.Log(LogLevel.Debug, "[KK_SCLSU] FieldType: " + fi.FieldType);
                try
                {
                    if (fi.Name == "single")
                    {
                        var o = (MPCharCtrl.StateToggleInfo)fi.GetValue(__instance);
                        o.active = true;
                        o.toggle.isOn = _char.GetVisibleSimple();
                    }
                    else if (fi.Name == "color")
                    {
                        var o2 = (MPCharCtrl.StateButtonInfo)fi.GetValue(__instance);
                        o2.active = true;
                        colorBtn = o2;
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Error, "[KK_SCLSU] Exception: " + e);
                    Logger.Log(LogLevel.Error, "[KK_SCLSU] Exception: " + e.Message);
                }
            }
            MethodInfo methodInfo = __instance.GetType().GetMethod("SetSimpleColor");
            methodInfo.Invoke(__instance,new object[] { _char.oiCharInfo.simpleColor });
            ociChar = _char;
        }

        public static void OnValueChangedSimplePostfix(object __instance, bool _value)
        {
            //Logger.Log(LogLevel.Debug, "[KK_SCLSU] Flag1");
            if ((bool)__instance.GetPrivateProperty("isUpdateInfo"))
            {
                return;
            }
            //Logger.Log(LogLevel.Debug, "[KK_SCLSU] Flag2");
            ociChar.oiCharInfo.visibleSimple = _value;
            //Logger.Log(LogLevel.Debug, "[KK_SCLSU] Flag3");
            ociChar.charInfo.fileStatus.visibleSimple = _value;
            Logger.Log(LogLevel.Debug, "[KK_SCLSU] Set Visible Simple:"+ ociChar.oiCharInfo.visibleSimple );
        }

        public static void OnValueChangeSimpleColorPostfix(MPCharCtrl __instance, Color _color)
        {
            //base.ociChar.SetSimpleColor(_color);
            ociChar.oiCharInfo.simpleColor = _color;
            ociChar.charInfo.ChangeSimpleBodyColor(_color);
            Logger.Log(LogLevel.Debug, "[KK_SCLSU] Set Simple Color:"+ ociChar.oiCharInfo.simpleColor.ToString() );

            //this.otherInfo.SetSimpleColor(_color);
            if (null != colorBtn)
            {
                colorBtn.buttons[0].image.color = _color;
            }
        }
    }
}
