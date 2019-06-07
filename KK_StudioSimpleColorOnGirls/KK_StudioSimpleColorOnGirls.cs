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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Extension;
using Harmony;
using Manager;
using Studio;
using UniRx;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_StudioSimpleColorOnGirls {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioSimpleColorOnGirls : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Simple Color On Girls";
        internal const string GUID = "com.jim60105.kk.studiosimplecolorongirls";
        internal const string PLUGIN_VERSION = "19.06.07.0";

        public void Awake() {
            HarmonyInstance harmonyInstance = HarmonyInstance.Create(GUID);
            harmonyInstance.PatchAll(typeof(Patches));
            harmonyInstance.Patch(typeof(MPCharCtrl).GetNestedType("StateInfo", BindingFlags.NonPublic).GetMethod("OnValueChangedSimple", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(Patches.OnValueChangedSimplePostfix), null), null);
            harmonyInstance.Patch(typeof(MPCharCtrl).GetNestedType("StateInfo", BindingFlags.NonPublic).GetMethod("OnValueChangeSimpleColor", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(Patches.OnValueChangeSimpleColorPostfix), null), null);
            harmonyInstance.Patch(typeof(MPCharCtrl).GetNestedType("OtherInfo", BindingFlags.Public).GetMethod("UpdateInfo", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(Patches.UpdateInfoPostfix), null), null);

            //Workaround for EC Yoyaku Build
            if (null == typeof(ChaFileParameter).GetProperty("exType")) {
                Logger.Log(LogLevel.Message, "[KK_StudioSimpleColorOnGirls] This Plugin is not working without EC yoyaku tokuten, which released at 2019/04/26.");
                Logger.Log(LogLevel.Message, "[KK_StudioSimpleColorOnGirls] Please use v1.0.1 of this plugin.");
                return;
            }
        }
    }
    class Patches {
        //Copy Simple Color Functions to Female
        private static MPCharCtrl.StateButtonInfo colorBtn;
        private static OCIChar ociChar;

        public static void UpdateInfoPostfix(MPCharCtrl.OtherInfo __instance, OCIChar _char) {
            FieldInfo[] fieldInfo = __instance.GetType().GetFields();
            foreach (var fi in fieldInfo) {
                //Logger.Log(LogLevel.Debug, "[KK_SSCOG] Name: " + fi.Name);
                //Logger.Log(LogLevel.Debug, "[KK_SSCOG] FieldType: " + fi.FieldType);
                try {
                    if (fi.Name == "single") {
                        var o = (MPCharCtrl.StateToggleInfo)fi.GetValue(__instance);
                        o.active = true;
                        o.toggle.isOn = _char.GetVisibleSimple();
                    } else if (fi.Name == "color") {
                        var o2 = (MPCharCtrl.StateButtonInfo)fi.GetValue(__instance);
                        o2.active = true;
                        o2.buttons[0].image.color = _char.oiCharInfo.simpleColor;
                        colorBtn = o2;
                    }
                } catch (Exception e) {
                    Logger.Log(LogLevel.Error, "[KK_SSCOG] Exception: " + e);
                    Logger.Log(LogLevel.Error, "[KK_SSCOG] Exception: " + e.Message);
                }
            }
            MethodInfo methodInfo = __instance.GetType().GetMethod("SetSimpleColor");
            methodInfo.Invoke(__instance, new object[] { _char.oiCharInfo.simpleColor });
            ociChar = _char;
            Logger.Log(LogLevel.Debug, "[KK_SSCOG] Chara Status Info Updated");
        }

        public static void OnValueChangedSimplePostfix(object __instance, bool _value) {
            if ((bool)__instance.GetProperty("isUpdateInfo")) {
                return;
            }
            ociChar.oiCharInfo.visibleSimple = _value;
            ociChar.charInfo.fileStatus.visibleSimple = _value;
            Logger.Log(LogLevel.Debug, "[KK_SSCOG] Set Visible Simple:" + ociChar.oiCharInfo.visibleSimple);
        }

        public static void OnValueChangeSimpleColorPostfix(MPCharCtrl __instance, Color _color) {
            //base.ociChar.SetSimpleColor(_color);
            ociChar.charInfo.ChangeSimpleBodyColor(_color);
            //ChangeSimpleBodyColorPrefix(_color);  //Debug logging
            Logger.Log(LogLevel.Debug, "[KK_SSCOG] Set Simple Color:" + ociChar.oiCharInfo.simpleColor.ToString());

            //this.otherInfo.SetSimpleColor(_color);
            if (null != colorBtn) {
                colorBtn.buttons[0].image.color = _color;
            }
        }

        ////For Debug Only
        //[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "ChangeSimpleBodyColor")]
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
        //[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "ChangeSimpleBodyDraw")]
        //public static void ChangeSimpleBodyDrawPostfix(bool drawSimple)
        //{
        //    Logger.Log(LogLevel.Debug, "[KK_SSCOG] Change Simple visible:" + drawSimple);
        //}
        //public static void ChangeSimpleBodyColorPostfix(Color color)
        //{
        //    Logger.Log(LogLevel.Debug, "[KK_SSCOG] Change Simple Color:" + color);
        //}


        //When loading body, also load unity gameobjects of simply body from male asset 
        [HarmonyPostfix, HarmonyPatch(typeof(ChaReference), "CreateReferenceInfo")]
        public static void CreateReferenceInfoPostfix(ChaReference __instance, ulong flags, GameObject objRef) {
            if (flags >= 1UL && flags <= 15UL && (int)(flags - 1UL) == 2) {
                GameObject simpleBodyGameObject = CommonLib.LoadAsset<GameObject>("chara/oo_base.unity3d", "p_cm_body_00", true, Singleton<Character>.Instance.mainManifestName);
                FindAssist findAssist2 = new FindAssist();
                findAssist2.Initialize(simpleBodyGameObject.transform);
                if (
                    typeof(ChaReference).GetFields(AccessTools.all).Where(x => x.Name == "dictRefObj")
                    .FirstOrDefault().GetValue(__instance)
                    is Dictionary<ChaReference.RefObjKey, GameObject> dic
                    ) {
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
        private static void HideGameObj(GameObject go, string[] mrNameList) {
            goList.Clear();
            FindAll(go.transform);
            foreach (string st in mrNameList) {
                if (goList.ContainsKey(st)) {
                    //Logger.Log(LogLevel.Debug, "[KK_SSCOG] Hide GameObj Name: " + st);
                    GameObject g = null;
                    if (goList.TryGetValue(st, out g)) {
                        g.SetActive(false);
                    }
                }
            }
        }
        private static void FindAll(Transform trf) {
            if (!goList.ContainsKey(trf.name)) {
                goList.Remove(trf.name);
            }
            goList[trf.name] = trf.gameObject;
            for (int i = 0; i < trf.childCount; i++) {
                FindAll(trf.GetChild(i));
            }
        }

        //Update simple color for female's add function
        private static ChaFileStatus tempStatus = null;
        [HarmonyPostfix, HarmonyPatch(typeof(AddObjectAssist), "UpdateState")]
        private static void UpdateStatePostfix(ChaFileStatus _status) {
            tempStatus = _status;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AddObjectFemale), "Add", new Type[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
        private static void AddPostFix(ref OCICharFemale __result, OICharInfo _info) {
            __result.charInfo.fileStatus.visibleSimple = _info.visibleSimple;
            __result.charInfo.ChangeSimpleBodyColor(_info.simpleColor);
            if (null != tempStatus) {
                AddObjectAssist.UpdateState(__result, tempStatus);
                Logger.Log(LogLevel.Debug, "[KK_SSCOG] Simple Color Patch Finish");
            } else {
                Logger.Log(LogLevel.Error, "[KK_SSCOG] Update State FAILD: Can't get charFileStatus!");
            }
            return;
        }
    }
}
