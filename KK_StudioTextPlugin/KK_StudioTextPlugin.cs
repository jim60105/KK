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
using BepInEx;
using BepInEx.Logging;
using Extension;
using Harmony;
using Studio;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace KK_StudioTextPlugin {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioTextPlugin : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Text Plugin";
        internal const string GUID = "com.jim60105.kk.studiotextplugin";
        internal const string PLUGIN_VERSION = "19.06.28.0";

        public void Awake() {
            HarmonyInstance.Create(GUID).PatchAll(typeof(Patches));
            UIUtility.Init();
            Patches.Start();
        }
    }

    class Patches {
        private static Material font3DMaterial;
        private static bool creatingTextFolder = false;
        private static bool creatingTextStructure = false;
        private static readonly string displayPrefix = "-Text Plugin:";
        private static bool isConfigPanelCreated = false;
        public static void Start() {
            if (AssetBundle.LoadFromMemory(Properties.Resources.text) is AssetBundle assetBundle) {
                font3DMaterial = assetBundle.LoadAsset<Material>("Font3DMaterial");
                font3DMaterial.color = Color.white;
                assetBundle.Unload(false);
            } else {
                Logger.Log(LogLevel.Error, "[KK_STP] Load assetBundle faild");
            }
        }

        private static Image panel;
        private static void DrawConfigPanel() {
            if (isConfigPanelCreated) {
                return;
            }

            //畫Config視窗
            var panelRoot = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/04_Folder");
            //Draw Panel and ButtonAll
            panel = UIUtility.CreatePanel("ConfigPanel", panelRoot.transform);
            //Config1: Font
            var text1 = UIUtility.CreateText("FontText", panel.transform, "Font");
            text1.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -30), new Vector2(155f, -5f));
            var d = UIUtility.CreateDropdown("fontDropdown", panel.transform);
            d.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -85f), new Vector2(155f, -35f));
            d.GetComponentInChildren<Text>().resizeTextMaxSize = 16;
            //Font List
            d.options.Clear();
            var fontList = Font.GetOSInstalledFontNames();
            d.AddOptions(fontList.ToList());
            //Change Font
            d.onValueChanged.AddListener(delegate {
                OCIFolder[] folderArray = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                                           select Studio.Studio.GetCtrlInfo(v) as OCIFolder into v
                                           where v != null
                                           select v).ToArray();
                foreach (var oCIFolder in folderArray) {
                    SetFont(oCIFolder.objectItem, fontList[d.value]);
                    MakeAndSetConfigStructure(oCIFolder.treeNodeObject, Config.Font);
                }
            });

            panel.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -300), new Vector2(160, -70));
            foreach (var image in panel.GetComponentsInChildren<Image>()) {
                image.color = new Color32(120, 120, 120, 220);
            }
            foreach (var text in panel.GetComponentsInChildren<Text>()) {
                text.color = Color.white;
            }

            //拖曳event
            Vector2 mouse = Vector2.zero;
            EventTrigger trigger = panel.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            entry.callback.AddListener((data) => {
                mouse = new Vector2(Input.mousePosition.x - panel.transform.position.x, Input.mousePosition.y - panel.transform.position.y);
            });
            EventTrigger.Entry entry2 = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            entry2.callback.AddListener((data) => {
                panel.transform.position = new Vector3(Input.mousePosition.x - mouse.x, Input.mousePosition.y - mouse.y, 0);
            });
            trigger.triggers.Add(entry);
            trigger.triggers.Add(entry2);
            Logger.Log(LogLevel.Info, "[KK_STP] Draw ConfigPanel Finish");
            isConfigPanelCreated = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(TreeNodeObject), "OnClickSelect")]
        public static void OnClickSelectPostfix(TreeNodeObject __instance) {
            if (__instance.name.IndexOf(displayPrefix) >= 0) {
                panel.gameObject.SetActive(true);
                MakeAndSetConfigStructure(__instance);

                OCIFolder textOCIFolder = Studio.Studio.GetCtrlInfo(__instance) as OCIFolder;
                TextMesh t = textOCIFolder.objectItem.GetComponent<TextMesh>();
                MeshRenderer m = textOCIFolder.objectItem.GetComponent<MeshRenderer>();
                var i = Array.IndexOf(Font.GetOSInstalledFontNames(), t.font.name);
                if (i >= 0) {
                    panel.GetComponentInChildren<Dropdown>().value = i;
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(TreeNodeObject), "OnDeselect")]
        public static void OnDeselectPostfix(TreeNodeObject __instance) {
            panel.gameObject.SetActive(false);
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
                component.text = "文字Text";
                component.AddActionToButton(delegate {
                    TreeNodeCtrl treeNodeCtrl = Singleton<Studio.Studio>.Instance.treeNodeCtrl;
                    //創建文字
                    creatingTextFolder = true;
                    creatingTextStructure = true;
                    OCIFolder textOCIFolder = AddObjectFolder.Add();
                    Singleton<UndoRedoManager>.Instance.Clear();
                    if (Studio.Studio.optionSystem.autoSelect && textOCIFolder != null) {
                        treeNodeCtrl.SelectSingle(textOCIFolder.treeNodeObject);
                    }
                    TextMesh t = textOCIFolder.objectItem.GetComponent<TextMesh>();
                    t.transform.localRotation *= Quaternion.Euler(0f, 180f, 0f);
                });
                ((Dictionary<int, Image>)__instance.GetField("dicNode")).Add(60105, gameObject.GetComponent<Image>());
            }
        }

        public enum Config {
            All = 0,
            Font,
            FontSize,
            Color
        }
        public static void MakeAndSetConfigStructure(TreeNodeObject textTreeNodeObject, Config config = Config.All) {
            OCIFolder textOCIFolder = Studio.Studio.GetCtrlInfo(textTreeNodeObject) as OCIFolder;
            TextMesh t = textOCIFolder.objectItem.GetComponent<TextMesh>();
            MeshRenderer m = textOCIFolder.objectItem.GetComponent<MeshRenderer>();
            TreeNodeCtrl treeNodeCtrl = Singleton<Studio.Studio>.Instance.treeNodeCtrl;

            TreeNodeObject nConfig = doMain("-Text Config", "", textTreeNodeObject);
            if (config == Config.Font || config == Config.All)
                doMain("-Text Font:", t.font.name, nConfig);
            if (config == Config.FontSize || config == Config.All)
                doMain("-Text FontSize:", t.characterSize.ToString(), nConfig);
            if (config == Config.Color || config == Config.All)
                doMain("-Text Color:", m.material.color.ToString(), nConfig);

            TreeNodeObject doMain(string prefix, string value, TreeNodeObject nRoot) {
                TreeNodeObject node = nRoot.child?.Where((x) => {
                    return (Studio.Studio.GetCtrlInfo(x) is OCIFolder y) && y.name.IndexOf(prefix) >= 0;
                }).FirstOrDefault();
                OCIFolder folder;
                if (null == node) {
                    folder = AddObjectFolder.Add();
                    treeNodeCtrl.SetParent(folder.treeNodeObject, nRoot);
                    folder.objectInfo.changeAmount.Reset();
                    node = folder.treeNodeObject;

                } else {
                    folder = Studio.Studio.GetCtrlInfo(node) as OCIFolder;
                }
                folder.name = folder.objectItem.name = node.name = prefix + value;
                return node;
            }
        }

        public static string GetConfig(TreeNodeObject textTreeNodeObject, Config config) {
            OCIFolder textOCIFolder = Studio.Studio.GetCtrlInfo(textTreeNodeObject) as OCIFolder;
            TextMesh t = textOCIFolder.objectItem.GetComponent<TextMesh>();
            MeshRenderer m = textOCIFolder.objectItem.GetComponent<MeshRenderer>();
            TreeNodeObject GetChildNode(string prefix, TreeNodeObject nRoot) {
                return nRoot.child?.Where((x) => {
                    return (Studio.Studio.GetCtrlInfo(x) is OCIFolder y) && y.name.IndexOf(prefix) >= 0;
                }).FirstOrDefault();
            }

            TreeNodeObject nConfig = GetChildNode("-Text Config", textTreeNodeObject);
            string GetValue(string prefix) {
                OCIFolder f = Studio.Studio.GetCtrlInfo(GetChildNode(prefix, nConfig)) as OCIFolder;
                return f.name.Replace(prefix,"");
            }

            switch (config) {
                case Config.Font:
                    return GetValue("-Text Font:");
                case Config.FontSize:
                    return GetValue("-Text FontSize:");
                case Config.Color:
                    return GetValue("-Text Color:");
                default:
                    return "";
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AddObjectFolder), "Load", new Type[] { typeof(OIFolderInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
        public static void LoadPostfix(ref OCIFolder __result, OIFolderInfo _info, ObjectCtrlInfo _parent, TreeNodeObject _parentNode, bool _addInfo, int _initialPosition) {
            __result.treeNodeObject.onVisible = (TreeNodeObject.OnVisibleFunc)Delegate.Combine(__result.treeNodeObject.onVisible, new TreeNodeObject.OnVisibleFunc(__result.OnVisible));
            if (creatingTextFolder || __result.name.IndexOf(displayPrefix) >= 0) {
                __result.name = __result.treeNodeObject.name = creatingTextFolder ? displayPrefix + "New Text" : _info.name;
                var t = MakeTextObj(__result, creatingTextFolder ? "New Text" : _info.name.Replace(displayPrefix, ""));
                creatingTextFolder = false;
                if (_addInfo) {
                    MakeAndSetConfigStructure(__result.treeNodeObject);
                }
                creatingTextStructure = false;
                _info.changeAmount.OnChange();
                Logger.Log(LogLevel.Debug, $"[KK_STP] Pos:{_info.changeAmount.pos.ToString()}");
                Logger.Log(LogLevel.Debug, $"[KK_STP] Rot:{_info.changeAmount.rot.ToString()}");
                Logger.Log(LogLevel.Debug, $"[KK_STP] Scale:{_info.changeAmount.scale.ToString()}");

                Logger.Log(LogLevel.Info, "[KK_STP] Load Text:" + t.text);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(OCIFolder), "OnVisible")]
        public static void OnVisiblePostfix(bool _visible,OCIFolder __instance) {
            __instance.objectItem.SetActive(_visible);
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

        public static TextMesh MakeTextObj(OCIFolder folder, string text) {
            folder.objectItem.layer = 10;
            TextMesh t = folder.objectItem.AddComponent<TextMesh>();
            t.fontSize = 200;
            t.anchor = TextAnchor.MiddleCenter;
            t.characterSize = 0.01f;
            t.text = text;
            SetFont(folder.objectItem);
            DrawConfigPanel();

            Logger.Log(LogLevel.Info, "[KK_STP] Create Text");
            return t;
        }

        public static bool CheckFontInOS(string fontName) {
            var i = Array.IndexOf(Font.GetOSInstalledFontNames(), fontName);
            if (i >= 0) {
                return true;
            }
            Logger.Log(LogLevel.Message, "[KK_STP] Missing font: " + fontName);
            return false;
        }

        public static void SetFont(GameObject textGO, string fontName = "MS Gothic") {
            if (!CheckFontInOS(fontName)) {
                if (!String.Equals(fontName, "MS Gothic")) {
                    Logger.Log(LogLevel.Message, "[KK_STP] Fallback to MS Gothic");
                    fontName = "MS Gothic";
                } else {
                    Logger.Log(LogLevel.Message, "[KK_STP] Fallback to Arial");
                    fontName = "Arial";
                }
            }
            textGO.GetComponent<TextMesh>().font = Font.CreateDynamicFontFromOSFont(fontName, 200);
            textGO.GetComponent<MeshRenderer>().material = font3DMaterial;
            textGO.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", textGO.GetComponent<TextMesh>().font.material.mainTexture);
            textGO.GetComponent<MeshRenderer>().material.EnableKeyword("_NORMALMAP");
            return;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(TreeNodeCtrl), "RefreshHierachyLoop")]
        public static void RefreshHierachyLoopPostfix(TreeNodeObject _source, int _indent, bool _visible) {
            ObjectCtrlInfo objectCtrlInfo = Studio.Studio.GetCtrlInfo(_source);
            if (objectCtrlInfo.objectInfo.kind == 3 &&
                    objectCtrlInfo is OCIFolder oCIFolder &&
                    oCIFolder.name.IndexOf(displayPrefix) >= 0 &&
                    !creatingTextStructure
                ) {
                SetFont(oCIFolder.objectItem, GetConfig(oCIFolder.treeNodeObject, Config.Font));
                MakeAndSetConfigStructure(oCIFolder.treeNodeObject, Config.Font);
                oCIFolder.objectItem.SetActive(_source.visible);
            }
        }
    }
}
