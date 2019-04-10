using BepInEx.Logging;
using Extension;
using Harmony;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace KK_StudioCharaLoadSexUnlocker
{
    class CharaList_Patches
    {
        internal static void InitPatch(HarmonyInstance harmony)
        {
            harmony.Patch(typeof(CharaList).GetMethod("InitCharaList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), null, new HarmonyMethod(typeof(CharaList_Patches), "InitCharaListPostfix", null), null);
            harmony.Patch(typeof(CharaList).GetMethod("OnSelect", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(CharaList_Patches), "OnSelectPrefix", null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("OnDelete", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(CharaList_Patches), "OnDeletePrefix", null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("OnDeselect", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(CharaList_Patches), "OnDeselectPrefix", null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("OnSelectChara", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(CharaList_Patches), "OnSelectCharaPrefix", null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("ChangeCharaFemale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(CharaList_Patches), "ChangeCharaPrefix", null), null, null);
            harmony.Patch(typeof(CharaList).GetMethod("ChangeCharaMale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(CharaList_Patches), "ChangeCharaPrefix", null), null, null);
        }

        private class CharaListArrayObj
        {
            public Button buttonChange;
            public Button buttonLoad;
            public CharaFileSort charaFileSort;
        }
        private static List<CharaListArrayObj> charaListArray = new List<CharaListArrayObj>() { new CharaListArrayObj() ,new CharaListArrayObj()};

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
                (array[i].objectInfo as OICharInfo).SetPrivateProperty("sex", sex);
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
    }
}
