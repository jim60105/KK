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
using BepInEx.Harmony;
using BepInEx.Logging;
using Extension;
using HarmonyLib;
using StrayTech;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KK_StudioSimpleColorOnGirls {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioSimpleColorOnGirls : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Simple Color On Girls";
        internal const string GUID = "com.jim60105.kk.studiosimplecolorongirls";
        internal const string PLUGIN_VERSION = "20.06.21.1";
        internal const string PLUGIN_RELEASE_VERSION = "1.0.7.2";

        internal static new ManualLogSource Logger;
        public void Start() {
            Logger = base.Logger;
            if (!Extension.Extension.IsDarkness()) {
                Logger.LogError("This Plugin is not working without Darkness.");
                return;
            }
            if (!OobaseCheck()) {
                return;
            }

            Harmony harmonyInstance = HarmonyWrapper.PatchAll(typeof(Patches));
            harmonyInstance.Patch(
                typeof(MPCharCtrl).GetNestedType("StateInfo", BindingFlags.NonPublic).GetMethod("OnValueChangedSimple", AccessTools.all),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.OnValueChangedSimplePostfix)));
            harmonyInstance.Patch(
                typeof(MPCharCtrl).GetNestedType("StateInfo", BindingFlags.NonPublic).GetMethod("OnValueChangeSimpleColor", AccessTools.all),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.OnValueChangeSimpleColorPostfix)));
            harmonyInstance.Patch(
                typeof(MPCharCtrl).GetNestedType("OtherInfo", BindingFlags.Public).GetMethod("UpdateInfo", AccessTools.all),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.UpdateInfoPostfix)));
        }

        /// <summary>
        /// Check whether oo_base/p_cm_body_00 can be used.
        /// </summary>
        /// <returns>True if usable</returns>
        private static bool OobaseCheck() {
            GameObject simpleBodyGameObject = null;
            try {
                simpleBodyGameObject = CommonLib.LoadAsset<GameObject>("chara/oo_base.unity3d", "p_cm_body_00", true, Singleton<Manager.Character>.Instance.mainManifestName);
                simpleBodyGameObject.SetActive(false);
                FindAssist findAssist = new FindAssist();
                findAssist.Initialize(simpleBodyGameObject.transform);

                if ((new string[] { "n_silhouetteTop", "n_body_silhouette", "n_tang_silhouette" }).Any(str => null == findAssist.GetObjectFromName(str))) {
                    throw new Exception();
                }
            } catch (Exception) {
                Logger.LogError("Your chara/oo_base/p_cm_body_00 is damaged or not a supported version.");
                return false;
            } finally {
                GameObject.Destroy(simpleBodyGameObject);
            }

            return true;
        }
    }

    class Patches {
        private static readonly ManualLogSource Logger = KK_StudioSimpleColorOnGirls.Logger;
        //Copy Simple Color Functions to Female
        private static MPCharCtrl.StateButtonInfo colorBtn;
        private static OCIChar ociChar;

        public static void UpdateInfoPostfix(MPCharCtrl.OtherInfo __instance, OCIChar _char) {
            FieldInfo[] fieldInfo = __instance.GetType().GetFields();
            foreach (FieldInfo fi in fieldInfo) {
                //Logger.LogDebug("Name: " + fi.Name);
                //Logger.LogDebug("FieldType: " + fi.FieldType);
                try {
                    if (fi.Name == "single") {
                        MPCharCtrl.StateToggleInfo o = (MPCharCtrl.StateToggleInfo)fi.GetValue(__instance);
                        o.active = true;
                        o.toggle.isOn = _char.GetVisibleSimple();
                    } else if (fi.Name == "color") {
                        MPCharCtrl.StateButtonInfo o2 = (MPCharCtrl.StateButtonInfo)fi.GetValue(__instance);
                        o2.active = true;
                        o2.buttons[0].image.color = _char.oiCharInfo.simpleColor;
                        colorBtn = o2;
                    }
                } catch (Exception e) {
                    Logger.LogError("Exception: " + e);
                    Logger.LogError("Exception: " + e.Message);
                }
            }
            MethodInfo methodInfo = __instance.GetType().GetMethod("SetSimpleColor");
            methodInfo.Invoke(__instance, new object[] { _char.oiCharInfo.simpleColor });
            ociChar = _char;
            Logger.LogDebug("Chara Status Info Updated");
        }

        public static void OnValueChangedSimplePostfix(object __instance, bool _value) {
            if ((bool)__instance.GetProperty("isUpdateInfo")) {
                return;
            }
            ociChar.oiCharInfo.visibleSimple = _value;
            ociChar.charInfo.fileStatus.visibleSimple = _value;
            Logger.LogDebug("Set Visible Simple:" + ociChar.oiCharInfo.visibleSimple);
        }

        public static void OnValueChangeSimpleColorPostfix(MPCharCtrl __instance, Color _color) {
            //base.ociChar.SetSimpleColor(_color);
            ociChar.charInfo.ChangeSimpleBodyColor(_color);
            //ChangeSimpleBodyColorPrefix(_color);  //Debug logging
            Logger.LogDebug("Set Simple Color:" + ociChar.oiCharInfo.simpleColor.ToString());

            //otherInfo.SetSimpleColor(_color);
            if (null != colorBtn) {
                colorBtn.buttons[0].image.color = _color;
            }
        }

        //When loading body, also load unity gameobjects of simply body from male asset 
        [HarmonyPostfix, HarmonyPatch(typeof(ChaReference), "CreateReferenceInfo")]
        public static void CreateReferenceInfoPostfix(ChaReference __instance, ulong flags, GameObject objRef) {
            if (flags >= 1UL && flags <= 15UL && (int)(flags - 1UL) == 2) {
                GameObject simpleBodyGameObject = CommonLib.LoadAsset<GameObject>("chara/oo_base.unity3d", "p_cm_body_00", true, Singleton<Manager.Character>.Instance.mainManifestName);
                FindAssist findAssist = new FindAssist();
                findAssist.Initialize(simpleBodyGameObject.transform);
                Dictionary<ChaReference.RefObjKey, GameObject> dicRefObj = __instance.GetField("dictRefObj").ToDictionary<ChaReference.RefObjKey, GameObject>();

                /* cf_o_root
                 * └ n_silhouetteTop
                 *   └ n_body_silhouette
                 *   └ n_tang_silhouette
                 */
                GameObject SimpleTop = findAssist.GetObjectFromName("n_silhouetteTop");
                GameObject SimpleBody = findAssist.GetObjectFromName("n_body_silhouette");
                GameObject SimpleTang = findAssist.GetObjectFromName("n_tang_silhouette");

                doMain(dicRefObj, ChaReference.RefObjKey.S_SimpleTop, SimpleTop, objRef.FindChild("cf_o_root"));
                doMain(dicRefObj, ChaReference.RefObjKey.S_SimpleBody, SimpleBody, SimpleTop);
                doMain(dicRefObj, ChaReference.RefObjKey.S_SimpleTongue, SimpleTang, SimpleTop);
                __instance.SetField("dictRefObj", dicRefObj);

                GameObject.Destroy(simpleBodyGameObject);
            }
            return;

            void doMain(Dictionary<ChaReference.RefObjKey, GameObject> dic, ChaReference.RefObjKey key, GameObject newGameObject, GameObject newParent) {
                if (null == newGameObject || null == newParent) return;
                if (dic.TryGetValue(key, out GameObject go) && null != go) { GameObject.Destroy(go); }
                newGameObject.transform.SetParent(newParent.transform);
                dic[key] = newGameObject;
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
                //Logger.LogDebug("Simple Color Patch Finish");
            } else {
                Logger.LogError("Update State FAILD: Can't get charFileStatus!");
            }
            return;
        }
    }
}
