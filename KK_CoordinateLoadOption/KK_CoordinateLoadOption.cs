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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChaCustom;
using ExIni;
using ExtensibleSaveFormat;
using Extension;
using HarmonyLib;
using MessagePack;
using Studio;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KK_CoordinateLoadOption {
    using SCLO = KK_CoordinateLoadOption;

    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("Koikatu")]
    [BepInProcess("Koikatsu Party")]
    [BepInProcess("CharaStudio")]
    [BepInDependency("KCOX", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("KKABMX.Core", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.joan6694.illusionplugins.moreaccessories", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.deathweasel.bepinex.materialeditor", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.deathweasel.bepinex.hairaccessorycustomizer", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.jim60105.kk.charaoverlaysbasedoncoordinate", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInIncompatibility("KK_ClothesLoadOption")]
    [BepInIncompatibility("com.jim60105.kk.studiocoordinateloadoption")]
    public class KK_CoordinateLoadOption : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Coordinate Load Option";
        internal const string GUID = "com.jim60105.kk.coordinateloadoption";
        internal const string PLUGIN_VERSION = "21.12.11.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.1.7";

        public static bool insideStudio = Application.productName == "CharaStudio";

        Harmony harmonyInstance;
        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            Extension.Logger.logger = Logger;
            UIUtility.Init();
            harmonyInstance = Harmony.CreateAndPatchAll(typeof(Patches));
            //harmonyInstance.PatchAll(typeof(MoreAccessories_Support));

            //Studio
            Type CostumeInfoType = typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic);
            harmonyInstance.Patch(CostumeInfoType.GetMethod("Init", AccessTools.all),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.InitPostfix)));
            harmonyInstance.Patch(CostumeInfoType.GetMethod("OnClickLoad", AccessTools.all),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Patches.OnClickLoadPrefix)),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.OnClickLoadPostfix)));
            harmonyInstance.Patch(CostumeInfoType.GetMethod("OnSelect", AccessTools.all),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.OnSelectPostfix)));

            //Maker
            harmonyInstance.Patch(typeof(CustomCoordinateFile).GetMethod("Start", AccessTools.all),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.InitPostfix)));
            harmonyInstance.Patch(typeof(CustomCoordinateFile).GetMethod("OnChangeSelect", AccessTools.all),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.OnSelectPostfix)));
        }

        public static bool _isKCOXExist = false;
        public static bool _isABMXExist = false;
        public static bool _isMoreAccessoriesExist = false;
        public static bool _isMaterialEditorExist = false;
        public static bool _isHairAccessoryCustomizerExist = false;
        public static bool _isCharaOverlayBasedOnCoordinateExist = false;

        internal const int FORCECLEANCOUNT = 100;    //tmpChara清理倒數
        public enum ClothesKind {
            top = 0,
            bot = 1,
            bra = 2,
            shorts = 3,
            gloves = 4,
            panst = 5,
            socks = 6,
            shoes_inner = 7,
            shoes_outer = 8,
            accessories = 9 /*注意這個*/
        }

        public static string[] pluginBoundAccessories =
        {
            // Pre-filled
            "madevil.kk.ass",
            "madevil.kk.mr",
            "madevil.kk.ca",
            "madevil.kk.BonerStateSync",
            "madevil.kk.BendUrAcc",
            "madevil.kk.AAAPK"
        };

        internal static Vector3 defaultPanelPosition = Vector3.zero;
        public static ConfigEntry<Vector3> Maker_Panel_Position { get; private set; }
        public static ConfigEntry<Vector3> Studio_Panel_Position { get; private set; }
        public static ConfigEntry<string> Plugin_Bound_Accessories { get; private set; }

        public void Start() {
            _isKCOXExist = new KCOX_CCFCSupport(null).LoadAssembly();
            _isABMXExist = new ABMX_CCFCSupport(null).LoadAssembly();
            _isMoreAccessoriesExist = MoreAccessories_Support.LoadAssembly();
            _isMaterialEditorExist = new MaterialEditor_CCCFCSupport(null).LoadAssembly();
            _isHairAccessoryCustomizerExist = new HairAccessoryCustomizer_CCFCSupport(null).LoadAssembly();
            _isCharaOverlayBasedOnCoordinateExist = new COBOC_CCFCSupport(null).LoadAssembly();

            //Patch other plugins at Start()
            if(_isHairAccessoryCustomizerExist)
                HairAccessoryCustomizer_CCFCSupport.Patch(harmonyInstance);

            StringResources.StringResourcesManager.SetUICulture();

            Plugin_Bound_Accessories = Config.Bind<string>("Settings", "Plugin that bound accessories options", "", new ConfigDescription("Edit this only when any plugin maker tells you to do so. Fill in the GUIDs, and seperate them with comma(,), example: 'this.guid.A,some.guid.B,another.guid.C'"));
            pluginBoundAccessories = pluginBoundAccessories.Concat(Plugin_Bound_Accessories.Value.Split(','))
                                                           .Distinct()
                                                           .ToArray();

            if (insideStudio) {
                Studio_Panel_Position = Config.Bind<Vector3>("Settings", "Studio Panel Position", defaultPanelPosition);
                Studio_Panel_Position.SettingChanged += _MovePanel;
            } else {
                Maker_Panel_Position = Config.Bind<Vector3>("Settings", "Maker Panel Position", defaultPanelPosition);
                Maker_Panel_Position.SettingChanged += _MovePanel;
            }

            void _MovePanel(object sender, EventArgs e) {
                if (sender is ConfigEntry<Vector3> s) {
                    Patches.panel.transform.localPosition = s.Value;
                    if (s.Value == Vector3.zero && defaultPanelPosition != Vector3.zero) s.Value = defaultPanelPosition;
                }
            };
        }

        public void Update() => CoordinateLoad.Update();
    }

    //用於MakerUI之OnEnable回呼
    class OnEnableListener : MonoBehaviour {
        public delegate void EventHandler();
        public event EventHandler OnEnableEvent;

        public void OnEnable() => this.StartCoroutine(OnEnableCoroutine());

        //確保能在最後執行
        private IEnumerator OnEnableCoroutine() {
            yield return null;
            OnEnableEvent?.Invoke();
        }
    }

    class Patches {
        private static readonly ManualLogSource Logger = SCLO.Logger;
        private static CustomFileWindow CustomFileWindow;
        internal static string coordinatePath;

        public static string[] ClothesKindName;

        internal static Toggle[] tgls;
        internal static Toggle[] tgls2 = new Toggle[0]; //使用時再初始化
        internal static Image panel;
        private static Image panel2;
        private static RectTransform toggleGroup;
        internal static bool lockHairAcc = false;
        internal static bool addAccModeFlag = true;
        internal static bool[] charaOverlay = new bool[] { true, true, true };  //順序: Iris、Face、Body
        internal static bool readABMX = true;
        internal static bool boundAcc = false;

        public static void InitPostfix(object __instance) {
            //如果啟動機翻就一律顯示英文，否則機翻會毀了我的文字
            if (null != KoikatuHelper.TryGetPluginInstance("gravydevsupreme.xunity.autotranslator")) {
                string XUAConfigPath = Path.Combine(Paths.ConfigPath, "AutoTranslatorConfig.ini");
                if (IniFile.FromFile(XUAConfigPath).GetSection("TextFrameworks").GetKey("EnableUGUI").Value == "True") {
                    StringResources.StringResourcesManager.SetUICulture("en-US");
                    Logger.LogInfo("Found XUnityAutoTranslator Enabled, load English UI");
                }
            }

            ClothesKindName = new string[]{
                StringResources.StringResourcesManager.GetString("ClothesKind_top"),
                StringResources.StringResourcesManager.GetString("ClothesKind_bot"),
                StringResources.StringResourcesManager.GetString("ClothesKind_bra"),
                StringResources.StringResourcesManager.GetString("ClothesKind_shorts"),
                StringResources.StringResourcesManager.GetString("ClothesKind_gloves"),
                StringResources.StringResourcesManager.GetString("ClothesKind_panst"),
                StringResources.StringResourcesManager.GetString("ClothesKind_socks"),
                StringResources.StringResourcesManager.GetString("ClothesKind_shoes_inner"),
                StringResources.StringResourcesManager.GetString("ClothesKind_shoes_outer"),
                StringResources.StringResourcesManager.GetString("ClothesKind_accessories")
            };

            Array ClothesKindArray = Enum.GetValues(typeof(SCLO.ClothesKind));

            Transform PanelParent = null;
            if (SCLO.insideStudio) {
                PanelParent = (__instance.GetField("fileSort") as CharaFileSort).root.parent.parent.parent;
            } else {
                //Maker
                PanelParent = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow/WinRect/CoordinateLoad").transform;
            }

            #region UI
            //Show Selection Btn
            Image coordianteLoadPanel = UIUtility.CreatePanel("CoordinateLoadPanel", PanelParent);
            coordianteLoadPanel.GetComponent<Image>().color = new Color32(80, 80, 80, 220);
            Button btnCoordinateLoadOption = UIUtility.CreateButton("CoordinateLoadBtn", coordianteLoadPanel.transform, StringResources.StringResourcesManager.GetString("showSelection") + (KK_CoordinateLoadOption.insideStudio ? "" : ">>"));
            btnCoordinateLoadOption.GetComponentInChildren<Text>(true).color = Color.white;
            btnCoordinateLoadOption.GetComponent<Image>().color = Color.gray;
            btnCoordinateLoadOption.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -35f), new Vector2(-5f, -5f));
            btnCoordinateLoadOption.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            //Draw Panel
            panel = UIUtility.CreatePanel("CoordinateTooglePanel", PanelParent);
            panel.GetComponent<Image>().color = new Color32(80, 80, 80, 220);

            //Draw ButtonAll
            Button btnAll = UIUtility.CreateButton("BtnAll", panel.transform, "All");
            btnAll.GetComponentInChildren<Text>(true).color = Color.white;
            btnAll.GetComponent<Image>().color = Color.gray;
            btnAll.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -30), new Vector2(-5f, -5f));
            btnAll.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            //Draw Toggles
            tgls = new Toggle[ClothesKindArray.Length];
            for (int i = 0; i < ClothesKindArray.Length - 1; i++) {
                tgls[i] = UIUtility.CreateToggle(ClothesKindArray.GetValue(i).ToString(), panel.transform, ClothesKindName.GetValue(i).ToString());
                tgls[i].GetComponentInChildren<Text>(true).alignment = TextAnchor.MiddleLeft;
                tgls[i].GetComponentInChildren<Text>(true).color = Color.white;
                tgls[i].transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -60f - 25f * i), new Vector2(-5f, -35f - 25f * i));
                tgls[i].GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));
            }

            //分隔線
            Image line = UIUtility.CreateImage("line", panel.transform);
            line.color = Color.gray;
            line.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -262.5f), new Vector2(-5f, -261f));

            //AccToggle
            tgls[9] = UIUtility.CreateToggle(ClothesKindArray.GetValue(9).ToString(), panel.transform, ClothesKindName.GetValue(9).ToString());
            tgls[9].GetComponentInChildren<Text>(true).alignment = TextAnchor.MiddleLeft;
            tgls[9].GetComponentInChildren<Text>(true).color = Color.white;
            tgls[9].transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -292.5f), new Vector2(-5f, -267.5f));
            tgls[9].GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));
            tgls[9].onValueChanged.AddListener((x) => {
                if (tgls2.Length != 0 && null != panel2) {
                    panel2.gameObject.SetActive(x);
                }
            });
            List<Toggle> toggleList = tgls.ToList();

            //清空飾品btn
            Button btnClearAcc = UIUtility.CreateButton("BtnClearAcc", panel.transform, StringResources.StringResourcesManager.GetString("clearAccWord"));
            btnClearAcc.GetComponentInChildren<Text>(true).color = Color.white;
            btnClearAcc.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperCenter;
            btnClearAcc.GetComponent<Image>().color = Color.gray;
            btnClearAcc.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -317.5f), new Vector2(-5f, -292.5f));
            btnClearAcc.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            float baseY = -322;
            Toggle tglEyeOverlay, tglFaceOverlay, tglBodyOverlay;

            if (SCLO._isCharaOverlayBasedOnCoordinateExist) {
                bool onFromChildFlag = false;
                //分隔線
                Image line2 = UIUtility.CreateImage("line", panel.transform);
                line2.color = Color.gray;
                line2.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, baseY - 2f), new Vector2(-5f, baseY - 0.5f));

                //Chara Overlay toggle
                Toggle tglCharaOverlay = UIUtility.CreateToggle("TglCharaOverlay", panel.transform, StringResources.StringResourcesManager.GetString("charaOverlay"));
                tglCharaOverlay.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                tglCharaOverlay.GetComponentInChildren<Text>(true).color = Color.white;
                tglCharaOverlay.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, baseY - 32f), new Vector2(-5f, baseY - 6.5f));
                tglCharaOverlay.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));

                //Eye Overlay toggle
                tglEyeOverlay = UIUtility.CreateToggle("TglIrisOverlay", panel.transform, StringResources.StringResourcesManager.GetString("irisOverlay"));
                tglEyeOverlay.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                tglEyeOverlay.GetComponentInChildren<Text>(true).color = Color.white;
                tglEyeOverlay.transform.SetRect(Vector2.up, Vector2.one, new Vector2(25f, baseY - 57f), new Vector2(-5f, baseY - 32f));
                tglEyeOverlay.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));

                //Face Overlay toggle
                tglFaceOverlay = UIUtility.CreateToggle("TglFaceOverlay", panel.transform, StringResources.StringResourcesManager.GetString("faceOverlay"));
                tglFaceOverlay.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                tglFaceOverlay.GetComponentInChildren<Text>(true).color = Color.white;
                tglFaceOverlay.transform.SetRect(Vector2.up, Vector2.one, new Vector2(25f, baseY - 82f), new Vector2(-5f, baseY - 57f));
                tglFaceOverlay.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));

                //Body Overlay toggle
                tglBodyOverlay = UIUtility.CreateToggle("TglBodyOverlay", panel.transform, StringResources.StringResourcesManager.GetString("bodyOverlay"));
                tglBodyOverlay.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                tglBodyOverlay.GetComponentInChildren<Text>(true).color = Color.white;
                tglBodyOverlay.transform.SetRect(Vector2.up, Vector2.one, new Vector2(25f, baseY - 107f), new Vector2(-5f, baseY - 82f));
                tglBodyOverlay.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));

                baseY -= 107f;

                tglEyeOverlay.onValueChanged.AddListener((x) => {
                    charaOverlay[0] = x;
                    onFromChildFlag = true;
                    tglCharaOverlay.isOn |= x;
                    onFromChildFlag = false;
                });
                tglFaceOverlay.onValueChanged.AddListener((x) => {
                    charaOverlay[1] = x;
                    onFromChildFlag = true;
                    tglCharaOverlay.isOn |= x;
                    onFromChildFlag = false;
                });
                tglBodyOverlay.onValueChanged.AddListener((x) => {
                    charaOverlay[2] = x;
                    onFromChildFlag = true;
                    tglCharaOverlay.isOn |= x;
                    onFromChildFlag = false;
                });

                tglCharaOverlay.onValueChanged.AddListener((x) => {
                    if (!onFromChildFlag) {
                        tglEyeOverlay.isOn = x;
                        tglFaceOverlay.isOn = x;
                        tglBodyOverlay.isOn = x;
                    }
                });
                //tglCharaOverlay.isOn = false;
                toggleList.AddRange(new Toggle[] { tglCharaOverlay, tglEyeOverlay, tglFaceOverlay, tglBodyOverlay });
            }

            if (SCLO._isABMXExist) {
                //分隔線
                Image line3 = UIUtility.CreateImage("line", panel.transform);
                line3.color = Color.gray;
                line3.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, baseY - 2f), new Vector2(-5f, baseY - 0.5f));

                //Chara Overlay toggle
                Toggle tglReadABMX = UIUtility.CreateToggle("TglReadABMX", panel.transform, StringResources.StringResourcesManager.GetString("readABMX"));
                tglReadABMX.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                tglReadABMX.GetComponentInChildren<Text>(true).color = Color.white;
                tglReadABMX.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, baseY - 32f), new Vector2(-5f, baseY - 6.5f));
                tglReadABMX.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));

                baseY -= 32f;

                tglReadABMX.onValueChanged.AddListener((x) => {
                    readABMX = x;
                });
                tglReadABMX.isOn = true;
                toggleList.Add(tglReadABMX);
            }

            //Draw accessories panel
            panel2 = UIUtility.CreatePanel("AccessoriesTooglePanel", panel.transform);
            panel2.GetComponent<Image>().color = new Color32(80, 80, 80, 220);

            Button btnAll2 = UIUtility.CreateButton("BtnAll2", panel2.transform, "All");
            btnAll2.GetComponentInChildren<Text>(true).color = Color.white;
            btnAll2.GetComponent<Image>().color = Color.gray;
            btnAll2.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -30), new Vector2(-5f, -5f));
            btnAll2.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            //反選髮飾品Btn
            Button btnReverseHairAcc = UIUtility.CreateButton("BtnReverseHairAcc", panel2.transform, StringResources.StringResourcesManager.GetString("reverseHairAcc"));
            btnReverseHairAcc.GetComponentInChildren<Text>(true).color = Color.white;
            btnReverseHairAcc.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperCenter;
            btnReverseHairAcc.GetComponent<Image>().color = Color.gray;
            btnReverseHairAcc.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -55f), new Vector2(-5f, -30f));
            btnReverseHairAcc.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            //飾品載入模式btn
            Button btnChangeAccLoadMode = UIUtility.CreateButton("BtnChangeAccLoadMode", panel2.transform, "AccModeBtn");
            btnChangeAccLoadMode.GetComponentInChildren<Text>(true).color = Color.white;
            btnChangeAccLoadMode.GetComponentInChildren<Text>(true).alignment = TextAnchor.MiddleCenter;
            btnChangeAccLoadMode.GetComponent<Image>().color = Color.gray;
            btnChangeAccLoadMode.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -80), new Vector2(-5f, -55f));
            btnChangeAccLoadMode.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));
            panel2.gameObject.SetActive(false);

            //Lock頭髮飾品toggle
            Toggle tglHair = UIUtility.CreateToggle("lockHairAcc", panel2.transform, StringResources.StringResourcesManager.GetString("lockHairAcc"));
            tglHair.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
            tglHair.GetComponentInChildren<Text>(true).color = Color.yellow;
            tglHair.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -110f), new Vector2(-5f, -85f));
            tglHair.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));
            tglHair.isOn = lockHairAcc;
            tglHair.onValueChanged.AddListener((x) => {
                lockHairAcc = x;
            });

            //滾動元件
            ScrollRect scrollRect = UIUtility.CreateScrollView("scroll", panel2.transform);
            toggleGroup = scrollRect.content;
            scrollRect.transform.SetRect(Vector2.zero, Vector2.one, new Vector2(0f, 5f), new Vector2(0, -110f));
            scrollRect.GetComponent<Image>().color = new Color32(0, 0, 0, 0);
            foreach (Image img in scrollRect.verticalScrollbar.GetComponentsInChildren<Image>()) {
                img.color = Color.Lerp(img.color, Color.black, 0.6f);
            }
            (scrollRect.verticalScrollbar.transform as RectTransform).offsetMin = new Vector2(-16f, 0);
            scrollRect.scrollSensitivity = 30;

            //選項盤位置
            if (SCLO.insideStudio) {
                coordianteLoadPanel.transform.SetRect(Vector2.zero, Vector2.one, new Vector2(170f, -45f), new Vector2(0, -345f));
                panel.transform.SetRect(Vector2.zero, Vector2.one, new Vector2(170f, (baseY - 7.5f) - 40), new Vector2(-52, -345 - 40));
                _SetPanel(SCLO.Studio_Panel_Position);
                panel2.transform.SetRect(Vector2.one, Vector2.one, new Vector2(1f, -612.5f), new Vector2(200f, 0f));
            } else {
                coordianteLoadPanel.transform.SetRect(Vector2.zero, Vector2.zero, new Vector2(596, 1), new Vector2(766, 41));
                panel.transform.SetRect(Vector2.zero, Vector2.zero, new Vector2(777, -8), new Vector2(947, -7.5f - (baseY - 2.5f)));
                _SetPanel(SCLO.Maker_Panel_Position);
                panel2.transform.SetRect(Vector2.right, Vector2.right, new Vector2(1, 0), new Vector2(200f, 612.5f));
            }
            void _SetPanel(ConfigEntry<Vector3> c) {
                SCLO.defaultPanelPosition = panel.transform.localPosition;
                if (c.Value == Vector3.zero) c.Value = SCLO.defaultPanelPosition;
                panel.transform.localPosition = c.Value;
            }

            panel.gameObject.SetActive(false);

            //拖曳event
            Vector2 mouse = Vector2.zero;
            EventTrigger trigger = panel.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            entry.callback.AddListener((data) => {
                mouse = new Vector2(Input.mousePosition.x - panel.transform.localPosition.x, Input.mousePosition.y - panel.transform.localPosition.y);
            });
            EventTrigger.Entry entry2 = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            entry2.callback.AddListener((data) => {
                if (SCLO.insideStudio) {
                    SCLO.Studio_Panel_Position.Value = new Vector3(Input.mousePosition.x - mouse.x, Input.mousePosition.y - mouse.y, 0);
                } else {
                    SCLO.Maker_Panel_Position.Value = new Vector3(Input.mousePosition.x - mouse.x, Input.mousePosition.y - mouse.y, 0);
                }
            });
            trigger.triggers.Add(entry);
            trigger.triggers.Add(entry2);
            #endregion

            #region Button_Logic
            //Button邏輯
            btnCoordinateLoadOption.onClick.RemoveAllListeners();
            btnCoordinateLoadOption.onClick.AddListener(() => {
                if (null != panel) {
                    bool active = !panel.IsActive();
                    panel.gameObject.SetActive(active);

                    //Maker: 修改服裝選擇器下方的勾選列
                    if (!SCLO.insideStudio) {
                        GameObject stackLoadPanel = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow/WinRect/CoordinateLoad/Select");
                        stackLoadPanel.GetComponentsInChildren<Toggle>().ToList().ForEach(tgl => tgl.isOn = true);  //全勾上tmpChara才能完整載入
                        stackLoadPanel.SetActive(!active);

                        //在CoordinateLoad OnEnable時改變ListArea長度
                        GameObject go = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow/WinRect/CoordinateLoad");
                        if (null == go.GetComponent(typeof(OnEnableListener))) {
                            OnEnableListener onEnableListener = (OnEnableListener)go.AddComponent(typeof(OnEnableListener));
                            onEnableListener.OnEnableEvent += delegate {
                                GameObject listView = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow/WinRect/ListArea");
                                listView.transform.SetRect(Vector2.up, Vector2.up, new Vector2(4, Patches.panel.IsActive() ? -880 : -834), new Vector2(772, -72));
                                //Logger.LogDebug("OnEnable: " + Patches.panel.IsActive());
                            };
                        }
                        go.SetActive(false);
                        go.SetActive(true);
                    }
                }
            });

            btnAll.onClick.RemoveAllListeners();
            btnAll.onClick.AddListener(() => {
                if (toggleList.All(x => x.isOn == true)) {
                    foreach (Toggle x in toggleList) { x.isOn = false; }
                } else {
                    foreach (Toggle x in toggleList) { x.isOn = true; }
                }
            });
            btnAll2.onClick.RemoveAllListeners();
            btnAll2.onClick.AddListener(() => {
                if (tgls2.All(x => x.isOn == true)) {
                    foreach (Toggle x in tgls2) { x.isOn = false; }
                } else {
                    foreach (Toggle x in tgls2) { x.isOn = true; }
                }
            });
            btnClearAcc.onClick.RemoveAllListeners();
            btnClearAcc.onClick.AddListener(() => {
                if (SCLO.insideStudio) {
                    IEnumerable<OCIChar> array = Singleton<GuideObjectManager>.Instance.selectObjectKey
                        .Select(p => (OCIChar)Studio.Studio.GetCtrlInfo(p))
                        .Where(p => null != p);
                    foreach (OCIChar ocichar in array) {
                        CoordinateLoad.ClearAccessories(ocichar.charInfo);
                    }
                } else {
                    CoordinateLoad.ClearAccessories(Singleton<CustomBase>.Instance.chaCtrl);
                    Singleton<CustomBase>.Instance.updateCustomUI = true;
                }

                Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_s);
                Logger.LogDebug("Clear accessories Finish");
            });
            btnReverseHairAcc.onClick.RemoveAllListeners();
            btnReverseHairAcc.onClick.AddListener(() => {
                CoordinateLoad.MakeTmpChara((_) => {
                    CoordinateLoad.tmpChaCtrl.StopAllCoroutines();
                    for (int i = 0; i < MoreAccessories_Support.GetAccessoriesAmount(CoordinateLoad.tmpChaCtrl.chaFile); i++) {
                        if (i < tgls2.Length) {
                            if (CoordinateLoad.IsHairAccessory(CoordinateLoad.tmpChaCtrl, i)) {
                                tgls2[i].isOn = !tgls2[i].isOn;
                                Logger.LogDebug($"Reverse Hair Acc.: {i}");
                            }
                        }
                    }
                    Singleton<Manager.Character>.Instance.DeleteChara(CoordinateLoad.tmpChaCtrl);
                    CoordinateLoad.tmpChaCtrl = null;
                    Logger.LogDebug($"Delete Temp Chara");
                    Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_s);
                    Logger.LogDebug("Reverse Hair Acc. toggles.");
                });
            });

            btnChangeAccLoadMode.onClick.RemoveAllListeners();
            btnChangeAccLoadMode.onClick.AddListener(() => {
                addAccModeFlag = !addAccModeFlag;
                btnChangeAccLoadMode.GetComponentInChildren<Text>().text =
                    addAccModeFlag ?
                    StringResources.StringResourcesManager.GetString("addMode") :
                    StringResources.StringResourcesManager.GetString("replaceMode");
                Logger.LogDebug("Set add accessories mode to " + (addAccModeFlag ? "add" : "replace") + " mode");
            });
            addAccModeFlag = true;
            btnChangeAccLoadMode.onClick.Invoke();
            #endregion

            //Logger.LogDebug("Draw UI Finish");

            //Block Maker Load Button
            if (!SCLO.insideStudio) {
                Button btnLoad = (__instance.GetField("fileWindow") as CustomFileWindow).btnCoordeLoadLoad;
                btnLoad.onClick.RemoveAllListeners();
                btnLoad.onClick.AddListener(OnClickLoadPostfix);
            }
        }

        //選擇衣裝
        internal static void OnSelectPostfix(object __instance) {
            if (null == panel2) {
                return;
            }
            if (SCLO.insideStudio) {
                coordinatePath = (__instance.GetField("fileSort") as CharaFileSort)?.selectPath ?? "";
            } else {
                CustomFileWindow = (CustomFileWindow)__instance.GetField("fileWindow");
                object customFileInfoComponent = __instance.GetField("listCtrl")?.Invoke("GetSelectTopItem");

                //OnDeslect in Maker
                if (null == customFileInfoComponent) return;

                if (null != customFileInfoComponent.GetType().GetField("info")) {
                    coordinatePath = customFileInfoComponent.GetField("info").GetField("FullPath") as string ?? "";
                } else {
                    //KKP
                    coordinatePath = customFileInfoComponent.GetProperty("info").GetProperty("FullPath") as string ?? "";
                }
            }
            Logger.LogDebug($"Coordinate Path: {coordinatePath}");
            if (string.IsNullOrEmpty(coordinatePath)) return;

            //取得飾品清單
            ChaFileCoordinate tmpChaFileCoordinate = new ChaFileCoordinate();
            tmpChaFileCoordinate.LoadFile(coordinatePath);

            List<string> accNames = new List<string>();

            accNames.AddRange(tmpChaFileCoordinate.accessory.parts.Select(x => CoordinateLoad.GetNameFromIDAndType(x.id, (ChaListDefine.CategoryNo)x.type)));

            if (SCLO._isMoreAccessoriesExist) {
                accNames.AddRange(MoreAccessories_Support.LoadMoreAcc(tmpChaFileCoordinate));
            }

            // 檢查選中的服裝是否有要綁定飾品的插件資料
            boundAcc = false;
            foreach (string guid in SCLO.pluginBoundAccessories)
            {
                if (null != ExtendedSave.GetExtendedDataById(tmpChaFileCoordinate, guid))
                {
                    boundAcc = true;
                    Logger.LogWarning($"The accessories option is disabled due to the plugin data ({guid}) found on selected coordinate ({tmpChaFileCoordinate.coordinateName})");
                    break;
                }
            }

            foreach (Toggle tgl in toggleGroup.gameObject.GetComponentsInChildren<Toggle>()) {
                GameObject.Destroy(tgl.gameObject);
            }
            List<Toggle> tmpTgls = new List<Toggle>();
            foreach (string accName in accNames) {
                Toggle toggle = UIUtility.CreateToggle(Enum.GetValues(typeof(SCLO.ClothesKind)).GetValue(9).ToString(), toggleGroup.transform, accName);
                toggle.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                toggle.GetComponentInChildren<Text>(true).color = Color.white;
                toggle.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -25f * (tmpTgls.Count + 1)), new Vector2(0f, -25f * tmpTgls.Count));
                toggle.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));
                toggle.isOn = true;
                toggle.interactable = !boundAcc;
                tmpTgls.Add(toggle);
            }
            tgls2 = tmpTgls.ToArray();
            toggleGroup.transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(0, -(25f * (accNames.Count - 20))), new Vector2(0, 0));

            panel2.gameObject.SetActive(tgls[(int)SCLO.ClothesKind.accessories].isOn);
            //Logger.LogDebug("Onselect");
        }

        [HarmonyPriority(Priority.First)]
        internal static bool OnClickLoadPrefix() => false;

        //Load衣裝時觸發
        internal static void OnClickLoadPostfix() {
            if (CoordinateLoad.oCICharQueue.Count != 0) return;

            Logger.LogDebug("Studio Coordinate Load Option Start");

            //bool isAllTrueFlag = true;
            bool isAllFalseFlag = true;

            List<bool> toggleBoolList = tgls.Select(x => x.isOn).ToList();
            toggleBoolList.AddRange(charaOverlay);
            toggleBoolList.Add(readABMX);

            foreach (bool b in toggleBoolList) {
                //isAllTrueFlag &= b;
                isAllFalseFlag &= !b;
            }

            ////飾品細項只反應到AllTrue檢查
            //foreach (Toggle tgl in tgls2) {
            //    isAllTrueFlag &= tgl.isOn;
            //}

            //全沒選
            if (panel.IsActive() && isAllFalseFlag) {
                Logger.LogInfo("No Toggle selected, skip loading coordinate");
                Logger.LogDebug("Studio Coordinate Load Option Finish");
                return;
            }

            if (SCLO.insideStudio) {
                IEnumerable<OCIChar> array = GuideObjectManager.Instance.selectObjectKey
                    .Select(p => (OCIChar)Studio.Studio.GetCtrlInfo(p))
                    .Where(p => null != p).OfType<OCIChar>();
                if (array.Count() == 0) {
                    //沒選中人
                    Logger.LogMessage("No available characters selected");
                    Logger.LogDebug("Studio Coordinate Load Option Finish");
                } else if (!panel.IsActive()/* || (isAllTrueFlag && !lockHairAcc && !addAccModeFlag)*/) {
                    //未展開Panel
                    Logger.LogInfo("Use original game function");
                    foreach (OCIChar ocichar in array) {
                        ocichar.LoadClothesFile(coordinatePath);
                    }
                    Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_s);
                } else {
                    //建立tmpChara並等待載入完成，然後再呼叫換衣
                    CoordinateLoad.totalAmount = array.Count();
                    CoordinateLoad.oCICharQueue = new Queue<OCIChar>(array);
                    CoordinateLoad.finishedCount = 0;
                    CoordinateLoad.MakeTmpChara(CoordinateLoad.ChangeCoordinate);
                }
            } else {
                if (!panel.IsActive()/* || (isAllTrueFlag && !lockHairAcc && !addAccModeFlag)*/) {
                    //未展開Panel
                    Logger.LogInfo("Use original game function");
                    ChaControl chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
                    bool flag = CustomFileWindow.tglCoordeLoadClothes && CustomFileWindow.tglCoordeLoadClothes.isOn;
                    bool flag2 = CustomFileWindow.tglCoordeLoadAcs && CustomFileWindow.tglCoordeLoadAcs.isOn;
                    byte[] bytes = MessagePackSerializer.Serialize<ChaFileClothes>(chaCtrl.nowCoordinate.clothes);
                    byte[] bytes2 = MessagePackSerializer.Serialize<ChaFileAccessory>(chaCtrl.nowCoordinate.accessory);
                    chaCtrl.nowCoordinate.LoadFile(coordinatePath);
                    if (!flag) {
                        chaCtrl.nowCoordinate.clothes = MessagePackSerializer.Deserialize<ChaFileClothes>(bytes);
                    }
                    if (!flag2) {
                        chaCtrl.nowCoordinate.accessory = MessagePackSerializer.Deserialize<ChaFileAccessory>(bytes2);
                    }

                    //強制重整，修正ABMX在讀取衣裝後，不會正常反應
                    new ABMX_CCFCSupport(chaCtrl).SetExtDataFromController();

                    chaCtrl.Reload(false, true, true, true);
                    chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)chaCtrl.chaFile.status.coordinateType);
                    Singleton<CustomBase>.Instance.updateCustomUI = true;
                    Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_s);
                } else {
                    //建立tmpChara並等待載入完成，然後再呼叫換衣
                    CoordinateLoad.MakeTmpChara(CoordinateLoad.ChangeCoordinate);
                }
            }
        }
    }

    class CoordinateLoad {
        private static readonly ManualLogSource Logger = SCLO.Logger;
        internal static int totalAmount = 0;
        internal static Queue<OCIChar> oCICharQueue = new Queue<OCIChar>();
        internal static int finishedCount = 0;
        internal static ChaControl tmpChaCtrl;
        private static ChaFileCoordinate backupTmpCoordinate;
        private static int forceCleanCount = SCLO.FORCECLEANCOUNT;
        private static COBOC_CCFCSupport coboc;
        private static HairAccessoryCustomizer_CCFCSupport hairacc;

        internal static void Update() {
            if (null != tmpChaCtrl) {
                forceCleanCount--;
                if (forceCleanCount <= 0) {
                    End(forceClean: true);
                }
            }
        }

        internal static void MakeTmpChara(Action<object> callback) {
            forceCleanCount = SCLO.FORCECLEANCOUNT;

            ChaControl chaCtrl = null;
            OCIChar ocichar = null;
            if (SCLO.insideStudio) {
                if (oCICharQueue.Count != 0) {
                    ocichar = oCICharQueue.Dequeue();

                    //Bone
                    foreach (OCIChar.BoneInfo boneInfo in ocichar.listBones.Where(b => b.boneGroup == OIBoneInfo.BoneGroup.Hair)) {
                        Singleton<GuideObjectManager>.Instance.Delete(boneInfo.guideObject, true);
                    }
                    ocichar.listBones = ocichar.listBones.Where(b => b.boneGroup != OIBoneInfo.BoneGroup.Hair).ToList<OCIChar.BoneInfo>();
                    ocichar.hairDynamic = null;
                    ocichar.skirtDynamic = null;
                    chaCtrl = ocichar.charInfo;
                }
                // 如果是ReverseHairAcc功能，chaCtrl就會保持null
                // 後續用到chaCtrl都應該檢核
            } else {
                chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
            }

            // Save ExtendedData to ChaControl.nowCoordinate
            // KKAPI prevents plugins from storing coordinates outside of Maker, I pray here that users will not edit the coordinate in any way in Studio, and the character status and extended data must be consistent.
            if (!SCLO.insideStudio)
                Extension.Reflection.InvokeStatic(typeof(ExtendedSave), "CoordinateWriteEvent", new object[] { chaCtrl.nowCoordinate });

            //KK_COBOC
            coboc = new COBOC_CCFCSupport(chaCtrl);
            if (SCLO._isCharaOverlayBasedOnCoordinateExist && null != chaCtrl) {
                //coboc.SetExtDataFromController();
                coboc.GetControllerAndBackupData(targetChaCtrl: chaCtrl);
                coboc.GetIrisDisplaySide();
            }
            hairacc = new HairAccessoryCustomizer_CCFCSupport(chaCtrl);
            if (SCLO._isHairAccessoryCustomizerExist && null != chaCtrl) {
                hairacc.GetControllerAndBackupData(targetChaCtrl: chaCtrl);
                HairAccessoryCustomizer_CCFCSupport.UpdateBlock = true;
            }

            backupTmpCoordinate = new ChaFileCoordinate();
            backupTmpCoordinate.LoadFile(Patches.coordinatePath);

            //丟到Camera外面就看不見了
            tmpChaCtrl = Singleton<Manager.Character>.Instance.CreateFemale(Camera.main.gameObject, -1);
            tmpChaCtrl.gameObject.transform.localPosition = new Vector3(-100, -100, -100);
            tmpChaCtrl.Load(true);
            tmpChaCtrl.fileParam.lastname = "黑肉";
            tmpChaCtrl.fileParam.firstname = "舔舔";
            tmpChaCtrl.fileStatus.coordinateType = chaCtrl?.fileStatus.coordinateType ?? 0;
            if (hairacc.isExist && null != chaCtrl) {
                //取得BackupData
                hairacc.GetControllerAndBackupData(sourceChaCtrl: tmpChaCtrl, sourceCoordinate: backupTmpCoordinate);

                //禁用ColorMatch: 這在Maker中必要 (在Studio會被內部檢核阻擋)
                //在Maker中，若原本的HairAccData有啟用ColorMatch，會在換完Acc後把飾品原生顏色回寫為HairMatchColor
                //所以在此取得Backup後、開始換衣前將所有的ColorMatch禁用
                hairacc.DisableColorMatches();

                //將Controller中之HairAccessories拷貝到tmpChaCtrl
                //這是Ref Copy，這是MakerAPI並無區分多ChaControl的對應
                //且Maker換衣時無法呼叫HairAccCusController.LoadData()，只能呼叫LoadCoordinate()
                //故必須在tmpChaCtrl上完整複製chaCtrl資料，並在換裝完後由Coordinate寫回
                hairacc.SetExtDataFromController();
                hairacc.CopyHairAccBetweenControllers(chaCtrl, tmpChaCtrl);
                hairacc.CopyAllHairAccExtdata(chaCtrl, tmpChaCtrl);
            }

            tmpChaCtrl.StartCoroutine(LoadTmpChara());

            IEnumerator LoadTmpChara() {
                //KCOX在讀衣裝前需要先做Reload初始化
                tmpChaCtrl.Reload();
                yield return new WaitUntil(delegate { return CheckPluginPrepared(); });

                tmpChaCtrl.nowCoordinate.LoadFile(Patches.coordinatePath);
                tmpChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)tmpChaCtrl.fileStatus.coordinateType);
                tmpChaCtrl.Reload(false, true, true, true);

                forceCleanCount = SCLO.FORCECLEANCOUNT;

                yield return new WaitUntil(delegate { return CheckPluginPrepared(backupTmpCoordinate); });

                callback?.Invoke((object)ocichar ?? chaCtrl);
            }

            bool CheckPluginPrepared(ChaFileCoordinate backCoordinate = null) =>
                null != tmpChaCtrl &&
                new KCOX_CCFCSupport(tmpChaCtrl).CheckControllerPrepared() &&
                (null == backCoordinate || new HairAccessoryCustomizer_CCFCSupport(tmpChaCtrl).CheckControllerPrepared(backCoordinate)) &&
                new MaterialEditor_CCCFCSupport(tmpChaCtrl).CheckControllerPrepared();
        }

        internal static void ChangeCoordinate(object OcicharOrChaCtrl) {
            tmpChaCtrl.StopAllCoroutines();

            ChaControl chaCtrl;
            OCIChar ocichar = null;
            try {
                if (SCLO.insideStudio) {
                    ocichar = OcicharOrChaCtrl as OCIChar;
                    chaCtrl = ocichar.charInfo;
                } else {
                    chaCtrl = OcicharOrChaCtrl as ChaControl;
                }
            } catch (Exception) { return; };
            chaCtrl.StopAllCoroutines();

            // 檢查自己身上是否有要綁定飾品的插件資料
            foreach (var guid in SCLO.pluginBoundAccessories)
            {
                if (null != ExtendedSave.GetExtendedDataById(chaCtrl.nowCoordinate, guid))
                {
                    Patches.boundAcc = true;
                    Patches.tgls2.ToList().ForEach(tg => {
                        tg.isOn = true;
                        tg.interactable = false;
                    });
                    Logger.Log(LogLevel.Message | LogLevel.Warning, $"The accessories option is disabled due to the plugin data ({guid}) found on your character {chaCtrl.fileParam.fullname}");
                    break;
                }
            }

            KCOX_CCFCSupport kcox = new KCOX_CCFCSupport(chaCtrl);
            ABMX_CCFCSupport abmx = new ABMX_CCFCSupport(chaCtrl);
            MaterialEditor_CCCFCSupport me = new MaterialEditor_CCCFCSupport(chaCtrl);

            //Load Coordinate
            Queue<int> accQueue = new Queue<int>();

            foreach (Toggle tgl in Patches.tgls) {
                object tmpToggleType = null;
                int kind = -2;
                try {
                    tmpToggleType = Enum.Parse(typeof(SCLO.ClothesKind), tgl.name);
                    kind = Convert.ToInt32(tmpToggleType);
                } catch (ArgumentException) {
                    kind = -1;
                }
                if (tgl.isOn) {
                    if (kind == 9) {
                        if(Patches.boundAcc)
                        {
                            ClearAccessories(chaCtrl);
                        }
                        if (SCLO._isMoreAccessoriesExist) {
                            MoreAccessories_Support.CopyMoreAccessories(tmpChaCtrl, chaCtrl);
                        } else {
                            //Copy accessories
                            ChaFileAccessory.PartsInfo[] chaCtrlAccParts = chaCtrl.nowCoordinate.accessory.parts;
                            ChaFileAccessory.PartsInfo[] tmpCtrlAccParts = tmpChaCtrl.nowCoordinate.accessory.parts;

                            ChangeAccessories(tmpChaCtrl, tmpCtrlAccParts, chaCtrl, chaCtrlAccParts, accQueue);

                            //accQueue內容物太多，報告後捨棄
                            while (accQueue.Count > 0) {
                                int slot = accQueue.Dequeue();
                                ChaFileAccessory.PartsInfo part = tmpCtrlAccParts[slot];
                                Logger.LogMessage("Accessories slot is not enough! Discard " + GetNameFromIDAndType(part.id, (ChaListDefine.CategoryNo)part.type));
                            }
                        }
                        chaCtrl.ChangeAccessory(true);
                        Logger.LogDebug("->Changed: " + tgl.name);
                    } else if (kind >= 0) {
                        //if (SCLO._isMaterialEditorExist)
                        //    MaterialEditor_Support.RemoveMaterialEditorData(chaCtrl, kind, chaCtrl.objClothes[kind], MaterialEditor_Support.ObjectType.Clothing);

                        //Change clothes
                        byte[] tmp = MessagePackSerializer.Serialize<ChaFileClothes.PartsInfo>(tmpChaCtrl.nowCoordinate.clothes.parts[kind]);
                        chaCtrl.nowCoordinate.clothes.parts[kind] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(tmp);
                        chaCtrl.ChangeClothes(kind, tmpChaCtrl.nowCoordinate.clothes.parts[kind].id, tmpChaCtrl.nowCoordinate.clothes.subPartsId[0], tmpChaCtrl.nowCoordinate.clothes.subPartsId[1], tmpChaCtrl.nowCoordinate.clothes.subPartsId[2], true);

                        if (kcox.isExist)
                            kcox.CopyKCOXData(tmpChaCtrl, kind);

                        if (me.isExist)
                            me.CopyMaterialEditorData(tmpChaCtrl, kind, kind, chaCtrl.objClothes[kind], MaterialEditor_CCCFCSupport.ObjectType.Clothing);

                        Logger.LogDebug("->Changed: " + tgl.name + " / ID: " + chaCtrl.nowCoordinate.clothes.parts[kind].id);
                    }
                }
            }

            //存入至ExtendedData，然後Reload---

            //HairAcc
            if (hairacc.isExist) {
                //寫入 (即使未載入Acc，也需要將一開始的備份寫回)
                hairacc.SetToExtData();
                hairacc.SetDataToCoordinate();

                //讀出驗證
                hairacc.GetCoordinateData(hairacc.GetDataFromExtData(chaCtrl), (ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType, out Dictionary<int, object> nowCoor);
                //if (null != nowCoor) {
                //    Logger.LogDebug($"->Hair Count {nowCoor.Count}: {string.Join(",", nowCoor.Select(x => x.Key.ToString()).ToArray())}");
                //}

                HairAccessoryCustomizer_CCFCSupport.UpdateBlock = false;

                //Load to controller (Maker只有從Coordinate存才能運作)
                hairacc.SetControllerFromExtData();
                hairacc.SetControllerFromCoordinate();
            }

            //Material Editor
            if (me.isExist)
                me.SetExtDataFromController();

            //KCOX
            if (kcox.isExist) {
                kcox.SetExtDataFromController();
                chaCtrl.StartCoroutine(kcox.Update());
            }

            //MoreAcc
            if (SCLO._isMoreAccessoriesExist) {
                chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType);
                MoreAccessories_Support.SetExtDataFromPlugin(chaCtrl);
                MoreAccessories_Support.Update();
                Logger.LogDebug($"Acc Count : {MoreAccessories_Support.GetAccessoriesAmount(chaCtrl.chaFile)}");
            }

            //ABMX
            if (abmx.isExist) {
                if (Patches.readABMX) abmx.CopyABMXData(tmpChaCtrl);
                abmx.SetExtDataFromController();
            }

            // 處理要綁定飾品的插件資料
            if (Patches.boundAcc && Patches.tgls[9].isOn)
            {
                foreach (var guid in SCLO.pluginBoundAccessories)
                {
                    ExtendedSave.SetExtendedDataById(chaCtrl.nowCoordinate, guid, ExtendedSave.GetExtendedDataById(backupTmpCoordinate, guid));
                }
            }

            if (!SCLO.insideStudio)
                Singleton<CustomBase>.Instance.updateCustomUI = true;

            //Reload
            chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType);
            chaCtrl.ChangeCoordinateType((ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType, false);
            //chaCtrl.Reload();   //全false的Reload會觸發KKAPI的hook
            chaCtrl.StartCoroutine(_read());
            IEnumerator _read()
            {
                yield return null;
                Extension.Reflection.InvokeStatic(typeof(ExtendedSave), "CoordinateReadEvent", new object[] { chaCtrl.nowCoordinate });
            }

            //---存入至ExtendedData，然後Reload

            //KK_COBOC
            if (kcox.isExist && coboc.isExist) {
                if (Patches.charaOverlay.ToList().Where((x) => x).Count() > 0 && null != ExtendedSave.GetExtendedDataById(backupTmpCoordinate, coboc.GUID)) {
                    coboc.CopyCurrentCharaOverlayByController(tmpChaCtrl, Patches.charaOverlay);
                    coboc.SetExtDataFromController();
                } else {
                    coboc.SetIrisDisplaySide();
                    Logger.LogDebug("Skip load CharaOverlay");
                }
            }

            finishedCount++;

            if (SCLO.insideStudio && null != ocichar) {
                //Bone & FK,IK
                chaCtrl.UpdateBustSoftnessAndGravity();
                AddObjectAssist.InitHairBone(ocichar, Singleton<Info>.Instance.dicBoneInfo);
                ocichar.hairDynamic = AddObjectFemale.GetHairDynamic(ocichar.charInfo.objHair);
                ocichar.skirtDynamic = AddObjectFemale.GetSkirtDynamic(ocichar.charInfo.objClothes);
                ocichar.InitFK(null);
                foreach (var tmp in FKCtrl.parts.Select((OIBoneInfo.BoneGroup p, int i2) => new { p, i2 })) {
                    ocichar.ActiveFK(tmp.p, ocichar.oiCharInfo.activeFK[tmp.i2], ocichar.oiCharInfo.activeFK[tmp.i2]);
                }
                ocichar.ActiveKinematicMode(OICharInfo.KinematicMode.FK, ocichar.oiCharInfo.enableFK, true);
                ocichar.UpdateFKColor(new OIBoneInfo.BoneGroup[] { OIBoneInfo.BoneGroup.Hair });
                //State
                ocichar.ChangeEyesOpen(ocichar.charFileStatus.eyesOpenMax);
                ocichar.ChangeBlink(ocichar.charFileStatus.eyesBlink);
                ocichar.ChangeMouthOpen(ocichar.oiCharInfo.mouthOpen);

                Logger.LogInfo($"Loaded: {finishedCount}/{totalAmount}");
            } else {
                Singleton<CustomBase>.Instance.updateCustomUI = true;
            }

            forceCleanCount = SCLO.FORCECLEANCOUNT;
            End();
        }

        private static void End(bool forceClean = false) {
            hairacc.ClearBackup();
            hairacc = null;
            coboc.ClearBackup();
            coboc = null;

            tmpChaCtrl.StopAllCoroutines();
            backupTmpCoordinate = null;
            Singleton<Manager.Character>.Instance.DeleteChara(tmpChaCtrl);
            tmpChaCtrl = null;
            Logger.LogDebug($"Delete Temp Chara");

            if (oCICharQueue.Count > 0 && !forceClean && SCLO.insideStudio) {
                MakeTmpChara(ChangeCoordinate);
            } else {
                oCICharQueue.Clear();
                totalAmount = 0;
                Logger.LogInfo($"Load End");

                if (forceClean) {
                    Logger.Log(LogLevel.Message | LogLevel.Error | LogLevel.Warning, "Coordinate Load ended unexpectedly.");
                    Logger.Log(LogLevel.Message | LogLevel.Error, "Please call the original game function manually instead.");
                    Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.cancel);
                } else {
                    Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_s);
                }
            }
        }

        public static void ChangeAccessories(ChaControl sourceChaCtrl, ChaFileAccessory.PartsInfo[] sourceParts, ChaControl targetChaCtrl, ChaFileAccessory.PartsInfo[] targetParts, Queue<int> accQueue) {
            accQueue.Clear();
            //防呆檢查targetParts欄位不能小於sourceParts
            if (sourceParts.Length > targetParts.Length) {
                Logger.LogError($"ChangeAccessories targetParts < sourceParts");
                return;
            }

            bool isAllFalseFlag = true;
            foreach (bool b in Patches.tgls2.Select(x => x.isOn).ToArray()) {
                isAllFalseFlag &= !b;
            }
            if (isAllFalseFlag && accQueue.Count == 0) {
                Logger.LogDebug("Load Accessories All False");
                Logger.LogDebug("Load Accessories Finish");
                return;
            }
            Logger.LogDebug($"Acc Count : {Patches.tgls2.Length}");

            MaterialEditor_CCCFCSupport me = new MaterialEditor_CCCFCSupport(targetChaCtrl);

            for (int i = 0; i < targetParts.Length && i < Patches.tgls2.Length; i++) {
                if ((bool)Patches.tgls2[i]?.isOn) {
                    if (Patches.addAccModeFlag) {
                        //增加模式
                        if (targetParts[i].type == 120) {
                            DoChangeAccessory(i, i);
                        } else {
                            EnQueue();
                        }
                    } else {
                        //取代模式
                        if (IsHairAccessory(targetChaCtrl, i) && Patches.lockHairAcc) {
                            EnQueue();
                        } else {
                            //如果是取代模式且非髮飾品則取代
                            DoChangeAccessory(i, i);
                        }
                    }
                } else {
                    //如果沒勾選就不改變
                    continue;
                }

                void EnQueue() {
                    if (sourceParts[i]?.type == 120) {
                        Logger.LogDebug($"->Lock: Acc{i} / Part: {(ChaListDefine.CategoryNo)targetParts[i].type} / ID: {targetParts[i].id}");
                        Logger.LogDebug($"->Pass: Acc{i} / Part: {(ChaListDefine.CategoryNo)sourceParts[i].type} / ID: {sourceParts[i].id}");
                    } else {
                        Logger.LogDebug($"->Lock: Acc{i} / Part: {(ChaListDefine.CategoryNo)targetParts[i].type} / ID: {targetParts[i].id}");
                        Logger.LogDebug($"->EnQueue: Acc{i} / Part: {(ChaListDefine.CategoryNo)sourceParts[i].type} / ID: {sourceParts[i].id}");
                        accQueue.Enqueue(i);
                    }
                }
            }

            //遍歷空欄dequeue accQueue
            for (int j = 0; j < targetParts.Length && accQueue.Count > 0; j++) {
                if (targetParts[j].type == 120) {
                    int slot = accQueue.Dequeue();
                    DoChangeAccessory(slot, j);
                    Logger.LogDebug($"->DeQueue: Acc{j} / Part: {(ChaListDefine.CategoryNo)targetParts[j].type} / ID: {targetParts[j].id}");
                } //else continue;
            }

            /// <summary>
            /// 換飾品
            /// </summary>
            /// <param name="sourceSlot">來源slot</param>
            /// <param name="targetSlot">目標slot</param>
            void DoChangeAccessory(int sourceSlot, int targetSlot) {
                //來源目標都空著就跳過
                if (sourceParts[sourceSlot].type == 120 && targetParts[targetSlot].type == 120) {
                    Logger.LogDebug($"->BothEmpty: SourceAcc{sourceSlot}, TargetAcc{targetSlot}");
                    return;
                }

                if (me.isExist) {
                    me.RemoveMaterialEditorData(targetSlot, GetChaAccessoryComponent(targetChaCtrl, targetSlot)?.gameObject, MaterialEditor_CCCFCSupport.ObjectType.Accessory);
                }

                byte[] tmp = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(sourceParts[sourceSlot]);
                targetParts[targetSlot] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tmp);

                if (hairacc.isExist) {
                    hairacc.CopyHairAcc(sourceChaCtrl, sourceSlot, targetChaCtrl, targetSlot);
                }

                if (me.isExist) {
                    me.CopyMaterialEditorData(sourceChaCtrl, sourceSlot, targetSlot, GetChaAccessoryComponent(sourceChaCtrl, sourceSlot)?.gameObject, MaterialEditor_CCCFCSupport.ObjectType.Accessory);
                }
                Logger.LogDebug($"->Changed: Acc{targetSlot} / Part: {(ChaListDefine.CategoryNo)targetParts[targetSlot].type} / ID: {targetParts[targetSlot].id}");
            }
        }

        public static void ClearAccessories(ChaControl chaCtrl) {
            for (int i = 0; i < 20; i++) {
                if (!(IsHairAccessory(chaCtrl, i) && Patches.lockHairAcc)) {
                    chaCtrl.nowCoordinate.accessory.parts[i] = new ChaFileAccessory.PartsInfo();
                } else {
                    Logger.LogDebug($"Keep HairAcc{i}: {chaCtrl.nowCoordinate.accessory.parts[i].id}");
                }
            }
            if (SCLO._isMoreAccessoriesExist) {
                MoreAccessories_Support.ClearMoreAccessoriesData(chaCtrl);
            }
            chaCtrl.ChangeAccessory(true);
            chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType);
            chaCtrl.ChangeCoordinateTypeAndReload(false);
        }

        /// <summary>
        /// 傳入ID和Type查詢名稱
        /// </summary>
        /// <param name="id">查詢ID</param>
        /// <param name="type">查詢Type</param>
        /// <returns>名稱</returns>
        internal static string GetNameFromIDAndType(int id, ChaListDefine.CategoryNo type) {
            ChaListControl chaListControl = Singleton<Manager.Character>.Instance.chaListCtrl;
            Logger.LogDebug($"Find Accessory id / type: {id} / {type}");

            string name = "";
            if (type == (ChaListDefine.CategoryNo)120) {
                name = StringResources.StringResourcesManager.GetString("empty");
            }
            if (null == name || "" == name) {
                name = chaListControl.GetListInfo(type, id)?.Name;
            }
            if (null == name || "" == name) {
                name = StringResources.StringResourcesManager.GetString("unreconized");
            }

            return name;
        }

        /// <summary>
        /// 檢查是否為頭髮飾品
        /// </summary>
        /// <param name="chaCtrl">對象角色</param>
        /// <param name="index">飾品欄位index</param>
        /// <returns></returns>
        public static bool IsHairAccessory(ChaControl chaCtrl, int index) => GetChaAccessoryComponent(chaCtrl, index)?.gameObject.GetComponent<ChaCustomHairComponent>() != null;

        /// <summary>
        /// 取得ChaAccessoryComponent
        /// </summary>
        /// <param name="chaCtrl"></param>
        /// <param name="index"></param>
        /// <returns>ChaAccessoryComponent</returns>
        public static ChaAccessoryComponent GetChaAccessoryComponent(ChaControl chaCtrl, int index) {
            if (SCLO._isMoreAccessoriesExist) {
                return MoreAccessories_Support.GetChaAccessoryComponent(chaCtrl, index);
            } else {
                return chaCtrl.GetAccessoryComponent(index);
            }
        }
    }
}
