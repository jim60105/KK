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
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Extension;
using Harmony;
using Studio;
using UnityEngine;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace KK_StudioTextPlugin {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioTextPlugin : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Text Plugin";
        internal const string GUID = "com.jim60105.kk.studiotextplugin";
        internal const string PLUGIN_VERSION = "19.06.26.0";

        public void Awake() {
            HarmonyInstance.Create(GUID).PatchAll(typeof(Patches));
            Patches.Start();
        }
    }

    class Patches {
        private static Material font3DMaterial;
        private static bool creatingTextObj = false;
        private static readonly string displayPrefix = "-Text Plugin:";
        public static void Start() {
            byte[] ba;
            using (Stream resFilestream = Assembly.GetExecutingAssembly().GetManifestResourceStream("KK_StudioTextPlugin.Resources.text.assetbundle")) {
                ba = new byte[resFilestream.Length];
                resFilestream.Read(ba, 0, ba.Length);
            }
            if (AssetBundle.LoadFromMemory(ba) is AssetBundle assetBundle) {
                font3DMaterial = assetBundle.LoadAsset<Material>("Font3DMaterial");
                font3DMaterial.color = Color.white;
            } else {
                Logger.Log(LogLevel.Error, "[KK_STP] Load assetBundle faild");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ItemCategoryList), "InitList")]
        public static void InitListPostfix(int _group, ItemCategoryList __instance) {
            if (_group == 9) {
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>((GameObject)__instance.GetField("objectPrefab"));
                if (!gameObject.activeSelf) {
                    gameObject.SetActive(true);
                }
                gameObject.transform.SetParent((Transform)__instance.GetField("transformRoot"), false);
                ListNode component = gameObject.GetComponent<ListNode>();
                component.AddActionToButton(delegate {
                    //創建文字
                    creatingTextObj = true;
                    Singleton<Studio.Studio>.Instance.AddFolder();
                    creatingTextObj = false;
                });
                component.text = "文字Text";
                ((Dictionary<int, Image>)__instance.GetField("dicNode")).Add(60105, gameObject.GetComponent<Image>());
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AddObjectFolder), "Load", new Type[] { typeof(OIFolderInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
        public static void LoadPostfix(ref OCIFolder __result, OIFolderInfo _info) {
            if (creatingTextObj || __result.name.IndexOf(displayPrefix) >= 0) {
                __result.name = creatingTextObj ? displayPrefix+"New Text" : _info.name;
                MakeTextObj(__result.objectItem, creatingTextObj ? "New Text" : _info.name.Replace(displayPrefix, ""));
                _info.changeAmount.OnChange();
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MPFolderCtrl), "OnEndEditName")]
        public static bool OnEditNamePrefix(MPFolderCtrl __instance, string _value) {
            if (__instance.ociFolder.objectItem.GetComponents<TextMesh>().Length != 0) {
                __instance.ociFolder.name = displayPrefix + _value;
                __instance.ociFolder.objectItem.GetComponent<TextMesh>().text = _value;
                Logger.Log(LogLevel.Info, "[KK_STP] Edit Text: " + _value);
                return false;
            }
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MPFolderCtrl), "UpdateInfo")]
        public static void UpdateInfoPostfix(MPFolderCtrl __instance) {
            if (__instance.ociFolder == null) {
                return;
            }
            __instance.SetField("isUpdateInfo", true);
            InputField input = (InputField)__instance.GetField("inputName");
            input.text = __instance.ociFolder.name.Replace(displayPrefix, "");
            __instance.SetField("inputName", input);
            __instance.SetField("isUpdateInfo", false);
        }

        public static void MakeTextObj(GameObject parentGO, string text) {
            parentGO.layer = 10;
            TextMesh t = parentGO.AddComponent<TextMesh>();
            t.fontSize = 200;
            t.anchor = TextAnchor.MiddleCenter;
            t.characterSize = 0.01f;
            t.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            t.font = Font.CreateDynamicFontFromOSFont("MS Gothic", 200);
            parentGO.GetComponentInChildren<MeshRenderer>().material = font3DMaterial;
            parentGO.GetComponentInChildren<MeshRenderer>().material.SetTexture("_MainTex", t.font.material.mainTexture);
            parentGO.GetComponentInChildren<MeshRenderer>().material.EnableKeyword("_NORMALMAP");
            t.text = text;
            Logger.Log(LogLevel.Info, "[KK_STP] Create Text");
        }
    }
}
