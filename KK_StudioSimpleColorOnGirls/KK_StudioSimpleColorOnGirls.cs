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
using BepInEx.Configuration;
using BepInEx.Logging;
using Extension;
using HarmonyLib;
using StrayTech;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KK_StudioSimpleColorOnGirls {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioSimpleColorOnGirls : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Simple Color On Girls";
        internal const string GUID = "com.jim60105.kk.studiosimplecolorongirls";
        internal const string PLUGIN_VERSION = "20.08.05.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.2.0";

        public static ConfigEntry<bool> Force_Reset_Color_Girl { get; private set; }
        public static ConfigEntry<bool> Force_Reset_Color_Boy { get; private set; }
        public static ConfigEntry<Color> Default_Color { get; private set; }
        internal static new ManualLogSource Logger;
        public void Start() {
            Logger = base.Logger;
            Extension.Extension.LogPrefix = $"[{PLUGIN_NAME}]";

            Force_Reset_Color_Girl = Config.Bind<bool>("Config", "Reset color on Girls", false);
            Force_Reset_Color_Boy = Config.Bind<bool>("Config", "Reset color on Boys", false);
            Default_Color = Config.Bind<Color>("Config", "Reset Color", new Color(1, 1, 1, 0.5f));
            if (null == typeof(ChaFileParameter).GetProperty("exType")) {
                Logger.LogError("This Plugin is not working without Darkness.");
                return;
            }

            //oo_base test
            GameObject go = null;
            try {
                go = Patches.CreateSilhouetteGameObjects();
                if (null == go || go.GetAllChildren().Count != 2) {
                    Logger.LogError("Your chara/oo_base/p_cm_body_00 is damaged or not a supported version.");
                    return;
                }
            } finally { GameObject.Destroy(go); }

            Harmony harmonyInstance = Harmony.CreateAndPatchAll(typeof(Patches), GUID);
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
    }

    class Patches {
        //private static readonly ManualLogSource Logger = KK_StudioSimpleColorOnGirls.Logger;
        #region Add Simple Color Functions to Female
        private static MPCharCtrl.StateButtonInfo colorBtn;
        private static OCIChar ociChar;

        public static void UpdateInfoPostfix(MPCharCtrl.OtherInfo __instance, OCIChar _char) {
            MPCharCtrl.StateToggleInfo tgl = (MPCharCtrl.StateToggleInfo)__instance.GetField("single");
            tgl.active = true;
            tgl.toggle.isOn = _char.GetVisibleSimple();

            MPCharCtrl.StateButtonInfo btn = (MPCharCtrl.StateButtonInfo)__instance.GetField("color");
            btn.active = true;
            btn.buttons[0].image.color = _char.oiCharInfo.simpleColor;
            colorBtn = btn;

            __instance.Invoke("SetSimpleColor", new object[] { _char.oiCharInfo.simpleColor });
            ociChar = _char;
            //Logger.LogDebug("Chara Status Info Updated");
        }

        public static void OnValueChangedSimplePostfix(object __instance, bool _value) {
            Type CommonInfoType = typeof(MPCharCtrl).GetNestedType("CommonInfo", BindingFlags.NonPublic);
            MethodInfo getProMethod = typeof(Extension.Extension).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == nameof(Extension.Extension.GetProperty) && m.IsGenericMethod).First();
            getProMethod = getProMethod.MakeGenericMethod(CommonInfoType);
            if (getProMethod.Invoke(__instance, new object[] { __instance, "isUpdateInfo" }) is bool b && b) {
                return;
            }
            ociChar.oiCharInfo.visibleSimple = _value;
            ociChar.charInfo.fileStatus.visibleSimple = _value;
            //Logger.LogDebug("Set Visible Simple:" + ociChar.oiCharInfo.visibleSimple);
        }

        public static void OnValueChangeSimpleColorPostfix(Color _color) {
            //base.ociChar.SetSimpleColor(_color);
            ociChar.charInfo.ChangeSimpleBodyColor(_color);

            //otherInfo.SetSimpleColor(_color);
            if (null != colorBtn) {
                colorBtn.buttons[0].image.color = _color;
            }
            //Logger.LogDebug("Set Simple Color:" + ociChar.oiCharInfo.simpleColor.ToString());
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AddObjectFemale), "Add", new Type[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
        public static void AddPostFix(ref OCICharFemale __result, OICharInfo _info) 
            => AddPost(__result, _info, KK_StudioSimpleColorOnGirls.Force_Reset_Color_Girl.Value);
        [HarmonyPostfix, HarmonyPatch(typeof(AddObjectMale), "Add", new Type[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
        public static void AddPostFix2(ref OCICharMale __result, OICharInfo _info)
            => AddPost(__result, _info, KK_StudioSimpleColorOnGirls.Force_Reset_Color_Boy.Value);
        private static void AddPost(OCIChar __result, OICharInfo _info, bool enableReset) {
            if (enableReset) {
                _info.simpleColor = KK_StudioSimpleColorOnGirls.Default_Color.Value;
            }
            __result.charInfo.fileStatus.visibleSimple = _info.visibleSimple;
            __result.charInfo.ChangeSimpleBodyColor(_info.simpleColor);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(OICharInfo), "Load", new Type[] { typeof(BinaryReader), typeof(Version), typeof(bool), typeof(bool) })]
        public static void LoadPostFix(OICharInfo __instance) {
            if ((__instance.sex == 0 && KK_StudioSimpleColorOnGirls.Force_Reset_Color_Boy.Value) ||
            (__instance.sex == 1 && KK_StudioSimpleColorOnGirls.Force_Reset_Color_Girl.Value)) {
                __instance.simpleColor = KK_StudioSimpleColorOnGirls.Default_Color.Value;
            }
        }
        #endregion

        //加載身體時，從male assets中加載SimpleBody的Unity GameObjects
        [HarmonyPostfix, HarmonyPatch(typeof(ChaReference), "CreateReferenceInfo")]
        public static void CreateReferenceInfoPostfix(object __instance, ulong flags, GameObject objRef) {
            if (flags >= 1UL && flags <= 15UL && (int)(flags - 1UL) == 2) {
                Dictionary<ChaReference.RefObjKey, GameObject> dictRefObj;
                dictRefObj = __instance.GetField<ChaReference>("dictRefObj").ToDictionary<ChaReference.RefObjKey, GameObject>();

                /* cf_o_root
                 * └ n_silhouetteTop    (SimpleTop)
                 *   └ n_body_silhouette
                 *   └ n_tang_silhouette */
                GameObject SimpleTop = CreateSilhouetteGameObjects();
                SimpleTop.transform.SetParent(objRef.FindChild("cf_o_root").transform);

                editDict(ChaReference.RefObjKey.S_SimpleTop, SimpleTop);
                editDict(ChaReference.RefObjKey.S_SimpleBody, SimpleTop.FindChild("n_body_silhouette"));
                editDict(ChaReference.RefObjKey.S_SimpleTongue, SimpleTop.FindChild("n_tang_silhouette"));
                void editDict(ChaReference.RefObjKey key, GameObject newGO) {
                    if (dictRefObj.TryGetValue(key, out GameObject oldGO) && null != oldGO) { GameObject.Destroy(oldGO); }
                    dictRefObj[key] = newGO;
                }

                __instance.SetField<ChaReference>("dictRefObj", dictRefObj);
            }
        }

        /// <summary>
        /// Create Simple gameobjects.
        /// </summary>
        /// <returns>False if failed</returns>
        internal static GameObject CreateSilhouetteGameObjects() {
            GameObject simpleBodyGameObject = null;
            try {
                //不能緩存後拷貝，必須每次都LoadAsset來用
                simpleBodyGameObject = CommonLib.LoadAsset<GameObject>("chara/oo_base.unity3d", "p_cm_body_00", true, Singleton<Manager.Character>.Instance.mainManifestName);
                simpleBodyGameObject.SetActive(false);
                FindAssist findAssist = new FindAssist();
                findAssist.Initialize(simpleBodyGameObject.transform);

                return findAssist.GetObjectFromName("n_silhouetteTop");
            } finally { GameObject.Destroy(simpleBodyGameObject); }
        }
    }
}
