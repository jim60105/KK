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
            harmony.Patch(typeof(AddObjectFemale).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy|BindingFlags.Static),null, new HarmonyMethod(typeof(Patches), nameof(AddPostFix), new Type[] { typeof(ChaFileStatus)}), null);
            //harmony.Patch(typeof(ChaControl).GetMethod("LoadAsync", AccessTools.all),null, new HarmonyMethod(typeof(Patches), nameof(LoadAsyncPostfix),null), null);
            harmony.Patch(typeof(ChaReference).GetMethod("CreateReferenceInfo", AccessTools.all),null, new HarmonyMethod(typeof(Patches), nameof(CreateReferenceInfoPostfix),null), null);
        }

        public static void CreateReferenceInfoPostfix(ChaReference __instance, ulong flags, GameObject objRef)
        {
            if (flags >= 1UL && flags <= 15UL && (int)(flags - 1UL)==2)
            {
                GameObject simpleBodyGameObject = CommonLib.LoadAsset<GameObject>("chara/oo_base.unity3d", "p_cm_body_00", true, Singleton<Character>.Instance.mainManifestName);
                FindAssist findAssist2 = new FindAssist();
                findAssist2.Initialize(simpleBodyGameObject.transform);
                if (
                    typeof(ChaReference).GetFields(AccessTools.all).Where(x => x.Name == "dictRefObj")
                    .FirstOrDefault().GetValue(__instance)
                    is Dictionary<ChaReference.RefObjKey, GameObject> dic
                    )
                {
                    simpleBodyGameObject.isStatic = true;
                    dic.Remove(ChaReference.RefObjKey.S_SimpleTop);
                    dic.Remove(ChaReference.RefObjKey.S_SimpleBody);
                    dic.Remove(ChaReference.RefObjKey.S_SimpleTongue);
                    dic[ChaReference.RefObjKey.S_SimpleTop] = findAssist2.GetObjectFromName("n_silhouetteTop");
                    dic[ChaReference.RefObjKey.S_SimpleBody] = findAssist2.GetObjectFromName("n_body_silhouette");
                    dic[ChaReference.RefObjKey.S_SimpleTongue] = findAssist2.GetObjectFromName("n_tang_silhouette");
                    simpleBodyGameObject.transform.SetParent(objRef.transform);

                    HideGameObjMesh(simpleBodyGameObject, new string[] {
                        "o_body_a",
                        "o_nip",
                        "o_tang",
                        "n_dankon",
                        "o_mnpa",
                        "o_mnpb",
                        "n_mnpb",
                        "o_gomu",
                    });

                    return;
                }
            }
        }

        private static Dictionary<string, GameObject> goList = new Dictionary<string, GameObject>();
        private static void HideGameObjMesh(GameObject go, string[] mrNameList)
        {
            FindAll(go.transform);
            foreach (string st in mrNameList)
            {
                if (goList.ContainsKey(st))
                {
                    //Logger.Log(LogLevel.Debug, "[KK_SCLSU] Hide GameObj Name: " + st);
                    GameObject g = null;
                    if (goList.TryGetValue(st, out g))
                    {
                            g.SetActive(false);
                    }
                    else
                    {
                        //Logger.Log(LogLevel.Error, "[KK_SCLSU] g NotGet: " + st);
                    }
                }else
                {
                    //Logger.Log(LogLevel.Error, "[KK_SCLSU] Hide Mesh Name FAILED: " + st);
                }
            }
        }
        private static void FindAll(Transform trf)
        {
            if (!goList.ContainsKey(trf.name))
            {
                goList[trf.name] = trf.gameObject;
            }
            for (int i = 0; i < trf.childCount; i++)
            {
                FindAll(trf.GetChild(i));
            }
        }

        //public static void LoadAsyncPostfix(ChaControl __instance)
        //{
        //    ((ChaInfo)__instance).SetPrivateProperty("loadEnd", false);

        //    string mainManifestName = Singleton<Character>.Instance.mainManifestName;
        //    string assetBundleName3 = "chara/oo_base.unity3d";
        //    string assetName3Simple = (!__instance.hiPoly) ? "p_cm_body_00_low" : "p_cm_body_00";
        //    GameObject simpleBodyGameObject = CommonLib.LoadAsset<GameObject>(assetBundleName3, assetName3Simple, true, mainManifestName);
        //    //Singleton<Character>.Instance.AddLoadAssetBundle(assetBundleName3, mainManifestName);
        //    if (simpleBodyGameObject)
        //    {
        //        bool flag = false;
        //        if (
        //            typeof(ChaReference).GetFields(AccessTools.all).Where(x => x.Name == "dictRefObj")
        //            .FirstOrDefault().GetValue(__instance)
        //            is Dictionary<ChaReference.RefObjKey, GameObject> dic
        //            )
        //        {
        //            dic.Remove(ChaReference.RefObjKey.S_SimpleTop);
        //            dic.Remove(ChaReference.RefObjKey.S_SimpleBody);
        //            dic.Remove(ChaReference.RefObjKey.S_SimpleTongue);

        //            FindAssist findAssist = new FindAssist();
        //            findAssist.Initialize(simpleBodyGameObject.transform);

        //            dic[ChaReference.RefObjKey.S_SimpleTop] = findAssist.GetObjectFromName("n_silhouetteTop");
        //            dic[ChaReference.RefObjKey.S_SimpleBody] = findAssist.GetObjectFromName("n_body_silhouette");
        //            dic[ChaReference.RefObjKey.S_SimpleTongue] = findAssist.GetObjectFromName("n_tang_silhouette");
        //            Logger.Log(LogLevel.Debug, "[KK_SCLSU] FINISH!!!!!!");
        //            flag = true;
        //        }

        //        if (!flag)
        //        {
        //            Logger.Log(LogLevel.Error, "[KK_SCLSU] Set Simple Renderer FAILED");
        //            GameObject.Destroy(simpleBodyGameObject);
        //            return;
        //        }
        //        GameObject referenceInfo5 = ((ChaReference)__instance).GetReferenceInfo(ChaReference.RefObjKey.S_SimpleBody);
        //        if (referenceInfo5)
        //        {
        //            ((ChaInfo)__instance).SetPrivateProperty("rendSimpleBody", referenceInfo5.GetComponent<Renderer>());
        //        }
        //        GameObject referenceInfo6 = ((ChaReference)__instance).GetReferenceInfo(ChaReference.RefObjKey.S_SimpleTongue);
        //        if (referenceInfo6)
        //        {
        //            ((ChaInfo)__instance).SetPrivateProperty("rendSimpleTongue", referenceInfo6.GetComponent<Renderer>());
        //        }
        //        var root = __instance.objRoot;
        //        Logger.Log(LogLevel.Debug, "[KK_SCLSU] Flag2");
        //        simpleBodyGameObject.transform.SetParent(root.transform, false);
        //        Logger.Log(LogLevel.Debug, "[KK_SCLSU] Flag1");
        //    }

        //    if (Singleton<Character>.Instance.enableCharaLoadGCClear)
        //    {
        //        Resources.UnloadUnusedAssets();
        //        GC.Collect();
        //    }
        //    ((ChaInfo)__instance).SetPrivateProperty("loadEnd", true);
        //    return;
        //}

        //public static bool ReplaceReferenceInfoSimple(object instance, GameObject objRef)
        //{
        //    //Release

        //    return false;
        //}

        private static void AddPostFix(ChaControl _female, OICharInfo _info, ObjectCtrlInfo _parent, TreeNodeObject _parentNode, bool _addInfo, int _initialPosition, ref OCICharFemale __result, ChaFileStatus ___chaFileStatus)
        {
            Logger.Log(LogLevel.Debug, "[KK_SCLSU] Add Patch Start");
            __result.SetVisibleSimple(_info.visibleSimple);
            __result.SetSimpleColor(_info.simpleColor);
            AddObjectAssist.UpdateState(__result, ___chaFileStatus);
            return;
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
            FieldInfo[] fieldInfo = __instance.GetType().GetFields();
            foreach (var fi in fieldInfo)
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
            methodInfo.Invoke(__instance, new object[] { _char.oiCharInfo.simpleColor });
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
            Logger.Log(LogLevel.Debug, "[KK_SCLSU] Set Visible Simple:" + ociChar.oiCharInfo.visibleSimple);
        }

        public static void OnValueChangeSimpleColorPostfix(MPCharCtrl __instance, Color _color)
        {
            //base.ociChar.SetSimpleColor(_color);
            //ociChar.oiCharInfo.simpleColor = _color;
            ociChar.charInfo.ChangeSimpleBodyColor(_color);
            ChangeSimpleBodyColorPrefix(_color);
            Logger.Log(LogLevel.Debug, "[KK_SCLSU] Set Simple Color:" + ociChar.oiCharInfo.simpleColor.ToString());

            //this.otherInfo.SetSimpleColor(_color);
            if (null != colorBtn)
            {
                colorBtn.buttons[0].image.color = _color;
            }
        }

        public static bool ChangeSimpleBodyColorPrefix(Color color)
        {
            ociChar.charInfo.fileStatus.simpleColor = color;
            if (ociChar.charInfo.rendSimpleBody)
            {
                Material material = ociChar.charInfo.rendSimpleBody.material;
                if (material)
                {
                    material.SetColor(ChaShader._Color, color);
                    Logger.Log(LogLevel.Debug, "[KK_SCLSU] Set Body Simple Color:" + color);
                }
            }
            else
            {
                Logger.Log(LogLevel.Debug, "[KK_SCLSU] No Simple Body Rendered");
            }
            if (ociChar.charInfo.rendSimpleTongue)
            {
                Material material2 = ociChar.charInfo.rendSimpleTongue.material;
                if (material2)
                {
                    material2.SetColor(ChaShader._Color, color);
                    Logger.Log(LogLevel.Debug, "[KK_SCLSU] Set Tongue Simple Color:" + color);
                }
            }
            return false;
        }
    }
}
