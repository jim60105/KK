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
    public class KK_StudioCoordinateLoadOption : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Coordinate Load Option";
        internal const string GUID = "com.jim60105.kk.studiocoordinateloadoption";
        internal const string PLUGIN_VERSION = "19.11.19.1";

        internal static new ManualLogSource Logger;

        public void Awake() {
            Logger = base.Logger;
            UIUtility.Init();
            var harmonyInstance = HarmonyWrapper.PatchAll(typeof(Patches));
            harmonyInstance.PatchAll(typeof(MoreAccessories_Support));
            harmonyInstance.Patch(typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("Init", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(Patches.InitPostfix), null), null);
            harmonyInstance.Patch(typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("OnClickLoad", AccessTools.all), new HarmonyMethod(typeof(Patches), nameof(Patches.OnClickLoadPrefix), null), null, null);
            harmonyInstance.Patch(typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("OnSelect", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(Patches.OnSelectPostfix), null), null);
        }

        public static bool _isKCOXExist = false;
        public static bool _isABMXExist = false;
        public static bool _isMoreAccessoriesExist = false;
        public static bool _isMaterialEditorExist = false;
        public static bool _isHairAccessoryCustomizerExist = false;

        public void Start() {
            _isKCOXExist = KCOX_Support.LoadAssembly();
            _isABMXExist = ABMX_Support.LoadAssembly();
            _isMoreAccessoriesExist = MoreAccessories_Support.LoadAssembly();
            _isMaterialEditorExist = MaterialEditor_Support.LoadAssembly();
            _isHairAccessoryCustomizerExist = HairAccessoryCustomizer_Support.LoadAssembly();

            StringResources.StringResourcesManager.SetUICulture();
        }

        internal static BaseUnityPlugin TryGetPluginInstance(string pluginName, Version minimumVersion = null) {
            BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(pluginName, out var target);
            if (null != target) {
                if (target.Metadata.Version >= minimumVersion) {
                    return target.Instance;
                }
                Logger.LogMessage($"{pluginName} v{target.Metadata.Version.ToString()} is detacted OUTDATED.");
                Logger.LogMessage($"Please update {pluginName} to at least v{minimumVersion.ToString()} to enable related feature.");
            }
            return null;
        }
    }

    internal class Patches {
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
        internal static bool lockHairAcc = true;
        internal static bool addAccModeFlag = true;

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

            //Draw Panel and ButtonAll
            charaFileSort = (CharaFileSort)__instance.GetField("fileSort");
            Image panel = UIUtility.CreatePanel("CoordinateTooglePanel", charaFileSort.root.parent.parent.parent);
            panel.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(405f, -33f), new Vector2(150f, 340f));
            panel.GetComponent<Image>().color = new Color32(80, 80, 80, 220);

            Button btnAll = UIUtility.CreateButton("BtnAll", panel.transform, "All");
            btnAll.GetComponentInChildren<Text>(true).color = Color.white;
            btnAll.GetComponent<Image>().color = Color.gray;
            btnAll.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -30), new Vector2(-5f, -5f));
            btnAll.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            //Draw Toggles
            tgls = new Toggle[ClothesKindArray.Length];
            for (int i = 0; i < ClothesKindArray.Length; i++) {
                tgls[i] = UIUtility.CreateToggle(ClothesKindArray.GetValue(i).ToString(), panel.transform, ClothesKindName.GetValue(i).ToString());
                tgls[i].GetComponentInChildren<Text>(true).alignment = TextAnchor.MiddleLeft;
                tgls[i].GetComponentInChildren<Text>(true).color = Color.white;
                tgls[i].transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -60f - 25f * i), new Vector2(-5f, -35f - 25f * i));
                tgls[i].GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));
                if (i == (int)ClothesKind.accessories) {
                    tgls[i].onValueChanged.AddListener((x) => {
                        if (tgls2.Length != 0 && null != panel2) {
                            panel2.gameObject.SetActive(x);
                        }
                    });
                }
            }

            //分隔線
            Image line = UIUtility.CreateImage("line", panel.transform);
            line.color = Color.gray;
            line.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -287.5f), new Vector2(140f, -286f));

            //排除頭髮飾品toggle
            Toggle tglHair = UIUtility.CreateToggle("lockHairAcc", panel.transform, StringResources.StringResourcesManager.GetString("lockHairAcc"));
            tglHair.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
            tglHair.GetComponentInChildren<Text>(true).color = Color.yellow;
            tglHair.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -317.5f), new Vector2(-5f, -292.5f));
            tglHair.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));
            tglHair.isOn = lockHairAcc;
            tglHair.onValueChanged.AddListener((x) => {
                lockHairAcc = x;
            });

            //飾品載入模式btn
            Button btnChangeAccLoadMode = UIUtility.CreateButton("BtnChangeAccLoadMode", panel.transform, "AccModeBtn");
            btnChangeAccLoadMode.GetComponentInChildren<Text>(true).color = Color.white;
            btnChangeAccLoadMode.GetComponentInChildren<Text>(true).alignment = TextAnchor.MiddleCenter;
            btnChangeAccLoadMode.GetComponent<Image>().color = Color.gray;
            btnChangeAccLoadMode.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -342.5f), new Vector2(140f, -317.5f));
            btnChangeAccLoadMode.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            //清空飾品btn
            Button btnClearAcc = UIUtility.CreateButton("BtnClearAcc", panel.transform, StringResources.StringResourcesManager.GetString("clearAccWord"));
            btnClearAcc.GetComponentInChildren<Text>(true).color = Color.white;
            btnClearAcc.GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperCenter;
            btnClearAcc.GetComponent<Image>().color = Color.gray;
            btnClearAcc.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -369f), new Vector2(140f, -344f));
            btnClearAcc.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            //Draw accessories panel
            panel2 = UIUtility.CreatePanel("AccessoriesTooglePanel", panel.transform);
            panel2.transform.SetRect(Vector2.one, Vector2.one, new Vector2(5f, -537.5f), new Vector2(180f, 0f));
            panel2.GetComponent<Image>().color = new Color32(80, 80, 80, 220);
            panel2.gameObject.SetActive(false);
            Button btnAll2 = UIUtility.CreateButton("BtnAll2", panel2.transform, "All");
            btnAll2.GetComponentInChildren<Text>(true).color = Color.white;
            btnAll2.GetComponent<Image>().color = Color.gray;
            btnAll2.transform.SetRect(Vector2.up, Vector2.one, new Vector2(5f, -30), new Vector2(-5f, -5f));
            btnAll2.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            //滾動元件
            ScrollRect scrollRect = UIUtility.CreateScrollView("scroll", panel2.transform);
            toggleGroup = scrollRect.content;
            scrollRect.transform.SetRect(Vector2.zero, Vector2.one, new Vector2(0f, 5f), new Vector2(0, -35f));
            scrollRect.GetComponent<Image>().color = new Color32(0, 0, 0, 0);
            foreach (var img in scrollRect.verticalScrollbar.GetComponentsInChildren<Image>()) {
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
                foreach (var ocichar in array) {
                    for (int i = 0; i < 20; i++) {
                        if (!IsHairAccessory(ocichar.charInfo, i)) {
                            ocichar.charInfo.nowCoordinate.accessory.parts[i] = new ChaFileAccessory.PartsInfo();
                        }
                    }
                    if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                        //以下這行造成後續換衣不正常運作的問題，未勾選項也會被載入
                        //ocichar.charInfo.chaFile.coordinate[ocichar.charInfo.fileStatus.coordinateType] = ocichar.charInfo.nowCoordinate;
                        MoreAccessories_Support.ClearMoreAccessoriesData(ocichar.charInfo);
                    }
                    //ocichar.charInfo.ChangeAccessory(true);
                    ocichar.charInfo.Reload(false, true, true, true);
                    ocichar.charInfo.AssignCoordinate((ChaFileDefine.CoordinateType)ocichar.charInfo.fileStatus.coordinateType);
                }

                KK_StudioCoordinateLoadOption.Logger.LogDebug("Clear accessories Finish");
            });

            btnChangeAccLoadMode.onClick.RemoveAllListeners();
            btnChangeAccLoadMode.onClick.AddListener(() => {
                addAccModeFlag = !addAccModeFlag;
                btnChangeAccLoadMode.GetComponentInChildren<Text>().text =
                    addAccModeFlag ?
                    StringResources.StringResourcesManager.GetString("addMode") :
                    StringResources.StringResourcesManager.GetString("replaceMode");
                KK_StudioCoordinateLoadOption.Logger.LogDebug("Set add accessories mode to " + (addAccModeFlag ? "add" : "replace") + " mode");
            });
            btnChangeAccLoadMode.onClick.Invoke();

            KK_StudioCoordinateLoadOption.Logger.LogDebug("Draw UI Finish");
        }

        internal static string GetNameFromIDAndType(int id, ChaListDefine.CategoryNo type) {
            ChaListControl chaListControl = Singleton<Manager.Character>.Instance.chaListCtrl;
            KK_StudioCoordinateLoadOption.Logger.LogDebug($"Find Accessory id / type: {id} / {type}");

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
            var tmpChaFileCoordinate = new ChaFileCoordinate();
            tmpChaFileCoordinate.LoadFile(charaFileSort.selectPath);

            List<string> accNames = new List<string>();

            accNames.AddRange(tmpChaFileCoordinate.accessory.parts.Select(x => GetNameFromIDAndType(x.id, (ChaListDefine.CategoryNo)x.type)));

            if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                accNames.AddRange(MoreAccessories_Support.LoadMoreAccFromCoordinate(tmpChaFileCoordinate));
            }

            foreach (var tgl in toggleGroup.gameObject.GetComponentsInChildren<Toggle>()) {
                GameObject.Destroy(tgl.gameObject);
            }
            var tmpTgls = new List<Toggle>();
            foreach (var accName in accNames) {
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
            KK_StudioCoordinateLoadOption.Logger.LogDebug("Onselect");
        }

        //Load衣裝時觸發
        internal static bool OnClickLoadPrefix() {
            KK_StudioCoordinateLoadOption.Logger.LogDebug("Studio Coordinate Load Option Start");

            bool isAllTrueFlag = true;
            bool isAllFalseFlag = true;
            foreach (Toggle tgl in tgls) {
                isAllTrueFlag &= tgl.isOn;
                isAllFalseFlag &= !tgl.isOn;
            }
            foreach (Toggle tgl in tgls2) {
                isAllTrueFlag &= tgl.isOn;
            }
            if (isAllFalseFlag) {
                KK_StudioCoordinateLoadOption.Logger.LogInfo("No Toggle selected, skip loading coordinate");
                KK_StudioCoordinateLoadOption.Logger.LogDebug("Studio Coordinate Load Option Finish");
                return false;
            }

            OCIChar[] array = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                               select Studio.Studio.GetCtrlInfo(v) as OCIChar into v
                               where v != null
                               select v).ToArray();
            if (isAllTrueFlag && !lockHairAcc && !addAccModeFlag) {
                KK_StudioCoordinateLoadOption.Logger.LogInfo("Toggle all true, use original game function");
                foreach (var ocichar in array) {
                    ocichar.LoadClothesFile(charaFileSort.selectPath);
                }
            } else {
                ChaControl tmpChaCtrl = Singleton<Manager.Character>.Instance.CreateFemale(Singleton<Manager.Scene>.Instance.commonSpace, -1);
                tmpChaCtrl.Load();
                tmpChaCtrl.nowCoordinate.LoadFile(charaFileSort.selectPath);
                tmpChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)tmpChaCtrl.fileStatus.coordinateType);
                tmpChaCtrl.Reload(false, true, false, true);
                foreach (var ocichar in array) {
                    LoadCoordinates(ocichar.charInfo, tmpChaCtrl);
                }
                Singleton<Manager.Character>.Instance.DeleteChara(tmpChaCtrl);
                //UnityEngine.Object.Destroy(tmpChaCtrl);

                KK_StudioCoordinateLoadOption.Logger.LogDebug("Studio Coordinate Load Option Finish");
            }
            return false;
        }

        /// <summary>
        /// 檢查是否為頭髮飾品
        /// </summary>
        /// <param name="chaCtrl">對象角色</param>
        /// <param name="index">飾品欄位index</param>
        /// <returns></returns>
        public static bool IsHairAccessory(ChaControl chaCtrl, int index) {
            if (!lockHairAcc) { return false; }
            return GetChaAccessoryComponent(chaCtrl, index)?.gameObject.GetComponent<ChaCustomHairComponent>() != null;
        }

        public static ChaAccessoryComponent GetChaAccessoryComponent(ChaControl chaCtrl, int index) {
            if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                return MoreAccessories_Support.GetChaAccessoryComponent(chaCtrl, index);
            } else {
                return chaCtrl.GetAccessoryComponent(index);
                //if (index < chaCtrl.cusAcsCmp.Length) {
                //    chaAccessoryComponent = chaCtrl.cusAcsCmp[index];
                //} else {
                //    chaAccessoryComponent = null;
                //}
            }
        }

        private static void LoadCoordinates(ChaControl chaCtrl, ChaControl tmpChaCtrl) {
            ChaFileCoordinate tmpChaFileCoordinate = tmpChaCtrl.nowCoordinate;

            foreach (var tgl in tgls) {
                if (tgl.isOn) {
                    object tmpToggleType = null;
                    int kind = -2;
                    try {
                        tmpToggleType = Enum.Parse(typeof(ClothesKind), tgl.name);
                        kind = Convert.ToInt32(tmpToggleType);
                    } catch (ArgumentException) {
                        kind = -1;
                    }

                    if (kind == 9) {
                        //Copy accessories
                        Queue<int> accQueue = new Queue<int>();
                        ChaFileAccessory.PartsInfo[] chaAccParts = chaCtrl.nowCoordinate.accessory.parts;
                        ChaFileAccessory.PartsInfo[] tmpAccParts = tmpChaFileCoordinate.accessory.parts;
                        for (int i = 0; i < tmpAccParts.Length; i++) {
                            if (!(bool)tgls2[i]?.isOn) {
                                continue;
                            }

                            if (chaAccParts[i].type == 120 && tmpAccParts[i].type == 120) {
                                continue;
                            } else if ((IsHairAccessory(chaCtrl, i) || addAccModeFlag) && chaAccParts[i].type != 120) {
                                //如果要替入的不是空格，就放進accQueue
                                if (tmpAccParts[i].type != 120) {
                                    accQueue.Enqueue(i);
                                    KK_StudioCoordinateLoadOption.Logger.LogDebug($"->Lock: Acc{i} / ID: {chaAccParts[i].id}");
                                    KK_StudioCoordinateLoadOption.Logger.LogDebug($"->EnQueue: Acc{i} / ID: {tmpAccParts[i].id}");
                                } //else continue;
                            } else {
                                CopyAccessory(tmpChaCtrl, i, chaCtrl, i);
                                KK_StudioCoordinateLoadOption.Logger.LogDebug($"->Directly Change: Acc{i} / ID: {chaAccParts[i].id} -> {tmpAccParts[i].id}");
                            }
                        }

                        //遍歷空欄dequeue accQueue
                        for (int j = 0; j < chaAccParts.Length && accQueue.Count > 0; j++) {
                            if (chaAccParts[j].type == 120) {
                                CopyAccessory(tmpChaCtrl, accQueue.Dequeue(), chaCtrl, j);
                                KK_StudioCoordinateLoadOption.Logger.LogDebug($"->DeQueue: Acc{j} / ID: {tmpAccParts[j].id}");
                            } //else continue;
                        }

                        //accQueue內容物太多，放不下的丟到MoreAcc，或是報告後捨棄
                        if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                            MoreAccessories_Support._accQueue = new Queue<int>(accQueue);
                            accQueue.Clear();
                        } else {
                            while (accQueue.Count > 0) {
                                ChaFileAccessory.PartsInfo tmp = tmpAccParts[accQueue.Dequeue()];
                                KK_StudioCoordinateLoadOption.Logger.LogMessage("Accessories slot is not enough! Discard " + GetNameFromIDAndType(tmp.id, (ChaListDefine.CategoryNo)tmp.type));
                            }
                        }
                        KK_StudioCoordinateLoadOption.Logger.LogDebug("->Changed: " + tgl.name);
                    } else if (kind >= 0) {
                        //Change clothes
                        var tmp = MessagePackSerializer.Serialize<ChaFileClothes.PartsInfo>(tmpChaFileCoordinate.clothes.parts[kind]);
                        chaCtrl.nowCoordinate.clothes.parts[kind] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(tmp);
                        chaCtrl.ChangeClothes(kind, tmpChaFileCoordinate.clothes.parts[kind].id, tmpChaFileCoordinate.clothes.subPartsId[0], tmpChaFileCoordinate.clothes.subPartsId[1], tmpChaFileCoordinate.clothes.subPartsId[2], true);

                        KK_StudioCoordinateLoadOption.Logger.LogDebug("->Changed: " + tgl.name + " / ID: " + tmpChaFileCoordinate.clothes.parts[kind].id);
                    }
                }
            }
            chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType);
            chaCtrl.Reload(false, true, false, true);

            HairAccessoryCustomizer_Support.CleanHairAccBackup();
            LoadExtData(chaCtrl, tmpChaCtrl);
        }

        private static void LoadExtData(ChaControl chaCtrl, ChaControl tmpChaCtrl) {
            //Backup KCOX
            if (KK_StudioCoordinateLoadOption._isKCOXExist) {
                KCOX_Support.BackupKCOXData(chaCtrl, chaCtrl.nowCoordinate.clothes);
            }

            //BackupAllAccessories
            if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                MoreAccessories_Support.GetMoreAccInfoLists(chaCtrl, tmpChaCtrl);
                MoreAccessories_Support.CopyAllAccessories();
            }

            //Backup ABMX
            if (KK_StudioCoordinateLoadOption._isABMXExist) {
                ABMX_Support.BackupABMXData(chaCtrl);
            }

            //Backup Material
            if (KK_StudioCoordinateLoadOption._isMaterialEditorExist) {
                MaterialEditor_Support.BackupMaterialData(chaCtrl);
            }

            //fake load
            using (FileStream fileStream = new FileStream(charaFileSort.selectPath, FileMode.Open, FileAccess.Read)) {
                fakeCallFlag_LoadBytes = true;
                chaCtrl.nowCoordinate.LoadFile(fileStream);
                fakeCallFlag_LoadBytes = false;
            }

            ////Rollback All Accessories
            ////因飾品邏輯較複雜，不採用「單純倒回不變更的飾品」的方式
            ////先全部回退後再正向載入飾品，以免邏輯亂套
            //if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
            //    MoreAccessories_Support.CopyMoreAccessoriesData(tmpChaCtrl, chaCtrl);
            //    chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)tmpChaCtrl.fileStatus.coordinateType);
            //    chaCtrl.Reload(false, true, false, true);
            //}
            //if (KK_StudioCoordinateLoadOption._isHairAccessoryCustomizerExist) {
            //    HairAccessoryCustomizer_Support.CopyHairAcc(tmpChaCtrl, chaCtrl);
            //    tmpChaCtrl.nowCoordinate.LoadFile(charaFileSort.selectPath);
            //    tmpChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)tmpChaCtrl.fileStatus.coordinateType);
            //    tmpChaCtrl.Reload(false, true, false, true);
            //}

            //if (KK_StudioCoordinateLoadOption._isHairAccessoryCustomizerExist) {
            //    HairAccessoryCustomizer_Support.BackupHairAccDict(tmpChaCtrl, chaCtrl);
            //}
            //CopyAccessories(tmpChaCtrl, chaCtrl);

            foreach (var tgl in tgls) {
                int kind;
                try {
                    kind = Convert.ToInt32(Enum.Parse(typeof(ClothesKind), tgl.name));
                } catch (ArgumentException) {
                    kind = -1;
                }

                if (tgl.isOn) {
                    //Rollback Parts of MoreAccessories
                    if (kind == 9 && KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                        MoreAccessories_Support.GetMoreAccInfoLists(tmpChaCtrl, chaCtrl);
                        MoreAccessories_Support.RollbackMoreAccessories(tgls2.Select(x => x.isOn).ToArray());
                    }
                } else {
                    if (kind == 9) {
                        //Rollback All MoreAcc
                        if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                            MoreAccessories_Support.GetMoreAccInfoLists(tmpChaCtrl, chaCtrl);
                            MoreAccessories_Support.CopyAllAccessories();
                        }
                    } else if (kind >= 0) {
                        //Rollback KCOX
                        if (KK_StudioCoordinateLoadOption._isKCOXExist) {
                            KCOX_Support.RollbackOverlay(true, kind, chaCtrl);
                            if (kind == 0) {
                                for (int j = 0; j < SubClothesNames.Length; j++) {
                                    KCOX_Support.RollbackOverlay(false, j, chaCtrl);
                                }
                                foreach (var maskKind in KCOX_Support.MaskKind) {
                                    KCOX_Support.RollbackOverlay(true, 0, chaCtrl, maskKind);
                                }
                            }
                        }

                        //Rollback Clothes Material
                        if (KK_StudioCoordinateLoadOption._isMaterialEditorExist) {
                            MaterialEditor_Support.RollbackMaterialData((int)MaterialEditor_Support.ObjectType.Clothing, chaCtrl.fileStatus.coordinateType, kind);
                        }

                        //Rollback ABMX
                        if (KK_StudioCoordinateLoadOption._isABMXExist) {
                            if (kind == 1) {
                                ABMX_Support.RollbackABMXBone(chaCtrl);
                            }
                        }
                    }
                }
            }

            //Rollback Accessories Material
            if (KK_StudioCoordinateLoadOption._isMaterialEditorExist) {
                int rollbackAmount = 20;
                if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                    rollbackAmount = MoreAccessories_Support.GetAccessoriesAmount(tmpChaCtrl);
                }
                for (int i = 0; i < rollbackAmount; i++) {
                    if ((!tgls[(int)ClothesKind.accessories].isOn || tgls2.Length <= i || !tgls2[i].isOn) && null != GetChaAccessoryComponent(chaCtrl, i)?.gameObject) {
                        MaterialEditor_Support.RollbackMaterialData((int)MaterialEditor_Support.ObjectType.Accessory, chaCtrl.fileStatus.coordinateType, i);
                    }
                }
            }

            MaterialEditor_Support.CleanMaterialBackup();
            KCOX_Support.CleanKCOXBackup();
            HairAccessoryCustomizer_Support.CleanHairAccBackup();
            MoreAccessories_Support.CleanMoreAccBackup();

            //KCOX、MaterialEditor需要Reload
            chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType);
            chaCtrl.ChangeCoordinateTypeAndReload(false);
        }

        //public static void CopyAccessories(ChaControl sourceChaCtrl, ChaControl targetChaCtrl) {
        //    if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
        //        MoreAccessories_Support.GetMoreAccInfoLists(sourceChaCtrl, targetChaCtrl);
        //        MoreAccessories_Support.CopyAllAccessories();
        //        return;
        //    }

        //    int accAmount = 20;
        //    //sourceCtrl.ChangeCoordinateTypeAndReload(false);
        //    //targetCtrl.ChangeCoordinateTypeAndReload(false);
        //    for (int i = 0; i < accAmount; i++) {
        //        CopyAccessory(sourceChaCtrl, i, targetChaCtrl, i);
        //    }
        //}

        public static void CopyAccessory(ChaControl sourceChaCtrl, int sourceSlot, ChaControl targetChaControl, int targetSlot) {
            byte[] tmp = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(sourceChaCtrl.nowCoordinate.accessory.parts[sourceSlot] ?? new ChaFileAccessory.PartsInfo());
            targetChaControl.nowCoordinate.accessory.parts[targetSlot] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tmp);
            if (KK_StudioCoordinateLoadOption._isHairAccessoryCustomizerExist) {
                HairAccessoryCustomizer_Support.CopyHairAcc(sourceChaCtrl, sourceSlot, targetChaControl, targetSlot);
            }
        }

        private static bool fakeCallFlag_LoadBytes = false;

        [HarmonyPrefix, HarmonyPatch(typeof(ChaFileCoordinate), "LoadBytes")]
        public static bool LoadBytesPrefix(ref bool __result) {
            __result = true;
            return !fakeCallFlag_LoadBytes;
        }

        //另一插件(KK_ClothesLoadOption)在和我相同的位置畫Panel，將他Block掉
        //因為他的插件在CharaMaker和Studio皆有功能，僅Studio部分和我重疊，故採此對策
        //若是要選擇用他的插件，直接將我移除即可。
        private static void BlockAnotherPlugin() {
            var anotherPlugin = GameObject.Find("StudioScene/Canvas Main Menu/ClosesLoadOption Panel");
            if (null != anotherPlugin) {
                anotherPlugin.transform.localScale = Vector3.zero;
                KK_StudioCoordinateLoadOption.Logger.LogDebug("Block KK_ClothesLoadOption Panel");
            }
        }

        //如果啟動機翻就一律顯示英文，否則機翻會毀了我的文字
        private static void BlockXUATranslate() {
            if (null != KK_StudioCoordinateLoadOption.TryGetPluginInstance("gravydevsupreme.xunity.autotranslator")) {
                var XUAConfigPath = Path.Combine(Paths.ConfigPath, "AutoTranslatorConfig.ini");
                if (IniFile.FromFile(XUAConfigPath).GetSection("TextFrameworks").GetKey("EnableUGUI").Value == "True") {
                    StringResources.StringResourcesManager.SetUICulture("en-US");
                    KK_StudioCoordinateLoadOption.Logger.LogInfo("Found XUnityAutoTranslator Enabled, load English UI");
                }
            }
        }
    }
}