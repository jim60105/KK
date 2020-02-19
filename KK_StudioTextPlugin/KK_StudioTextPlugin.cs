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
using BepInEx.Harmony;
using BepInEx.Logging;
using Extension;
using HarmonyLib;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KK_StudioTextPlugin {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioTextPlugin : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Text Plugin";
        internal const string GUID = "com.jim60105.kk.studiotextplugin";
        internal const string PLUGIN_VERSION = "20.02.19.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.1.4";

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            if (TextPlugin.Awake()) {
                UIUtility.Init();
                HarmonyWrapper.PatchAll(typeof(Patches));
            }
        }

        public static ConfigEntry<bool> Auto_change_default { get; private set; }
        public static ConfigEntry<string> Default_New_Text { get; private set; }

        // 新增字體物件時的預設參數
        public static ConfigEntry<string> Default_FontName { get; private set; }
        public static ConfigEntry<float> Default_FontSize { get; private set; }
        public static ConfigEntry<Color> Default_Color { get; private set; }
        public static ConfigEntry<FontStyle> Default_FontStyle { get; private set; }
        public static ConfigEntry<TextAlignment> Default_Alignment { get; private set; }
        public static ConfigEntry<TextAnchor> Default_Anchor { get; private set; }

        public void Start() {
            Default_New_Text = Config.Bind<string>("Config", "Default New Text", "NewText", "Text for new text. This is not affected by the Auto Change setting.");
            Auto_change_default = Config.Bind<bool>("Config", "Auto Change Default", true, "Save all text settings to create the next new Text Object.");

            Default_FontName = Config.Bind<string>("Default Settings", "FontName", "MS Gothic", new ConfigDescription("", new AcceptableValueList<string>(TextPlugin.GetDynamicFontNames().ToArray())));
            Default_FontSize = Config.Bind<float>("Default Settings", "FontSize", 1f, "Enter only floating point numbers.");
            Default_Color = Config.Bind<Color>("Default Settings", "Color", Color.white);
            Default_FontStyle = Config.Bind<FontStyle>("Default Settings", "FontStyle", FontStyle.Normal);
            Default_Alignment = Config.Bind<TextAlignment>("Default Settings", "Alignment", TextAlignment.Center);
            Default_Anchor = Config.Bind<TextAnchor>("Default Settings", "Anchor", TextAnchor.MiddleCenter);
        }
    }

    static class Patches {
        internal static ManualLogSource Logger = KK_StudioTextPlugin.Logger;
        //Flag
        private static bool isCreatingTextFolder = false;
        internal static bool isCreatingTextStructure = false;
        private static bool isConfigPanelCreated = false;
        private static bool onUpdating = false;

        //資料夾名稱前墜 - 千萬不要改動這些!!!
        public static readonly string TextObjPrefix = "-Text Plugin:";
        public static readonly string TextConfigPrefix = "-Text Config";
        public static readonly string TextConfigFontPrefix = "-Text Font:";
        public static readonly string TextConfigFontSizePrefix = "-Text FontSize:";
        public static readonly string TextConfigFontStylePrefix = "-Text FontStyle:";
        public static readonly string TextConfigColorPrefix = "-Text Color:";
        public static readonly string TextConfigAnchorPrefix = "-Text Anchor:";
        public static readonly string TextConfigAlignPrefix = "-Text Align:";

        internal static Image panel;
        internal static Image panelSelectFont;
        internal static void DrawConfigPanel() {
            if (isConfigPanelCreated) {
                return;
            }
            var panelRoot = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/04_Folder");
            //畫Config視窗
            panel = UIUtility.CreatePanel("ConfigPanel", panelRoot.transform);
            panel.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -510), new Vector2(160, -70));

            //畫大輸入視窗
            Image nameInputPanel = UIUtility.CreatePanel("FolderNameInputPanel", panel.transform);
            nameInputPanel.transform.SetRect(Vector2.up, Vector2.up, new Vector2(165f, -200f), new Vector2(645f, 0f));
            InputField inputField = UIUtility.CreateInputField("FolderNameInput", nameInputPanel.transform, "");
            inputField.transform.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));
            inputField.textComponent.resizeTextMinSize = 15;
            inputField.textComponent.resizeTextMaxSize = 20;
            inputField.text = KK_StudioTextPlugin.Default_New_Text.Value;
            inputField.lineType = InputField.LineType.MultiLineNewline;
            inputField.textComponent.alignment = TextAnchor.UpperLeft;
            nameInputPanel.gameObject.SetActive(false);

            //Config1: Font
            var text1 = UIUtility.CreateText("FontText", panel.transform, "Font");
            text1.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -30f), new Vector2(155f, -5f));

            //畫SelectFont視窗
            panelSelectFont = UIUtility.CreatePanel("SelectFontPanel", panel.transform);
            if (!TextPlugin.DisablePreview) {
                panelSelectFont.transform.SetRect(Vector2.up, Vector2.up, new Vector2(165f, -900f), new Vector2(645f, 0f));
            } else {
                panelSelectFont.transform.SetRect(Vector2.up, Vector2.up, new Vector2(165f, -900f), new Vector2(500f, 0f));
            }

            Button selectFontBtn = UIUtility.CreateButton("SelectFontBtn", panel.transform, "MS Gothic");
            selectFontBtn.GetComponentInChildren<Text>(true).color = Color.white;
            selectFontBtn.GetComponent<Image>().color = Color.gray;
            selectFontBtn.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -85f), new Vector2(155f, -35f));
            selectFontBtn.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));
            selectFontBtn.onClick.AddListener(delegate {
                panelSelectFont.gameObject.SetActive(!panelSelectFont.gameObject.activeSelf);
            });

            //滾動元件
            ScrollRect scrollRect = UIUtility.CreateScrollView("scroll", panelSelectFont.transform);
            var btnGroup = scrollRect.content;
            scrollRect.transform.SetRect(Vector2.zero, Vector2.one, new Vector2(0f, 5f), new Vector2(0, -5f));
            scrollRect.GetComponent<Image>().color = new Color32(0, 0, 0, 0);
            foreach (var img in scrollRect.verticalScrollbar.GetComponentsInChildren<Image>()) {
                img.color = Color.Lerp(img.color, Color.black, 0.6f);
            }
            (scrollRect.verticalScrollbar.transform as RectTransform).offsetMin = new Vector2(-16f, 0);
            scrollRect.scrollSensitivity = 50;
            //字體預覽之按鈕清單
            var tmpBtns = new List<Button>();
            foreach (string fontName in TextPlugin.GetDynamicFontNames()) {
                Button fontDisplayBtn = UIUtility.CreateButton("fontDisplayBtn", btnGroup.transform, fontName);
                Text text = fontDisplayBtn.GetComponentInChildren<Text>(true);
                text.color = Color.white;
                text.transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 7.5f), new Vector2(-5f, -5f));
                text.text = fontName;

                if (!TextPlugin.DisablePreview) {
                    text.alignment = TextAnchor.UpperLeft;
                    text.resizeTextForBestFit = false;
                    text.fontSize = 30;
                    Text demoText = UnityEngine.Object.Instantiate(text, text.transform.parent);
                    demoText.name = "demoText";
                    demoText.alignment = TextAnchor.LowerLeft;
                    demoText.text = KK_StudioTextPlugin.Default_New_Text.Value;
                    demoText.font = TextPlugin.GetFont(fontName);
                    demoText.font.RequestCharactersInTexture(KK_StudioTextPlugin.Default_New_Text.Value);
                    demoText.transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 7.5f), new Vector2(-5f, -30f));
                    fontDisplayBtn.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -75f * (tmpBtns.Count + 1)), new Vector2(-5f, -75f * tmpBtns.Count));
                } else {
                    text.alignment = TextAnchor.MiddleLeft;
                    fontDisplayBtn.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -35f * (tmpBtns.Count + 1)), new Vector2(-5f, -35f * tmpBtns.Count));
                }

                fontDisplayBtn.GetComponent<Image>().color = Color.gray;
                fontDisplayBtn.onClick.AddListener(delegate {
                    if (!onUpdating) {
                        selectFontBtn.GetComponentInChildren<Text>().text = fontName;
                        TextPlugin.ChangeFont(fontName);
                        panelSelectFont.gameObject.SetActive(false);
                    }
                });
                tmpBtns.Add(fontDisplayBtn);
            }
            if (TextPlugin.DisablePreview) {
                btnGroup.transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(0, 900f - (35f * tmpBtns.Count) - 10f), new Vector2(0, 0));
            } else {
                btnGroup.transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(0, 900f - (75f * tmpBtns.Count) - 10f), new Vector2(0, 0));
            }
            EventTrigger trigger3 = panelSelectFont.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry3 = new EventTrigger.Entry { eventID = EventTriggerType.Deselect };
            entry3.callback.AddListener((data) => {
                panelSelectFont.gameObject.SetActive(false);
                Logger.LogDebug("LostFocus.");
            });
            trigger3.triggers.Add(entry3);
            panelSelectFont.gameObject.SetActive(false);

            //Config2: FontSize (CharacterSize)
            var text2 = UIUtility.CreateText("FontSize", panel.transform, "FontSize");
            text2.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -115f), new Vector2(155f, -90f));
            var input = UIUtility.CreateInputField("fontSizeInput", panel.transform, "e.g., 1.56 (float)");
            input.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -155f), new Vector2(155f, -120f));
            input.text = "1";
            input.onEndEdit.AddListener(delegate {
                if (!onUpdating) {
                    if (!float.TryParse(input.text, out float f)) {
                        Logger.LogError("FormatException: Please input only numbers into FontSize.");
                        Logger.LogMessage("FormatException: Please input only numbers into FontSize.");
                        input.text = TextPlugin.GetConfig(null, TextPlugin.Config.FontSize);
                    } else {
                        TextPlugin.ChangeCharacterSize(f);
                    }
                }
            });

            //Config3: FontStyle
            var text3 = UIUtility.CreateText("FontStyle", panel.transform, "FontStyle");
            text3.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -185f), new Vector2(155f, -160f));
            var d2 = UIUtility.CreateDropdown("fontStyleDropdown", panel.transform);
            d2.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -225f), new Vector2(155f, -190f));
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

            //Config4: Anchor 
            var text4 = UIUtility.CreateText("Anchor", panel.transform, "Anchor");
            text4.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -325f), new Vector2(155f, -300f));
            var d3 = UIUtility.CreateDropdown("anchorDropdown", panel.transform);
            d3.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -365f), new Vector2(155f, -330f));
            //Font List
            d3.options.Clear();
            var textAnchor = Enum.GetNames(typeof(TextAnchor));
            d3.AddOptions(textAnchor.ToList());
            d3.value = 4;
            //Change Anchor 
            d3.onValueChanged.AddListener(delegate {
                if (!onUpdating) {
                    TextPlugin.ChangeAnchor(textAnchor[d3.value]);
                }
            });

            //Config5: Alignment 
            var text5 = UIUtility.CreateText("AlignmentText", panel.transform, "Align");
            text5.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -395f), new Vector2(155f, -370f));
            var d4 = UIUtility.CreateDropdown("alignmentDropdown", panel.transform);
            d4.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -435f), new Vector2(155f, -400f));
            //Font List
            d4.options.Clear();
            var textAlignment = Enum.GetNames(typeof(TextAlignment));
            d4.AddOptions(textAlignment.ToList());
            d4.value = 1;
            //Change Alignment 
            d4.onValueChanged.AddListener(delegate {
                if (!onUpdating) {
                    TextPlugin.ChangeAlignment(textAlignment[d4.value]);
                }
            });

            //Config6: Color
            var text6 = UIUtility.CreateText("Color", panel.transform, "Color");
            text6.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -255f), new Vector2(155f, -230f));
            var btn = UIUtility.CreateButton("colorBtn", panel.transform, "");
            btn.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -295f), new Vector2(155f, -260f));
            btn.onClick.AddListener(delegate {
                if (!onUpdating) {
                    if (!ColorUtility.TryParseHtmlString(TextPlugin.GetConfig(null, TextPlugin.Config.Color), out var color)) {
                        color = KK_StudioTextPlugin.Default_Color.Value;
                    }
                    Singleton<Studio.Studio>.Instance.colorPalette.Setup("字體顏色", color, new Action<Color>(TextPlugin.ChangeColor), true);
                    Singleton<Studio.Studio>.Instance.colorPalette.visible = true;
                }
            });

            foreach (var image in panel.GetComponentsInChildren<Image>(true)) {
                image.color = new Color32(120, 120, 120, 220);
            }
            foreach (var text in panel.GetComponentsInChildren<Text>(true)) {
                text.color = Color.white;
            }
            input.GetComponent<InputField>().placeholder.GetComponent<Text>().color = new Color(1, 1, 1, 0.3f);
            btn.image.color = KK_StudioTextPlugin.Default_Color.Value;

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
            CheckAlignSettingActive();
            Logger.LogDebug("Draw ConfigPanel Finish");
            isConfigPanelCreated = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(TreeNodeObject), "OnClickSelect")]
        public static void OnClickSelectPostfix(TreeNodeObject __instance) {
            if (Studio.Studio.GetCtrlInfo(__instance) is ObjectCtrlInfo objectCtrlInfo  &&
                objectCtrlInfo.objectInfo?.kind == 3 && 
                objectCtrlInfo is OCIFolder oCIFolder
            ) {
                //選擇到config資料夾時往上跳
                if (oCIFolder.name.Contains(TextConfigPrefix) ||
                    oCIFolder.name.Contains(TextConfigFontPrefix) ||
                    oCIFolder.name.Contains(TextConfigFontSizePrefix) ||
                    oCIFolder.name.Contains(TextConfigFontStylePrefix) ||
                    oCIFolder.name.Contains(TextConfigColorPrefix) ||
                    oCIFolder.name.Contains(TextConfigAnchorPrefix) ||
                    oCIFolder.name.Contains(TextConfigAlignPrefix)
                ) {
                    Singleton<Studio.Studio>.Instance.treeNodeCtrl.SelectSingle(__instance.parent);
                    OnClickSelectPostfix(__instance.parent);
                    return;
                }

                if (oCIFolder.name.Contains(TextObjPrefix)) {
                    TextMesh t = oCIFolder.objectItem.GetComponentInChildren<TextMesh>(true);
                    if (null == t) {
                        TextPlugin.MakeTextObj(oCIFolder, oCIFolder.name.Replace(TextObjPrefix, ""));
                        TextPlugin.MakeAndSetConfigStructure(oCIFolder.treeNodeObject);
                        t = oCIFolder.objectItem.GetComponentInChildren<TextMesh>(true);
                    }
                    MeshRenderer m = oCIFolder.objectItem.GetComponentInChildren<MeshRenderer>(true);
                    onUpdating = true;
                    panel.gameObject.SetActive(true);

                    //加載編輯選單內容
                    //Font
                    if (TextPlugin.CheckFontInOS(t.font.name)) {
                        panel.transform.Find("SelectFontBtn").GetComponent<Button>().GetComponentInChildren<Text>().text = t.font.name;
                        EditDemoText(t.text);
                    }

                    //FontSize
                    panel.GetComponentsInChildren<InputField>(true).Where(x => x.name == "fontSizeInput").Single().text = (t.characterSize * 500).ToString();
                    //FontStyle
                    panel.GetComponentsInChildren<Dropdown>(true)[0].value = Array.IndexOf(Enum.GetNames(typeof(FontStyle)), t.fontStyle.ToString());
                    //Color
                    panel.transform.Find("colorBtn").GetComponent<Button>().image.color = m.material.color;
                    //Anchor
                    panel.GetComponentsInChildren<Dropdown>(true)[1].value = Array.IndexOf(Enum.GetNames(typeof(TextAnchor)), t.anchor.ToString());
                    //Alignment
                    panel.GetComponentsInChildren<Dropdown>(true)[2].value = Array.IndexOf(Enum.GetNames(typeof(TextAlignment)), t.alignment.ToString());

                    CheckAlignSettingActive();
                    onUpdating = false;
                }
            }
        }

        //屏蔽掉Config資料夾的parent方法，以避免資料夾結構變動
        private static bool blockChangeParentFlag = false;
        [HarmonyPrefix, HarmonyPatch(typeof(WorkspaceCtrl), "OnClickParent")]
        public static void OnClickParentPrefix() => blockChangeParentFlag = true;
        [HarmonyPostfix, HarmonyPatch(typeof(WorkspaceCtrl), "OnClickParent")]
        public static void OnClickParentPostfix() => blockChangeParentFlag = false;

        [HarmonyPrefix, HarmonyPatch(typeof(WorkspaceCtrl), "OnClickRemove")]
        public static void OnClickRemovePrefix() => blockChangeParentFlag = true;
        [HarmonyPostfix, HarmonyPatch(typeof(WorkspaceCtrl), "OnClickRemove")]
        public static void OnClickRemovePostfix() => blockChangeParentFlag = false;

        [HarmonyPrefix, HarmonyPatch(typeof(TreeNodeCtrl), "SetParent", new Type[] { typeof(TreeNodeObject), typeof(TreeNodeObject) })]
        public static bool SetParentPrefix(ref TreeNodeObject _node) {
            //ObjectCtrlInfo objectCtrlInfo = Studio.Studio.GetCtrlInfo(_node);
            if (blockChangeParentFlag &&
                Studio.Studio.GetCtrlInfo(_node) is ObjectCtrlInfo objectCtrlInfo &&
                objectCtrlInfo.objectInfo?.kind == 3 &&
                objectCtrlInfo is OCIFolder oCIFolder &&
                    (oCIFolder.name.Contains(TextConfigPrefix) ||
                    oCIFolder.name.Contains(TextConfigFontPrefix) ||
                    oCIFolder.name.Contains(TextConfigFontSizePrefix) ||
                    oCIFolder.name.Contains(TextConfigFontStylePrefix) ||
                    oCIFolder.name.Contains(TextConfigColorPrefix) ||
                    oCIFolder.name.Contains(TextConfigAnchorPrefix) ||
                    oCIFolder.name.Contains(TextConfigAlignPrefix))
            ) {
                //Logger.LogDebug("Block parent changing.");
                return false;
            }
            return true;
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
                        OnClickSelectPostfix(textOCIFolder.treeNodeObject);
                    }
                });
                //目前最大的漢字編碼是臺灣的國家標準CNS11643，目前（4.0）共收錄可考證之正簡、日、韓語漢字共76,067個
                ((Dictionary<int, Image>)__instance.GetField("dicNode")).Add(76067, gameObject.GetComponent<Image>());
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AddObjectFolder), "Load", new Type[] { typeof(OIFolderInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
        public static void LoadPostfix(ref OCIFolder __result, OIFolderInfo _info, ObjectCtrlInfo _parent, TreeNodeObject _parentNode, bool _addInfo, int _initialPosition) {
            //Scene讀取的進入點
            if (isCreatingTextFolder || __result.name.Contains(TextObjPrefix)) {
                __result.name = isCreatingTextFolder ? TextObjPrefix + KK_StudioTextPlugin.Default_New_Text.Value : _info.name;
                var t = TextPlugin.MakeTextObj(__result, isCreatingTextFolder ? KK_StudioTextPlugin.Default_New_Text.Value : _info.name.Replace(TextObjPrefix, "").Replace("\\n", "\n"));
                isCreatingTextFolder = false;
                if (_addInfo) {
                    //Scene Load就不創建Config資料夾結構
                    TextPlugin.MakeAndSetConfigStructure(__result.treeNodeObject);
                }
                //套用座標、旋轉、縮放
                _info.changeAmount.OnChange();
                Logger.LogDebug($"Pos:{_info.changeAmount.pos.ToString()}");
                Logger.LogDebug($"Rot:{_info.changeAmount.rot.ToString()}");
                Logger.LogDebug($"Scale:{_info.changeAmount.scale.ToString()}");

                Logger.LogInfo("Load Text:" + t.text);
            }

            //處理Folder未定義OnVisible造成的不隱藏
            if (__result.name.Contains(TextObjPrefix))
                __result.treeNodeObject.onVisible = (TreeNodeObject.OnVisibleFunc)Delegate.Combine(__result.treeNodeObject.onVisible, new TreeNodeObject.OnVisibleFunc(__result.OnVisible));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(OCIFolder), "OnVisible")]
        public static void OnVisiblePostfix(bool _visible, OCIFolder __instance) {
            if (__instance.name.Contains(TextObjPrefix)) {
                GameObject go = __instance.objectItem.GetComponentInChildren<TextMesh>(true).gameObject;
                go?.SetActive(_visible);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MPFolderCtrl), "Start")]
        public static void Start(MPFolderCtrl __instance) {
            if (__instance.ociFolder.name.Contains(TextObjPrefix)) {
                InputField input = (InputField)__instance.GetField("inputName");
                input.readOnly = true;

                var inputFieldPanel = __instance.transform.Find("ConfigPanel/FolderNameInputPanel").gameObject;
                var inputField = inputFieldPanel.GetComponentInChildren<InputField>();

                inputField.onEndEdit.RemoveAllListeners();
                inputField.onEndEdit.AddListener(delegate {
                    input.text = inputField.text.TrimEnd('\n').Replace("\n", "\\n");
                    input.onEndEdit.Invoke(input.text);
                    inputFieldPanel.SetActive(false);
                });

                EventTrigger trigger = input.gameObject.AddComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                entry.callback.AddListener((data) => {
                    //點擊FolderName輸入框時觸發
                    inputField.text = input.text.Replace("\\n", "\n");
                    inputFieldPanel.SetActive(true);
                    inputField.Select();
                });
                trigger.triggers.Add(entry);
            }
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
                _value = _value.Replace("\\n", "\n");
                __instance.ociFolder.objectItem.GetComponentInChildren<TextMesh>(true).text = _value;
                EditDemoText(_value);
                CheckAlignSettingActive();
                Logger.LogInfo("Edit Text: " + _value);
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
                    __instance.ociFolder.name.Contains(TextConfigColorPrefix) ||
                    __instance.ociFolder.name.Contains(TextConfigAnchorPrefix) ||
                    __instance.ociFolder.name.Contains(TextConfigAlignPrefix)
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
                doMain(TextPlugin.SetFont, TextPlugin.Config.Font);
                doMain(TextPlugin.SetCharacterSize, TextPlugin.Config.FontSize);
                doMain(TextPlugin.SetFontStyle, TextPlugin.Config.FontStyle);
                doMain(TextPlugin.SetColor, TextPlugin.Config.Color);
                doMain(TextPlugin.SetAnchor, TextPlugin.Config.Anchor);
                doMain(TextPlugin.SetAlignment, TextPlugin.Config.Align);
                //oCIFolder.objectItem.SetActive(true);

                void doMain(Action<OCIFolder, string> setConfigFunc, TextPlugin.Config config) {
                    //如果沒有找到該設定項的folder，就不設定和建立，否則在讀取Scene時會報錯
                    if (TextPlugin.GetConfig(oCIFolder.treeNodeObject, config) is string s) {
                        setConfigFunc(oCIFolder, s);
                        TextPlugin.MakeAndSetConfigStructure(oCIFolder.treeNodeObject, config);
                    }
                }
            }
        }

        /// <summary>
        /// 觸發對齊選單項顯示與否
        /// </summary>
        private static void CheckAlignSettingActive() {
            bool active = false;
            OCIFolder[] folderArray = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                                       select Studio.Studio.GetCtrlInfo(v) as OCIFolder into v
                                       where v != null
                                       select v).ToArray();
            foreach (var oCIFolder in folderArray) {
                if (oCIFolder.objectItem.GetComponentInChildren<TextMesh>(true)?.text.IndexOf("\n") >= 0) {
                    active = true;
                    break;
                }
            }

            if (active) {
                panel.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -510), new Vector2(160, -70));
            } else {
                panel.transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -440), new Vector2(160, -70));
            }
            //Alignment
            panel.transform.Find("AlignmentText").GetComponent<Text>().gameObject.SetActive(active);
            panel.GetComponentsInChildren<Dropdown>(true).Last().gameObject.SetActive(active);
        }

        /// <summary>
        /// 修改Font選單的所有demoText
        /// </summary>
        /// <param name="text">要顯示的demoText</param>
        private static void EditDemoText(string text) {
            if (text.IndexOf("\n") > 0) {
                text = text.Substring(0, text.IndexOf("\n"));
            }
            if (!TextPlugin.DisablePreview) {
                foreach (var t in panelSelectFont.transform.GetComponentsInChildren<Text>()) {
                    if (t.name == "demoText") {
                        t.font.RequestCharactersInTexture(text);
                        t.text = text;
                    }
                }
            }
        }
    }
}
