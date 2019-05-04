using BepInEx.Logging;
using Extension;
using Harmony;
using Manager;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_StudioSimpleColorOnGirls
{
    class Patches
    {
        internal static void InitPatch(HarmonyInstance harmony)
        {
            harmony.Patch(typeof(MPCharCtrl).GetNestedType("StateInfo", BindingFlags.NonPublic).GetMethod("OnValueChangedSimple", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(OnValueChangedSimplePostfix), null), null);
            harmony.Patch(typeof(MPCharCtrl).GetNestedType("StateInfo", BindingFlags.NonPublic).GetMethod("OnValueChangeSimpleColor", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(OnValueChangeSimpleColorPostfix), null), null);
            harmony.Patch(typeof(MPCharCtrl).GetNestedType("OtherInfo", BindingFlags.Public).GetMethod("UpdateInfo", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(UpdateInfoPostfix), null), null);
            harmony.Patch(typeof(ChaReference).GetMethod("CreateReferenceInfo", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(CreateReferenceInfoPostfix), null), null);
            harmony.Patch(typeof(AddObjectFemale).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy, null, new Type[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) }, null), null, new HarmonyMethod(typeof(Patches), nameof(AddPostFix), null), null);
            harmony.Patch(typeof(AddObjectAssist).GetMethod("UpdateState", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy), null, new HarmonyMethod(typeof(Patches), nameof(UpdateStatePostfix), null), null);

            ////For Debug Only
            //harmony.Patch(typeof(ChaControl).GetMethod("ChangeSimpleBodyColor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), null,new HarmonyMethod(typeof(Patches), nameof(ChangeSimpleBodyColorPostfix), null), null);
            //harmony.Patch(typeof(ChaControl).GetMethod("ChangeSimpleBodyDraw", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), null,new HarmonyMethod(typeof(Patches), nameof(ChangeSimpleBodyDrawPostfix), null), null);
        }

        //Copy Simple Color Functions to Female
        private static MPCharCtrl.StateButtonInfo colorBtn;
        private static OCIChar ociChar;

        public static void UpdateInfoPostfix(MPCharCtrl.OtherInfo __instance, OCIChar _char)
        {
            FieldInfo[] fieldInfo = __instance.GetType().GetFields();
            foreach (var fi in fieldInfo)
            {
                //Logger.Log(LogLevel.Debug, "[KK_SSCOG] Name: " + fi.Name);
                //Logger.Log(LogLevel.Debug, "[KK_SSCOG] FieldType: " + fi.FieldType);
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
                        o2.buttons[0].image.color = _char.oiCharInfo.simpleColor;
                        colorBtn = o2;
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Error, "[KK_SSCOG] Exception: " + e);
                    Logger.Log(LogLevel.Error, "[KK_SSCOG] Exception: " + e.Message);
                }
            }
            MethodInfo methodInfo = __instance.GetType().GetMethod("SetSimpleColor");
            methodInfo.Invoke(__instance, new object[] { _char.oiCharInfo.simpleColor });
            ociChar = _char;
            Logger.Log(LogLevel.Debug, "[KK_SSCOG] Chara Status Info Updated");
        }

        public static void OnValueChangedSimplePostfix(object __instance, bool _value)
        {
            if ((bool)__instance.GetPrivateProperty("isUpdateInfo"))
            {
                return;
            }
            ociChar.oiCharInfo.visibleSimple = _value;
            ociChar.charInfo.fileStatus.visibleSimple = _value;
            Logger.Log(LogLevel.Debug, "[KK_SSCOG] Set Visible Simple:" + ociChar.oiCharInfo.visibleSimple);
        }

        public static void OnValueChangeSimpleColorPostfix(MPCharCtrl __instance, Color _color)
        {
            //base.ociChar.SetSimpleColor(_color);
            ociChar.charInfo.ChangeSimpleBodyColor(_color);
            //ChangeSimpleBodyColorPrefix(_color);  //Debug logging
            Logger.Log(LogLevel.Debug, "[KK_SSCOG] Set Simple Color:" + ociChar.oiCharInfo.simpleColor.ToString());

            //this.otherInfo.SetSimpleColor(_color);
            if (null != colorBtn)
            {
                colorBtn.buttons[0].image.color = _color;
            }
        }

        ////For Debug Only
        //public static bool ChangeSimpleBodyColorPrefix(Color color)
        //{
        //    ociChar.charInfo.fileStatus.simpleColor = color;
        //    if (ociChar.charInfo.rendSimpleBody)
        //    {
        //        Material material = ociChar.charInfo.rendSimpleBody.material;
        //        if (material)
        //        {
        //            material.SetColor(ChaShader._Color, color);
        //            Logger.Log(LogLevel.Debug, "[KK_SSCOG] Set Body Simple Color:" + color);
        //        }
        //    }
        //    else
        //    {
        //        Logger.Log(LogLevel.Debug, "[KK_SSCOG] No Simple Body Rendered");
        //    }
        //    if (ociChar.charInfo.rendSimpleTongue)
        //    {
        //        Material material2 = ociChar.charInfo.rendSimpleTongue.material;
        //        if (material2)
        //        {
        //            material2.SetColor(ChaShader._Color, color);
        //            Logger.Log(LogLevel.Debug, "[KK_SSCOG] Set Tongue Simple Color:" + color);
        //        }
        //    }
        //    return false;
        //}
        //public static void ChangeSimpleBodyDrawPostfix(bool drawSimple)
        //{
        //    Logger.Log(LogLevel.Debug, "[KK_SSCOG] Change Simple visible:" + drawSimple);
        //}
        //public static void ChangeSimpleBodyColorPostfix(Color color)
        //{
        //    Logger.Log(LogLevel.Debug, "[KK_SSCOG] Change Simple Color:" + color);
        //}


        //When loading body, also load unity gameobjects of simply body from male asset 
        public static void CreateReferenceInfoPostfix(ChaReference __instance, ulong flags, GameObject objRef)
        {
            if (flags >= 1UL && flags <= 15UL && (int)(flags - 1UL) == 2)
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
                    dic.TryGetValue(ChaReference.RefObjKey.S_SimpleTop, out GameObject go);
                    go?.SetActive(false);
                    dic.TryGetValue(ChaReference.RefObjKey.S_SimpleBody, out go);
                    go?.SetActive(false);
                    dic.TryGetValue(ChaReference.RefObjKey.S_SimpleTongue, out go);
                    go?.SetActive(false);
                    dic.Remove(ChaReference.RefObjKey.S_SimpleTop);
                    dic.Remove(ChaReference.RefObjKey.S_SimpleBody);
                    dic.Remove(ChaReference.RefObjKey.S_SimpleTongue);
                    dic[ChaReference.RefObjKey.S_SimpleTop] = findAssist2.GetObjectFromName("n_silhouetteTop");
                    dic[ChaReference.RefObjKey.S_SimpleBody] = findAssist2.GetObjectFromName("n_body_silhouette");
                    dic[ChaReference.RefObjKey.S_SimpleTongue] = findAssist2.GetObjectFromName("n_tang_silhouette");
                    simpleBodyGameObject.transform.SetParent(objRef.transform);

                    //Hide objects that are not using in simplyBodyGameObject
                    HideGameObj(simpleBodyGameObject, new string[] {
                        "o_body_a",
                        "o_nip",
                        "o_tang",
                        "n_dankon",
                        "o_mnpa",
                        "o_mnpb",
                        "n_mnpb",
                        "o_gomu"
                    });

                    return;
                }
            }
        }

        private static Dictionary<string, GameObject> goList = new Dictionary<string, GameObject>();
        private static void HideGameObj(GameObject go, string[] mrNameList)
        {
            goList.Clear();
            FindAll(go.transform);
            foreach (string st in mrNameList)
            {
                if (goList.ContainsKey(st))
                {
                    //Logger.Log(LogLevel.Debug, "[KK_SSCOG] Hide GameObj Name: " + st);
                    GameObject g = null;
                    if (goList.TryGetValue(st, out g))
                    {
                        g.SetActive(false);
                    }
                    //else
                    //{
                    //    Logger.Log(LogLevel.Error, "[KK_SSCOG] g NotGet: " + st);
                    //}
                }
                //else
                //{
                //    Logger.Log(LogLevel.Debug, "[KK_SSCOG] Hide Mesh But Not found: " + st);
                //}
            }
        }
        private static void FindAll(Transform trf)
        {
            if (!goList.ContainsKey(trf.name))
            {
                goList.Remove(trf.name);
            }
            goList[trf.name] = trf.gameObject;
            for (int i = 0; i < trf.childCount; i++)
            {
                FindAll(trf.GetChild(i));
            }
        }

        //Update simple color for female's add function
        private static ChaFileStatus tempStatus = null;
        private static void UpdateStatePostfix(ChaFileStatus _status)
        {
            tempStatus = _status;
        }
        private static void AddPostFix(ref OCICharFemale __result, OICharInfo _info)
        {
            __result.charInfo.fileStatus.visibleSimple = _info.visibleSimple;
            __result.charInfo.ChangeSimpleBodyColor(_info.simpleColor);
            if (null != tempStatus)
            {
                AddObjectAssist.UpdateState(__result, tempStatus);
                Logger.Log(LogLevel.Debug, "[KK_SSCOG] Simple Color Patch Finish");
            }
            else
            {
                Logger.Log(LogLevel.Error, "[KK_SSCOG] Update State FAILD: Can't get charFileStatus!");
            }
            return;
        }
    }
}
