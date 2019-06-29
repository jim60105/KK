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
        internal const string PLUGIN_VERSION = "19.06.29.3";

        public void Awake() {
            HarmonyInstance.Create(GUID).PatchAll(typeof(Patches));
            UIUtility.Init();
            Patches.Start();
        }
    }

    static class Patches {
        private static bool isCreatingTextFolder = false;
        internal static bool isCreatingTextStructure = false;
        private static bool isConfigPanelCreated = false;
        private static bool onUpdating = false;
        public static readonly string TextObjPrefix = "-Text Plugin:";
        public static readonly string TextConfigPrefix = "-Text Config";
        public static readonly string TextConfigFontPrefix = "-Text Font:";
        public static readonly string TextConfigFontSizePrefix = "-Text FontSize:";
        public static readonly string TextConfigFontStylePrefix = "-Text FontStyle:";
        public static readonly string TextConfigColorPrefix = "-Text Color:";
        internal static void Start() => TextPlugin.Start();

        internal static Image panel;
        internal static void DrawConfigPanel() {
            if (isConfigPanelCreated) {
                return;
            }

            //畫Config視窗
            var panelRoot = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/04_Folder");
            panel = UIUtility.CreatePanel("ConfigPanel", panelRoot.transform);
            //Config1: Font
            var text1 = UIUtility.CreateText("FontText", panel.transform, "Font");
            text1.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -30f), new Vector2(155f, -5f));
            var d = UIUtility.CreateDropdown("fontDropdown", panel.transform);
            d.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -85f), new Vector2(155f, -35f));
            d.GetComponentInChildren<Text>().resizeTextMaxSize = 18;
            //Font List
            d.options.Clear();
            var fontList = Font.GetOSInstalledFontNames();
            d.AddOptions(fontList.ToList());
            d.value = Array.IndexOf(fontList, "MS Gothic");
            //Change Font
            d.onValueChanged.AddListener(delegate {
                if (!onUpdating) {
                    TextPlugin.ChangeFont(fontList[d.value]);
                }
            });

            //Config2: FontSize (CharacterSize)
            var text2 = UIUtility.CreateText("FontSize", panel.transform, "FontSize");
            text2.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -115f), new Vector2(155f, -90f));
            var input = UIUtility.CreateInputField("fontSizeInput", panel.transform);
            input.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -145f), new Vector2(155f, -120f));
            input.text = "1";
            input.onEndEdit.AddListener(delegate {
                if (!onUpdating) {
                    if(!float.TryParse(input.text,out float f)){
                        Logger.Log(LogLevel.Error, "[KK_STP] FormatException: Please input only numbers into FontSize.");
                        Logger.Log(LogLevel.Message, "[KK_STP] FormatException: Please input only numbers into FontSize.");
                        input.text = TextPlugin.GetConfig(null,TextPlugin.Config.FontSize);
                    } else {
                        TextPlugin.ChangeCharacterSize(f);
                    }
                }
            });

            //Config3: FontStyle
            var text3 = UIUtility.CreateText("FontStyle", panel.transform, "FontStyle");
            text3.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -175f), new Vector2(155f, -150f));
            var d2 = UIUtility.CreateDropdown("fontStyleDropdown", panel.transform);
            d2.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -215f), new Vector2(155f, -180f));
            //Font List
            d2.options.Clear();
            var fontStyle = Enum.GetNames(typeof(FontStyle));
            d2.AddOptions(fontStyle.ToList());
            d2.value = 0;
            //Change FontStyle
            d2.onValueChanged.AddListener(delegate {
                if (!onUpdating) {
                    TextPlugin.ChangeFontStyle(fontStyle[d2.value]);
                }
            });

            //Config4: Color
            var text4 = UIUtility.CreateText("Color", panel.transform, "Color");
            text4.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -245f), new Vector2(155f, -220f));
            var btn = UIUtility.CreateButton("color", panel.transform, "");
            btn.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -275f), new Vector2(155f, -250f));
            btn.onClick.AddListener(delegate {
                if (!onUpdating) {
                    if (!ColorUtility.TryParseHtmlString(TextPlugin.GetConfig(null, TextPlugin.Config.Color), out var color)) {
                        color = Color.white;
                    }
                    Singleton<Studio.Studio>.Instance.colorPalette.Setup("字體顏色", color, new Action<Color>(TextPlugin.ChangeColor), true);
                    Singleton<Studio.Studio>.Instance.colorPalette.visible = true;
                }
            });

            panel.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -350), new Vector2(160, -70));
            foreach (var image in panel.GetComponentsInChildren<Image>()) {
                image.color = new Color32(120, 120, 120, 220);
            }
            foreach (var text in panel.GetComponentsInChildren<Text>()) {
                text.color = Color.white;
            }
            btn.image.color = Color.white;

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

            panel.gameObject.SetActive(true);
            Logger.Log(LogLevel.Info, "[KK_STP] Draw ConfigPanel Finish");
            isConfigPanelCreated = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(TreeNodeObject), "OnClickSelect")]
        public static void OnClickSelectPostfix(TreeNodeObject __instance) {
            ObjectCtrlInfo objectCtrlInfo = Studio.Studio.GetCtrlInfo(__instance);
            if (objectCtrlInfo.objectInfo.kind == 3 && objectCtrlInfo is OCIFolder oCIFolder ) {
                if (oCIFolder.name.Contains(TextConfigPrefix)) {
                    TreeNodeCtrl treeNodeCtrl = Singleton<Studio.Studio>.Instance.treeNodeCtrl;
                    treeNodeCtrl.SelectSingle(__instance.parent);
                    oCIFolder = Studio.Studio.GetCtrlInfo(__instance.parent) as OCIFolder;
                } else if (
                        oCIFolder.name.Contains(TextConfigFontPrefix) ||
                        oCIFolder.name.Contains(TextConfigFontSizePrefix) ||
                        oCIFolder.name.Contains(TextConfigFontStylePrefix) ||
                        oCIFolder.name.Contains(TextConfigColorPrefix)
                    ) {
                    TreeNodeCtrl treeNodeCtrl = Singleton<Studio.Studio>.Instance.treeNodeCtrl;
                    treeNodeCtrl.SelectSingle(__instance.parent.parent);
                    oCIFolder = Studio.Studio.GetCtrlInfo(__instance.parent.parent) as OCIFolder;
                }
                if (oCIFolder.name.Contains(TextObjPrefix)) {
                    TextMesh t = oCIFolder.objectItem.GetComponentInChildren<TextMesh>();
                    if (null == t) {
                        TextPlugin.MakeTextObj(oCIFolder, oCIFolder.name.Replace(TextObjPrefix,""));
                        TextPlugin.MakeAndSetConfigStructure(oCIFolder.treeNodeObject);
                        t = oCIFolder.objectItem.GetComponentInChildren<TextMesh>();
                    }
                    MeshRenderer m = oCIFolder.objectItem.GetComponentInChildren<MeshRenderer>();
                    onUpdating = true;
                    panel.gameObject.SetActive(true);

                    //加載編輯選單內容
                    //Font
                    if (TextPlugin.CheckFontInOS(t.font.name)) {
                        panel.GetComponentsInChildren<Dropdown>()[0].value = Array.IndexOf(Font.GetOSInstalledFontNames(), t.font.name);
                    }

                    //FontSize
                    panel.GetComponentInChildren<InputField>().text = (t.characterSize * 500).ToString();

                    //FontStyle
                    panel.GetComponentsInChildren<Dropdown>()[1].value = Array.IndexOf(Enum.GetNames(typeof(FontStyle)), t.fontStyle.ToString());

                    //Color
                    panel.GetComponentInChildren<Button>().image.color = m.material.color;

                    onUpdating = false;
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(TreeNodeObject), "OnDeselect")]
        public static void OnDeselectPostfix(TreeNodeObject __instance) {
            //TextObj失焦
            panel?.gameObject?.SetActive(false);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ItemCategoryList), "InitList")]
        public static void InitListPostfix(int _group, ItemCategoryList __instance) {
            //創建項目至"Add/物品/2D效果"清單內
            if (_group == 9) {  //2D效果
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>((GameObject)__instance.GetField("objectPrefab"));
                if (!gameObject.activeSelf) {
                    gameObject.SetActive(true);
                }
                gameObject.transform.SetParent((Transform)__instance.GetField("transformRoot"), false);
                ListNode component = gameObject.GetComponent<ListNode>();
                component.text = "文字Text";
                component.GetComponentInChildren<Text>().color = Color.yellow;
                component.AddActionToButton(delegate {
                    TreeNodeCtrl treeNodeCtrl = Singleton<Studio.Studio>.Instance.treeNodeCtrl;
                    isCreatingTextFolder = true;
                    OCIFolder textOCIFolder = AddObjectFolder.Add();
                    Singleton<UndoRedoManager>.Instance.Clear();
                    if (Studio.Studio.optionSystem.autoSelect && textOCIFolder != null) {
                        treeNodeCtrl.SelectSingle(textOCIFolder.treeNodeObject);
                    }
                });
                //目前最大的漢字編碼是臺灣的國家標準CNS11643，目前（4.0）共收錄可考證之正簡、日、韓語漢字共76,067個
                ((Dictionary<int, Image>)__instance.GetField("dicNode")).Add(76067, gameObject.GetComponent<Image>());
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AddObjectFolder), "Load", new Type[] { typeof(OIFolderInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
        public static void LoadPostfix(ref OCIFolder __result, OIFolderInfo _info, ObjectCtrlInfo _parent, TreeNodeObject _parentNode, bool _addInfo, int _initialPosition) {
            //Scene讀取的進入點
            //處理Folder未定義OnVisible造成的不隱藏
            __result.treeNodeObject.onVisible = (TreeNodeObject.OnVisibleFunc)Delegate.Combine(__result.treeNodeObject.onVisible, new TreeNodeObject.OnVisibleFunc(__result.OnVisible));

            if (isCreatingTextFolder || __result.name.Contains(TextObjPrefix)) {
                __result.name = isCreatingTextFolder ? TextObjPrefix + "New Text" : _info.name;
                var t = TextPlugin.MakeTextObj(__result, isCreatingTextFolder ? "New Text" : _info.name.Replace(TextObjPrefix, ""));
                isCreatingTextFolder = false;
                if (_addInfo) {
                    //Scene Load就不創建Config資料夾結構
                    TextPlugin.MakeAndSetConfigStructure(__result.treeNodeObject);
                }
                //套用座標、旋轉、縮放
                _info.changeAmount.OnChange();
                Logger.Log(LogLevel.Debug, $"[KK_STP] Pos:{_info.changeAmount.pos.ToString()}");
                Logger.Log(LogLevel.Debug, $"[KK_STP] Rot:{_info.changeAmount.rot.ToString()}");
                Logger.Log(LogLevel.Debug, $"[KK_STP] Scale:{_info.changeAmount.scale.ToString()}");

                Logger.Log(LogLevel.Info, "[KK_STP] Load Text:" + t.text);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(OCIFolder), "OnVisible")]
        public static void OnVisiblePostfix(bool _visible, OCIFolder __instance) {
            __instance.objectItem.SetActive(_visible);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MPFolderCtrl), "OnEndEditName")]
        public static bool OnEditNamePrefix(MPFolderCtrl __instance, ref string _value) {
            if (_value.Contains(TextObjPrefix)) {
                _value = _value.Replace(TextObjPrefix, "");
                __instance.ociFolder.name = __instance.ociFolder.objectItem.name = TextObjPrefix + _value;
                OnClickSelectPostfix(__instance.ociFolder.treeNodeObject);
                UpdateInfoPostfix(__instance);
            }
            if (__instance.ociFolder.name.Contains(TextObjPrefix)) {
                //對資料夾名稱做編輯，加上prefix
                __instance.ociFolder.name = __instance.ociFolder.objectItem.name = TextObjPrefix + _value;
                //改文字
                __instance.ociFolder.objectItem.GetComponentInChildren<TextMesh>().text = _value;
                Logger.Log(LogLevel.Info, "[KK_STP] Edit Text: " + _value);
                return false;
            }
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MPFolderCtrl), "UpdateInfo")]
        public static void UpdateInfoPostfix(MPFolderCtrl __instance) {
            //創建、更新編輯框時觸發
            if (__instance.ociFolder == null) {
                return;
            }

            if (__instance.ociFolder.name.Contains(TextConfigPrefix) ||
                    __instance.ociFolder.name.Contains(TextConfigFontPrefix) ||
                    __instance.ociFolder.name.Contains(TextConfigFontSizePrefix) ||
                    __instance.ociFolder.name.Contains(TextConfigFontStylePrefix) ||
                    __instance.ociFolder.name.Contains(TextConfigColorPrefix)
                ) {
                __instance.active = false;
            }

            if (__instance.ociFolder.name.Contains(TextObjPrefix)) {
                __instance.SetField("isUpdateInfo", true);
                InputField input = (InputField)__instance.GetField("inputName");
                //文字帶入編輯器時去掉prefix
                input.text = __instance.ociFolder.name.Replace(TextObjPrefix, "");
                __instance.SetField("inputName", input);
                __instance.SetField("isUpdateInfo", false);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ManipulatePanelCtrl), "SetActive")]
        public static void SetActivePostfix(ManipulatePanelCtrl __instance) {
            if (__instance.GetField("folderPanelInfo")?.GetField("m_MPFolderCtrl") != null) {
                UpdateInfoPostfix(__instance.GetField("folderPanelInfo")?.GetField("m_MPFolderCtrl") as MPFolderCtrl);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(TreeNodeCtrl), "RefreshHierachyLoop")]
        public static void RefreshHierachyLoopPostfix(TreeNodeObject _source, int _indent, bool _visible) {
            //套用文字狀態
            ObjectCtrlInfo objectCtrlInfo = Studio.Studio.GetCtrlInfo(_source);
            if (null != objectCtrlInfo &&
                    objectCtrlInfo.objectInfo.kind == 3 &&
                    objectCtrlInfo is OCIFolder oCIFolder &&
                    oCIFolder.name.Contains(TextObjPrefix) &&
                    !isCreatingTextStructure
                ) {
                TextPlugin.SetFont(oCIFolder, TextPlugin.GetConfig(oCIFolder.treeNodeObject, TextPlugin.Config.Font));
                TextPlugin.MakeAndSetConfigStructure(oCIFolder.treeNodeObject, TextPlugin.Config.Font);

                if (!float.TryParse(TextPlugin.GetConfig(oCIFolder.treeNodeObject, TextPlugin.Config.FontSize), out var f)) {
                    f = 1f;
                }
                TextPlugin.SetCharacterSize(oCIFolder, f);
                TextPlugin.MakeAndSetConfigStructure(oCIFolder.treeNodeObject, TextPlugin.Config.FontSize);

                TextPlugin.SetFontStyle(oCIFolder, TextPlugin.GetConfig(oCIFolder.treeNodeObject, TextPlugin.Config.FontStyle));
                TextPlugin.MakeAndSetConfigStructure(oCIFolder.treeNodeObject, TextPlugin.Config.FontStyle);

                if (!ColorUtility.TryParseHtmlString(TextPlugin.GetConfig(oCIFolder.treeNodeObject, TextPlugin.Config.Color), out var color)) {
                    color = Color.white;
                }
                TextPlugin.SetColor(oCIFolder, color);
                TextPlugin.MakeAndSetConfigStructure(oCIFolder.treeNodeObject, TextPlugin.Config.Color);
                oCIFolder.objectItem.SetActive(_source.visible);
            }
        }
    }
}
