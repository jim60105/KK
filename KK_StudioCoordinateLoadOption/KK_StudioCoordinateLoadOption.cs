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
using ExIni;
using Extension;
using HarmonyLib;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KK_StudioCoordinateLoadOption {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    [BepInDependency("KCOX", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("KKABMX.Core", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.joan6694.illusionplugins.moreaccessories", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.deathweasel.bepinex.materialeditor", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.deathweasel.bepinex.hairaccessorycustomizer", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.jim60105.kk.charaoverlaysbasedoncoordinate", BepInDependency.DependencyFlags.SoftDependency)]
    public class KK_StudioCoordinateLoadOption : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Coordinate Load Option";
        internal const string GUID = "com.jim60105.kk.studiocoordinateloadoption";
        internal const string PLUGIN_VERSION = "20.04.27.0";
        internal const string PLUGIN_RELEASE_VERSION = "3.2.2.2";

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            UIUtility.Init();
            Harmony harmonyInstance = HarmonyWrapper.PatchAll(typeof(Patches));
            harmonyInstance.PatchAll(typeof(MoreAccessories_Support));

            Type CostumeInfoType = typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic);
            harmonyInstance.Patch(CostumeInfoType.GetMethod("Init", AccessTools.all),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.InitPostfix)));
            harmonyInstance.Patch(CostumeInfoType.GetMethod("OnClickLoad", AccessTools.all),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Patches.OnClickLoadPrefix)),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.OnClickLoadPostfix)));
            harmonyInstance.Patch(CostumeInfoType.GetMethod("OnSelect", AccessTools.all),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.OnSelectPostfix)));
        }

        public static bool _isKCOXExist = false;
        public static bool _isABMXExist = false;
        public static bool _isMoreAccessoriesExist = false;
        public static bool _isMaterialEditorExist = false;
        public static bool _isHairAccessoryCustomizerExist = false;
        public static bool _isCharaOverlayBasedOnCoordinateExist = false;

        public void Start() {
            _isKCOXExist = KCOX_Support.LoadAssembly();
            _isABMXExist = ABMX_Support.LoadAssembly();
            _isMoreAccessoriesExist = MoreAccessories_Support.LoadAssembly();
            _isMaterialEditorExist = MaterialEditor_Support.LoadAssembly();
            _isHairAccessoryCustomizerExist = HairAccessoryCustomizer_Support.LoadAssembly();
            _isCharaOverlayBasedOnCoordinateExist = COBOC_Support.LoadAssembly();

            StringResources.StringResourcesManager.SetUICulture();
        }

        public void Update() => Patches.Update();
    }

    class Patches {
        private static readonly ManualLogSource Logger = KK_StudioCoordinateLoadOption.Logger;
        private static CharaFileSort charaFileSort;

        public static readonly string[] MainClothesNames = {
            "ct_clothesTop",
            "ct_clothesBot",
            "ct_bra",
            "ct_shorts",
            "ct_gloves",
            "ct_panst",
            "ct_socks",
            "ct_shoes_inner",
            "ct_shoes_outer"
        };

        public static readonly string[] SubClothesNames = {
            "ct_top_parts_A",
            "ct_top_parts_B",
            "ct_top_parts_C"
        };

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

        public static string[] ClothesKindName;

        private static Toggle[] tgls2 = new Toggle[0]; //使用時再初始化
        private static Toggle[] tgls;
        private static Image panel2;
        private static RectTransform toggleGroup;
        internal static bool lockHairAcc = false;
        internal static bool addAccModeFlag = true;
        private static int finishedCount = 0;
        internal static bool[] charaOverlay = new bool[] { true, true, true };  //順序: Iris、Face、Body

        public static void InitPostfix(object __instance) {
            BlockAnotherPlugin();
            BlockXUATranslate();

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

            Array ClothesKindArray = Enum.GetValues(typeof(ClothesKind));

            #region UI
            //Draw Panel and ButtonAll
            charaFileSort = (CharaFileSort)__instance.GetField("fileSort");
            Image panel = UIUtility.CreatePanel("CoordinateTooglePanel", charaFileSort.root.parent.parent.parent);
            panel.transform.SetRect(Vector2.zero, Vector2.one, new Vector2(170f, -667.5f), new Vector2(-55f, -345f));
            panel.GetComponent<Image>().color = new Color32(80, 80, 80, 220);

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

            //清空飾品btn
            Button btnClearAcc = UIUtility.CreateButton("BtnClearAcc", panel.transform, StringResources.StringResourcesManager.GetString("clearAccWord"));
            btnClearAcc.GetComponentInChildren<Text>(true).color = Color.white;
            btnClearAcc.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperCenter;
            btnClearAcc.GetComponent<Image>().color = Color.gray;
            btnClearAcc.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -317.5f), new Vector2(-5f, -292.5f));
            btnClearAcc.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            if (KK_StudioCoordinateLoadOption._isCharaOverlayBasedOnCoordinateExist) {
                panel.transform.SetRect(Vector2.zero, Vector2.one, new Vector2(170f, -436.5f), new Vector2(-55f, -345f));
                //分隔線
                Image line2 = UIUtility.CreateImage("line", panel.transform);
                line2.color = Color.gray;
                line2.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -324f), new Vector2(-5f, -322.5f));

                //Chara Overlay toggle
                Toggle tglCharaOverlay = UIUtility.CreateToggle("TglCharaOverlay", panel.transform, StringResources.StringResourcesManager.GetString("charaOverlay"));
                tglCharaOverlay.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                tglCharaOverlay.GetComponentInChildren<Text>(true).color = Color.white;
                tglCharaOverlay.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -354f), new Vector2(-5f, -329f));
                tglCharaOverlay.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));

                //Eye Overlay toggle
                Toggle tglEyeOverlay = UIUtility.CreateToggle("TglIrisOverlay", panel.transform, StringResources.StringResourcesManager.GetString("irisOverlay"));
                tglEyeOverlay.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                tglEyeOverlay.GetComponentInChildren<Text>(true).color = Color.white;
                tglEyeOverlay.transform.SetRect(Vector2.up, Vector2.one, new Vector2(25f, -379f), new Vector2(-5f, -354f));
                tglEyeOverlay.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));

                //Face Overlay toggle
                Toggle tglFaceOverlay = UIUtility.CreateToggle("TglFaceOverlay", panel.transform, StringResources.StringResourcesManager.GetString("faceOverlay"));
                tglFaceOverlay.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                tglFaceOverlay.GetComponentInChildren<Text>(true).color = Color.white;
                tglFaceOverlay.transform.SetRect(Vector2.up, Vector2.one, new Vector2(25f, -404f), new Vector2(-5f, -379f));
                tglFaceOverlay.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));

                //Body Overlay toggle
                Toggle tglBodyOverlay = UIUtility.CreateToggle("TglBodyOverlay", panel.transform, StringResources.StringResourcesManager.GetString("bodyOverlay"));
                tglBodyOverlay.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                tglBodyOverlay.GetComponentInChildren<Text>(true).color = Color.white;
                tglBodyOverlay.transform.SetRect(Vector2.up, Vector2.one, new Vector2(25f, -429f), new Vector2(-5f, -404f));
                tglBodyOverlay.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));

                tglEyeOverlay.onValueChanged.AddListener((x) => {
                    charaOverlay[0] = x;
                    tglCharaOverlay.isOn |= x;
                });
                tglFaceOverlay.onValueChanged.AddListener((x) => {
                    charaOverlay[1] = x;
                    tglCharaOverlay.isOn |= x;
                });
                tglBodyOverlay.onValueChanged.AddListener((x) => {
                    charaOverlay[2] = x;
                    tglCharaOverlay.isOn |= x;
                });

                tglCharaOverlay.onValueChanged.AddListener((x) => {
                    if (!x) {
                        tglEyeOverlay.isOn = x;
                        tglFaceOverlay.isOn = x;
                        tglBodyOverlay.isOn = x;
                    }
                });
                tglCharaOverlay.isOn = false;
            }

            //Draw accessories panel
            panel2 = UIUtility.CreatePanel("AccessoriesTooglePanel", panel.transform);
            panel2.transform.SetRect(Vector2.one, Vector2.one, new Vector2(5f, -612.5f), new Vector2(200f, 0f));
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
            #endregion

            //Button邏輯
            btnAll.onClick.RemoveAllListeners();
            btnAll.onClick.AddListener(() => {
                bool flag = false;
                for (int i = 0; i < tgls.Length; i++) {
                    if (!tgls[i].isOn && !flag) {
                        flag = true;
                        i = 0;
                    }
                    tgls[i].isOn = flag;
                }
            });
            btnAll2.onClick.RemoveAllListeners();
            btnAll2.onClick.AddListener(() => {
                bool flag = false;
                for (int i = 0; i < tgls2.Length; i++) {
                    if (!tgls2[i].isOn && !flag) {
                        flag = true;
                        i = 0;
                    }
                    tgls2[i].isOn = flag;
                }
            });
            btnClearAcc.onClick.RemoveAllListeners();
            btnClearAcc.onClick.AddListener(() => {
                OCIChar[] array = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                                   select Studio.Studio.GetCtrlInfo(v) as OCIChar into v
                                   where v != null
                                   select v).ToArray();
                foreach (OCIChar ocichar in array) {
                    for (int i = 0; i < 20; i++) {
                        if (!(IsHairAccessory(ocichar.charInfo, i) && lockHairAcc)) {
                            ocichar.charInfo.nowCoordinate.accessory.parts[i] = new ChaFileAccessory.PartsInfo();
                        } else {
                            Logger.LogDebug($"Keep HairAcc{i}: {ocichar.charInfo.nowCoordinate.accessory.parts[i].id}");
                        }
                    }
                    if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                        //以下這行造成後續換衣不正常運作的問題，未勾選項也會被載入
                        //ocichar.charInfo.chaFile.coordinate[ocichar.charInfo.fileStatus.coordinateType] = ocichar.charInfo.nowCoordinate;
                        MoreAccessories_Support.ClearMoreAccessoriesData(ocichar.charInfo);
                    }
                    //ocichar.charInfo.ChangeAccessory(true);
                    ocichar.charInfo.AssignCoordinate((ChaFileDefine.CoordinateType)ocichar.charInfo.fileStatus.coordinateType);
                    ocichar.charInfo.Reload(false, true, true, true);
                }

                Logger.LogDebug("Clear accessories Finish");
            });
            btnReverseHairAcc.onClick.RemoveAllListeners();
            btnReverseHairAcc.onClick.AddListener(() => {
                MakeTmpChara();
                for (int i = 0; i < MoreAccessories_Support.GetAccessoriesAmount(tmpChaCtrl.chaFile); i++) {
                    if (i < tgls2.Length) {
                        if (IsHairAccessory(tmpChaCtrl, i)) {
                            tgls2[i].isOn = !tgls2[i].isOn;
                            Logger.LogDebug($"Reverse Hair Acc.: {i}");
                        }
                    }
                }
                Singleton<Manager.Character>.Instance.DeleteChara(tmpChaCtrl);
                Logger.LogDebug($"Delete Temp Chara");
                Logger.LogDebug("Reverse Hair Acc. toggles.");
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
            btnChangeAccLoadMode.onClick.Invoke();

            //Logger.LogDebug("Draw UI Finish");
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

        //選擇衣裝
        internal static void OnSelectPostfix() {
            if (null == panel2) {
                return;
            }

            //取得飾品清單
            ChaFileCoordinate tmpChaFileCoordinate = new ChaFileCoordinate();
            tmpChaFileCoordinate.LoadFile(charaFileSort.selectPath);

            List<string> accNames = new List<string>();

            accNames.AddRange(tmpChaFileCoordinate.accessory.parts.Select(x => GetNameFromIDAndType(x.id, (ChaListDefine.CategoryNo)x.type)));

            if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                accNames.AddRange(MoreAccessories_Support.LoadMoreAcc(tmpChaFileCoordinate));
            }

            foreach (Toggle tgl in toggleGroup.gameObject.GetComponentsInChildren<Toggle>()) {
                GameObject.Destroy(tgl.gameObject);
            }
            List<Toggle> tmpTgls = new List<Toggle>();
            foreach (string accName in accNames) {
                Toggle toggle = UIUtility.CreateToggle(Enum.GetValues(typeof(ClothesKind)).GetValue(9).ToString(), toggleGroup.transform, accName);
                toggle.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                toggle.GetComponentInChildren<Text>(true).color = Color.white;
                toggle.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -25f * (tmpTgls.Count + 1)), new Vector2(0f, -25f * tmpTgls.Count));
                toggle.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));
                tmpTgls.Add(toggle);
            }
            tgls2 = tmpTgls.ToArray();
            toggleGroup.transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(0, -(25f * (accNames.Count - 20))), new Vector2(0, 0));
            if (tgls[(int)ClothesKind.accessories].isOn) {
                panel2.gameObject.SetActive(true);
            }
            Logger.LogDebug("Onselect");
        }

        [HarmonyPriority(Priority.First)]
        internal static bool OnClickLoadPrefix() => false;

        //Load衣裝時觸發
        internal static void OnClickLoadPostfix() {
            Logger.LogDebug("Studio Coordinate Load Option Start");

            bool isAllTrueFlag = true;
            bool isAllFalseFlag = true;
            foreach (Toggle tgl in tgls) {
                isAllTrueFlag &= tgl.isOn;
                isAllFalseFlag &= !tgl.isOn;
            }
            foreach (Toggle tgl in tgls2) {
                isAllTrueFlag &= tgl.isOn;
            }
            foreach (bool b in charaOverlay) {
                isAllTrueFlag &= b;
                isAllFalseFlag &= !b;
            }
            if (isAllFalseFlag) {
                Logger.LogInfo("No Toggle selected, skip loading coordinate");
                Logger.LogDebug("Studio Coordinate Load Option Finish");
                return;
            }

            OCIChar[] array = (from v in GuideObjectManager.Instance.selectObjectKey
                               select Studio.Studio.GetCtrlInfo(v) into v
                               where null != v
                               select v).OfType<OCIChar>().ToArray();
            if (isAllTrueFlag && !lockHairAcc && !addAccModeFlag) {
                Logger.LogInfo("Toggle all true, use original game function");
                foreach (OCIChar ocichar in array) {
                    ocichar.LoadClothesFile(charaFileSort.selectPath);
                }
            } else if (array.Length == 0) {
                Logger.LogMessage("No available characters selected");
                Logger.LogDebug("Studio Coordinate Load Option Finish");
            } else {
                //建立tmpChara並等待載入完成
                //然後再呼叫換衣
                oCICharArray = array;
                finishedCount = 0;
                MakeTmpChara(1);
            }
            return;
        }

        private static OCIChar[] oCICharArray;
        private static readonly Dictionary<OCIChar, float> mouthOpen = new Dictionary<OCIChar, float>();
        private static bool ReloadCheck1 = true;
        private static readonly Queue<ChaControl> ReloadCheck2 = new Queue<ChaControl>();
        private static readonly Queue<ChaControl> ReloadCheck3 = new Queue<ChaControl>();
        private static ChaControl tmpChaCtrl;
        private static ChaFileCoordinate backupTmpCoordinate;
        private static int callCount;

        internal static void Update() {
            #region ReloadCheck1
            if (!ReloadCheck1) {
                callCount--;
                ReloadCheck1Func();
            }
            #endregion

            #region ReloadCheck2
            if (ReloadCheck2.Count > 0) {
                callCount--;
                ReloadCheck2Func();
            }
            #endregion

            #region ReloadCheck3
            if (ReloadCheck3.Count > 0) {
                callCount--;
                ReloadCheck3Func();
            }
            #endregion
        }

        private static void MakeTmpChara(int reloadState = 0) {
            if (reloadState == 1) {
                ReloadCheck1 = false;
            }
            callCount = 10;

            //tmpChaCtrl = Singleton<Manager.Character>.Instance.CreateFemale(Singleton<Manager.Scene>.Instance.commonSpace, -1);
            tmpChaCtrl = Singleton<Manager.Character>.Instance.CreateFemale(Camera.main.gameObject, -1);
            tmpChaCtrl.Load();
            tmpChaCtrl.fileParam.lastname = "黑肉";
            tmpChaCtrl.fileParam.firstname = "舔舔";
            tmpChaCtrl.gameObject.transform.localPosition = new Vector3(0, 0, -10);   //丟到Camera後面就看不見了
            backupTmpCoordinate = new ChaFileCoordinate();
            backupTmpCoordinate.LoadFile(charaFileSort.selectPath);
            tmpChaCtrl.nowCoordinate.LoadFile(charaFileSort.selectPath);
            tmpChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)tmpChaCtrl.fileStatus.coordinateType);
            tmpChaCtrl.Reload();
            //=>Goto ReloadCheck1
        }

        internal static void ReloadCheck1Func() {
            if (null != tmpChaCtrl && (HairAccessoryCustomizer_Support.CheckHairLoadStateByCoordinate(tmpChaCtrl))) {
                ReloadCheck1 = true;

                tmpChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)tmpChaCtrl.fileStatus.coordinateType);
                if (KK_StudioCoordinateLoadOption._isHairAccessoryCustomizerExist) {
                    HairAccessoryCustomizer_Support.SetExtDataFromController(tmpChaCtrl);
                }
                if (KK_StudioCoordinateLoadOption._isMaterialEditorExist) {
                    MaterialEditor_Support.SetControllerFromCoordinate(tmpChaCtrl);
                }
                ReloadCheck2.Clear();
                ReloadCheck3.Clear();
                callCount = 10;
                foreach (OCIChar ocichar in oCICharArray) {
                    mouthOpen.Add(ocichar, ocichar.oiCharInfo.mouthOpen);
                    if (KK_StudioCoordinateLoadOption._isHairAccessoryCustomizerExist) {
                        HairAccessoryCustomizer_Support.SetExtDataFromController(ocichar.charInfo);
                    }
                    ReloadCheck2.Enqueue(ocichar.charInfo);
                    //=>Goto ReloadCheck2
                }
            } else if (callCount <= 0) {
                ReloadCheck1 = true;
                backupTmpCoordinate = null;
                callCount = 0;

                Singleton<Manager.Character>.Instance.DeleteChara(tmpChaCtrl);
                foreach (OCIChar ocichar in oCICharArray) {
                    ocichar.LoadClothesFile(charaFileSort.selectPath);
                }
                Logger.LogError("Reload tmpChaCtrl ERROR in step 1, call original game function instead");
            }
        }

        private static void ReloadCheck2Func() {
            if (HairAccessoryCustomizer_Support.CheckExtDataLoadStateByController(ReloadCheck2.Peek())) {
                ChaControl chaCtrl = ReloadCheck2.Dequeue();

                Queue<int> accQueue = new Queue<int>();

                foreach (Toggle tgl in tgls) {
                    object tmpToggleType = null;
                    int kind = -2;
                    try {
                        tmpToggleType = Enum.Parse(typeof(ClothesKind), tgl.name);
                        kind = Convert.ToInt32(tmpToggleType);
                    } catch (ArgumentException) {
                        kind = -1;
                    }
                    if (tgl.isOn) {
                        if (kind == 9) {
                            if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
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
                            Logger.LogDebug("->Changed: " + tgl.name);
                        } else if (kind >= 0) {
                            //Change clothes
                            byte[] tmp = MessagePackSerializer.Serialize<ChaFileClothes.PartsInfo>(tmpChaCtrl.nowCoordinate.clothes.parts[kind]);
                            chaCtrl.nowCoordinate.clothes.parts[kind] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(tmp);
                            chaCtrl.ChangeClothes(kind, tmpChaCtrl.nowCoordinate.clothes.parts[kind].id, tmpChaCtrl.nowCoordinate.clothes.subPartsId[0], tmpChaCtrl.nowCoordinate.clothes.subPartsId[1], tmpChaCtrl.nowCoordinate.clothes.subPartsId[2], true);

                            if (KK_StudioCoordinateLoadOption._isMaterialEditorExist) {
                                MaterialEditor_Support.CopyMaterialEditorData(MaterialEditor_Support.ObjectType.Clothing, tmpChaCtrl, kind, chaCtrl, kind);
                            }

                            Logger.LogDebug("->Changed: " + tgl.name + " / ID: " + chaCtrl.nowCoordinate.clothes.parts[kind].id);
                        }
                    }
                }

                //寫入Material Editor Data
                if (KK_StudioCoordinateLoadOption._isMaterialEditorExist) {
                    MaterialEditor_Support.SetToController(chaCtrl, MaterialEditor_Support.ObjectType.Clothing, MaterialEditor_Support.TargetMaterialBackup);
                    MaterialEditor_Support.SetToController(chaCtrl, MaterialEditor_Support.ObjectType.Accessory, MaterialEditor_Support.TargetMaterialBackup);
                    MaterialEditor_Support.SetExtDataFromController(chaCtrl);

                    //Backup Material
                    MaterialEditor_Support.SetCoordinateExtDataFromController(chaCtrl, backupTmpCoordinate);
                }

                //KK_COBOC
                if (KK_StudioCoordinateLoadOption._isCharaOverlayBasedOnCoordinateExist) {
                    if (charaOverlay.ToList().Where((x) => x).Count() > 0) {
                        COBOC_Support.CopyCurrentCharaOverlayByController(tmpChaCtrl, chaCtrl, charaOverlay);
                    } else {
                        Logger.LogDebug("Skip load CharaOverlay");
                    }

                    //Backup Chara Overlays
                    COBOC_Support.SetCoordinateExtDataFromController(chaCtrl, backupTmpCoordinate);
                }

                //Backup KCOX
                if (KK_StudioCoordinateLoadOption._isKCOXExist) {
                    KCOX_Support.BackupKCOXData(chaCtrl, chaCtrl.nowCoordinate.clothes);
                }

                //Backup ABMX
                if (KK_StudioCoordinateLoadOption._isABMXExist) {
                    ABMX_Support.BackupABMXData(chaCtrl);
                }

                //Backup KCOX
                if (KK_StudioCoordinateLoadOption._isKCOXExist) {
                    KCOX_Support.BackupKCOXData(chaCtrl, chaCtrl.nowCoordinate.clothes);
                }

                //Backup ABMX
                if (KK_StudioCoordinateLoadOption._isABMXExist) {
                    ABMX_Support.BackupABMXData(chaCtrl);
                }

                //Backup MoreAcc
                if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                    MoreAccessories_Support.CopyAllMoreAccessoriesData(chaCtrl, tmpChaCtrl);
                    MoreAccessories_Support.CopyAllMoreAccessoriesData(tmpChaCtrl, chaCtrl);
                }
                //chaCtrl.ChangeAccessory(true);
                chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType);
                chaCtrl.Reload();

                //fake load
                using (FileStream fileStream = new FileStream(charaFileSort.selectPath, FileMode.Open, FileAccess.Read)) {
                    fakeLoadFlag_LoadBytes = true;
                    chaCtrl.nowCoordinate.LoadFile(fileStream);
                    fakeLoadFlag_LoadBytes = false;
                }

                //Load完畢再Rollback
                callCount = 10;
                ReloadCheck3.Enqueue(chaCtrl);
                //=>Goto ReloadCheck3
            } else if (callCount <= 0) {
                ChaControl chaCtrl = ReloadCheck2.Dequeue();
                Logger.LogError($"Reload {chaCtrl.name} ERROR in step 2, call original game function instead");
                oCICharArray.Where(x => x.charInfo == chaCtrl).ToList().ForEach(x => {
                    x.LoadClothesFile(charaFileSort.selectPath);
                });
                finishedCount++;
                DeleteTmpCharaCheck();
                callCount = 10;
            }
        }

        internal static void ReloadCheck3Func() {
            if (HairAccessoryCustomizer_Support.CheckHairLoadStateByCoordinate(ReloadCheck3.Peek(), backupTmpCoordinate)) {
                ChaControl chaCtrl = ReloadCheck3.Dequeue();
                chaCtrl.StopAllCoroutines();
                //Rollback All MoreAccessories
                if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                    MoreAccessories_Support.CopyAllMoreAccessoriesData(tmpChaCtrl, chaCtrl);
                }

                foreach (Toggle tgl in tgls) {
                    int kind;
                    try {
                        kind = Convert.ToInt32(Enum.Parse(typeof(ClothesKind), tgl.name));
                    } catch (ArgumentException) {
                        kind = -1;
                    }

                    if (!tgl.isOn && kind >= 0 && kind < 9) {
                        //Rollback KCOX
                        if (KK_StudioCoordinateLoadOption._isKCOXExist) {
                            KCOX_Support.RollbackOverlay(true, kind, chaCtrl);
                            if (kind == 0) {
                                for (int j = 0; j < SubClothesNames.Length; j++) {
                                    KCOX_Support.RollbackOverlay(false, j, chaCtrl);
                                }
                                foreach (string maskKind in KCOX_Support.MaskKind) {
                                    KCOX_Support.RollbackOverlay(true, 0, chaCtrl, maskKind);
                                }
                            }
                        }

                        //Rollback ABMX
                        if (KK_StudioCoordinateLoadOption._isABMXExist) {
                            if (kind == 1) {
                                ABMX_Support.RollbackABMXBone(chaCtrl);
                            }
                        }
                    }
                }

                //HairAcc不需要Rollback，因為Load Coordinate時是直接從存檔->Controller
                //沒有修改chaCtrl上的ExtData，故只需Reload就會再覆蓋回來
                if (KK_StudioCoordinateLoadOption._isHairAccessoryCustomizerExist) {
                    //HairAccessoryCustomizer_Support.SetControllerFromExtData(chaCtrl);
                    HairAccessoryCustomizer_Support.GetDataFromExtData(chaCtrl, out Dictionary<int, object> nowCoor);
                    if (null == nowCoor) {
                        nowCoor = new Dictionary<int, object>();
                    }
                    Logger.LogDebug($"->Hair Count: {nowCoor.Count} : {string.Join(",", nowCoor.Select(x => x.Key.ToString()).ToArray())}");
                }

                //Rollback Material Editor Data
                //這Rollback後如果MaterialEditor內有尚未完成的LoadData就會跳錯誤，但並沒關係
                if (KK_StudioCoordinateLoadOption._isMaterialEditorExist) {
                    MaterialEditor_Support.SetControllerFromCoordinate(chaCtrl, backupTmpCoordinate);
                }

                if (KK_StudioCoordinateLoadOption._isCharaOverlayBasedOnCoordinateExist) {
                    COBOC_Support.SetControllerFromCoordinate(chaCtrl, backupTmpCoordinate);
                    COBOC_Support.SetExtDataFromController(chaCtrl);
                }

                //修正嘴開
                KeyValuePair<OCIChar, float> kv = mouthOpen.Where(x => x.Key.charInfo == chaCtrl).SingleOrDefault();
                if (!kv.Equals(default(KeyValuePair<OCIChar, float>))) {
                    kv.Key.ChangeMouthOpen(kv.Value);
                    mouthOpen.Remove(kv.Key);
                }

                //全false的Reload會觸發KKAPI的hook
                chaCtrl.Reload(false, true, true, true);

                finishedCount++;

                Logger.LogDebug($"Extended Parts Count : {MoreAccessories_Support.GetAccessoriesAmount(chaCtrl.chaFile)}");
                Logger.LogDebug($"Loaded: {finishedCount}/{oCICharArray.Length}");
                DeleteTmpCharaCheck();
            } else if (callCount <= 0) {
                ChaControl chaCtrl = ReloadCheck3.Dequeue();
                if (ReloadCheck3.Count == 0 && ReloadCheck2.Count == 0) {
                    backupTmpCoordinate = null;

                    Singleton<Manager.Character>.Instance.DeleteChara(tmpChaCtrl);
                    Logger.LogDebug($"Delete Temp Chara");
                }
                Logger.LogError($"Reload {chaCtrl.name} ERROR in step 3, call original game function instead");
                oCICharArray.Where(x => x.charInfo == chaCtrl).ToList().ForEach(x => {
                    x.LoadClothesFile(charaFileSort.selectPath);
                });
                finishedCount++;
                DeleteTmpCharaCheck();
                callCount = 10;
            }
        }

        private static void DeleteTmpCharaCheck() {
            if (finishedCount >= oCICharArray.Length) {
                HairAccessoryCustomizer_Support.ClearHairAccBackup();
                MaterialEditor_Support.ClearMaterialBackup();
                KCOX_Support.CleanKCOXBackup();
                Singleton<Manager.Character>.Instance.DeleteChara(tmpChaCtrl);
                Logger.LogDebug($"Delete Temp Chara");
                Logger.LogInfo($"Load Finish");
                //結束
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
            foreach (bool b in tgls2.Select(x => x.isOn).ToArray()) {
                isAllFalseFlag &= !b;
            }
            if (isAllFalseFlag && accQueue.Count == 0) {
                Logger.LogDebug("Load Accessories All False");
                Logger.LogDebug("Load Accessories Finish (1)");
                return;
            }
            Logger.LogDebug($"Acc Count : {tgls2.Length}");

            if (KK_StudioCoordinateLoadOption._isHairAccessoryCustomizerExist) {
                HairAccessoryCustomizer_Support.GetControllerAndBackupData(sourceChaCtrl, targetChaCtrl);
            }

            for (int i = 0; i < targetParts.Length && i < tgls2.Length; i++) {
                if ((bool)tgls2[i]?.isOn) {
                    if (addAccModeFlag) {
                        //增加模式
                        if (targetParts[i].type == 120) {
                            DoChangeAccessory(sourceChaCtrl, sourceParts, i, targetChaCtrl, targetParts, i);
                        } else {
                            EnQueue();
                        }
                    } else {
                        //取代模式
                        if (IsHairAccessory(targetChaCtrl, i) && lockHairAcc) {
                            EnQueue();
                        } else {
                            //如果是取代模式且非髮飾品則取代
                            DoChangeAccessory(sourceChaCtrl, sourceParts, i, targetChaCtrl, targetParts, i);
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
                    DoChangeAccessory(sourceChaCtrl, sourceParts, slot, targetChaCtrl, targetParts, j);
                    Logger.LogDebug($"->DeQueue: Acc{j} / Part: {(ChaListDefine.CategoryNo)targetParts[j].type} / ID: {targetParts[j].id}");
                } //else continue;
            }

            //寫入
            if (KK_StudioCoordinateLoadOption._isHairAccessoryCustomizerExist) {
                HairAccessoryCustomizer_Support.SetToExtData(targetChaCtrl, HairAccessoryCustomizer_Support.targetHairBackup);
            }
        }

        /// <summary>
        /// 換飾品
        /// </summary>
        /// <param name="sourceChaCtrl">來源ChaControl</param>
        /// <param name="sourceParts">來源PartsInfo list</param>
        /// <param name="sourceSlot">來源slot</param>
        /// <param name="targetChaCtrl">目標ChaControl</param>
        /// <param name="targetParts">目標PartsInfo list</param>
        /// <param name="targetSlot">目標slot</param>
        public static void DoChangeAccessory(ChaControl sourceChaCtrl,
                                           ChaFileAccessory.PartsInfo[] sourceParts,
                                           int sourceSlot,
                                           ChaControl targetChaCtrl,
                                           ChaFileAccessory.PartsInfo[] targetParts,
                                           int targetSlot) {
            //來源目標都空著就跳過
            if (sourceParts[sourceSlot].type == 120 && targetParts[targetSlot].type == 120) {
                Logger.LogDebug($"->BothEmpty: SourceAcc{sourceSlot}, TargetAcc{targetSlot}");
                return;
            }

            byte[] tmp = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(sourceParts[sourceSlot]);
            targetParts[targetSlot] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tmp);
            if (KK_StudioCoordinateLoadOption._isHairAccessoryCustomizerExist) {
                HairAccessoryCustomizer_Support.CopyHairAcc(sourceChaCtrl, sourceSlot, targetChaCtrl, targetSlot);
            }
            if (KK_StudioCoordinateLoadOption._isMaterialEditorExist) {
                MaterialEditor_Support.CopyMaterialEditorData(MaterialEditor_Support.ObjectType.Accessory, sourceChaCtrl, sourceSlot, targetChaCtrl, targetSlot);
            }
            Logger.LogDebug($"->Changed: Acc{targetSlot} / Part: {(ChaListDefine.CategoryNo)targetParts[targetSlot].type} / ID: {targetParts[targetSlot].id}");
        }

        /// <summary>
        /// 檢查是否為頭髮飾品
        /// </summary>
        /// <param name="chaCtrl">對象角色</param>
        /// <param name="index">飾品欄位index</param>
        /// <returns></returns>
        public static bool IsHairAccessory(ChaControl chaCtrl, int index) {
            return GetChaAccessoryComponent(chaCtrl, index)?.gameObject.GetComponent<ChaCustomHairComponent>() != null;
        }

        /// <summary>
        /// 取得ChaAccessoryComponent
        /// </summary>
        /// <param name="chaCtrl"></param>
        /// <param name="index"></param>
        /// <returns>ChaAccessoryComponent</returns>
        public static ChaAccessoryComponent GetChaAccessoryComponent(ChaControl chaCtrl, int index) {
            if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                if (index >= MoreAccessories_Support.GetAccessoriesAmount(chaCtrl.chaFile)) {
                    return null;
                }
                return MoreAccessories_Support.GetChaAccessoryComponent(chaCtrl, index);
            } else {
                return chaCtrl.GetAccessoryComponent(index);
            }
        }

        private static bool fakeLoadFlag_LoadBytes = false;
        [HarmonyPrefix, HarmonyPatch(typeof(ChaFileCoordinate), "LoadBytes"), HarmonyPriority(Priority.Last)]
        public static bool LoadBytesPrefix(ref bool __result) {
            __result = fakeLoadFlag_LoadBytes;
            return !fakeLoadFlag_LoadBytes;
        }

        //另一插件(KK_ClothesLoadOption)在和我相同的位置畫Panel，將他Block掉
        //因為他的插件在CharaMaker和Studio皆有功能，僅Studio部分和我重疊，故採此對策
        //若是要選擇用他的插件，直接將我這插件移除即可。
        private static void BlockAnotherPlugin() {
            GameObject anotherPlugin = GameObject.Find("StudioScene/Canvas Main Menu/ClosesLoadOption Panel");
            if (null != anotherPlugin) {
                anotherPlugin.transform.localScale = Vector3.zero;
                Logger.LogDebug("Block KK_ClothesLoadOption Panel");
            }
        }

        //如果啟動機翻就一律顯示英文，否則機翻會毀了我的文字
        private static void BlockXUATranslate() {
            if (null != Extension.Extension.TryGetPluginInstance("gravydevsupreme.xunity.autotranslator")) {
                string XUAConfigPath = Path.Combine(Paths.ConfigPath, "AutoTranslatorConfig.ini");
                if (IniFile.FromFile(XUAConfigPath).GetSection("TextFrameworks").GetKey("EnableUGUI").Value == "True") {
                    StringResources.StringResourcesManager.SetUICulture("en-US");
                    Logger.LogInfo("Found XUnityAutoTranslator Enabled, load English UI");
                }
            }
        }
    }
}
