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
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Extension;
using Harmony;
using MessagePack;
using Sideloader.AutoResolver;
using Studio;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace KK_StudioCoordinateLoadOption {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioCoordinateLoadOption : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Coordinate Load Option";
        internal const string GUID = "com.jim60105.kk.studiocoordinateloadoption";
        internal const string PLUGIN_VERSION = "19.07.26.0";

        public void Awake() {
            UIUtility.Init();
            HarmonyInstance harmonyInstance = HarmonyInstance.Create(GUID);
            harmonyInstance.PatchAll(typeof(Patches));
            harmonyInstance.PatchAll(typeof(MoreAccessories_Support));
            harmonyInstance.Patch(typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("Init", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(Patches.InitPostfix), null), null);
            harmonyInstance.Patch(typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("OnClickLoad", AccessTools.all), new HarmonyMethod(typeof(Patches), nameof(Patches.OnClickLoadPrefix), null), null, null);
            harmonyInstance.Patch(typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("OnSelect", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(Patches.OnSelectPostfix), null), null);
        }

        public static bool _isKCOXExist = false;
        public static bool _isABMXExist = false;
        public static bool _isMoreAccessoriesExist = false;

        public void Start() {
            bool IsPluginExist(string pluginName) {
                return null != BepInEx.Bootstrap.Chainloader.Plugins.Select(MetadataHelper.GetMetadata).FirstOrDefault(x => x.GUID == pluginName);
            }

            _isKCOXExist = IsPluginExist("KCOX") && KCOX_Support.LoadAssembly();
            _isABMXExist = IsPluginExist("KKABMX.Core") && ABMX_Support.LoadAssembly();
            _isMoreAccessoriesExist = IsPluginExist("com.joan6694.illusionplugins.moreaccessories") && MoreAccessories_Support.LoadAssembly();
        }
    }

    class Patches {
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

        public static string[] ClothesKindName = ClothesKindNameJp;

        public static readonly string[] ClothesKindNameJp = {
            "トップス",
            "ボトムス",
            "ブラ",
            "ショーツ",
            "手袋",
            "パンスト",
            "靴下",
            "靴(內履き)",
            "靴(外履き)",
            "アクセサリー"
        };

        public static readonly string[] ClothesKindNameCh = {
            "上衣",
            "下裝",
            "胸罩",
            "內褲",
            "手套",
            "褲襪",
            "襪子",
            "室內鞋",
            "室外鞋",
            "飾品"
        };

        public static readonly string[] ClothesKindNameEn = {
            "Tops",
            "Bottoms",
            "Bra",
            "Shorts",
            "Gloves",
            "Pantyhose",
            "Socks",
            "Indoor shoes",
            "Outdoor shoes",
            "Accessories"
        };

        private static Toggle[] tgls2 = new Toggle[0]; //使用時再初始化
        private static Toggle[] tgls;
        private static Image panel2;
        private static RectTransform toggleGroup;
        public static void InitPostfix(object __instance) {
            BlockAnotherPlugin();

            //依照系統語言選擇UI語言
            switch (Application.systemLanguage) {
                case SystemLanguage.Chinese:
                    ClothesKindName = ClothesKindNameCh;
                    break;
                case SystemLanguage.Japanese:
                    ClothesKindName = ClothesKindNameJp;
                    break;
                default:
                    ClothesKindName = ClothesKindNameEn;
                    break;
            }
            //如果找到機翻就一律顯示英文，否則機翻會毀了我的文字
            if (null != GameObject.Find("___XUnityAutoTranslator")) {
                ClothesKindName = ClothesKindNameEn;
                Logger.Log(LogLevel.Info, "[KK_SCLO] Found XUnityAutoTranslator, load English UI");
            }

            Array ClothesKindArray = Enum.GetValues(typeof(ClothesKind));

            //Draw Panel and ButtonAll
            charaFileSort = (CharaFileSort)__instance.GetField("fileSort");
            Image panel = UIUtility.CreatePanel("CoordinateTooglePanel", charaFileSort.root.parent.parent.parent);
            Button btnAll = UIUtility.CreateButton("BtnAll", panel.transform, "All");
            btnAll.GetComponentInChildren<Text>(true).color = Color.white;
            btnAll.GetComponent<Image>().color = Color.gray;
            btnAll.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -30), new Vector2(140f, -5f));
            btnAll.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            //Draw Toggles
            tgls = new Toggle[ClothesKindArray.Length];
            for (int i = 0; i < ClothesKindArray.Length; i++) {
                tgls[i] = UIUtility.CreateToggle(ClothesKindArray.GetValue(i).ToString(), panel.transform, ClothesKindName.GetValue(i).ToString());
                tgls[i].GetComponentInChildren<Text>(true).alignment = TextAnchor.MiddleLeft;
                tgls[i].GetComponentInChildren<Text>(true).color = Color.white;
                tgls[i].transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -60f - 25f * i), new Vector2(140f, -35f - 25f * i));
                tgls[i].GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));
                if (tgls[i].name == "accessories") {
                    tgls[i].onValueChanged.AddListener((x) => {
                        if (tgls2.Length != 0 && null != panel2) {
                            panel2.gameObject.SetActive(x);
                        }
                    });
                }
            }
            panel.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(405f, 52.5f), new Vector2(150f, 340f));
            panel.GetComponent<Image>().color = new Color32(80, 80, 80, 220);


            //Draw accessories panel
            panel2 = UIUtility.CreatePanel("AccessoriesTooglePanel", panel.transform);
            Button btnAll2 = UIUtility.CreateButton("BtnAll2", panel2.transform, "All");
            btnAll2.GetComponentInChildren<Text>(true).color = Color.white;
            btnAll2.GetComponent<Image>().color = Color.gray;
            btnAll2.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -30), new Vector2(170f, -5f));
            btnAll2.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));
            panel2.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(150f, -250f), new Vector2(180f, 287.5f));
            panel2.GetComponent<Image>().color = new Color32(80, 80, 80, 220);

            ScrollRect scrollRect = UIUtility.CreateScrollView("scroll", panel2.transform);
            toggleGroup = scrollRect.content;
            scrollRect.transform.SetRect(Vector2.zero, new Vector2(1f, 1f), Vector2.zero, new Vector2(0, -35f));
            scrollRect.GetComponent<Image>().color = new Color32(0, 0, 0, 0);
            scrollRect.scrollSensitivity = 30;
            panel2.gameObject.SetActive(false);

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
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Draw Panel Finish");
        }

        internal static void OnSelectPostfix(object __instance) {
            if (null == panel2) {
                return;
            }

            string[] accNames = LoadAccNames(charaFileSort.selectPath);

            foreach (var tgl in panel2.gameObject.GetComponentsInChildren<Toggle>()) {
                GameObject.Destroy(tgl.gameObject);
            }
            tgls2 = new Toggle[accNames.Length];
            for (int i = 0; i < accNames.Length; i++) {
                tgls2[i] = UIUtility.CreateToggle(Enum.GetValues(typeof(ClothesKind)).GetValue(9).ToString(), toggleGroup.transform, accNames[i]);
                tgls2[i].GetComponentInChildren<Text>(true).alignment = TextAnchor.MiddleLeft;
                tgls2[i].GetComponentInChildren<Text>(true).color = Color.white;
                tgls2[i].transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -25f * (i + 1)), new Vector2(175f, -25f * i));
                tgls2[i].GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));
            }
            toggleGroup.transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(0, -(25f * (accNames.Length - 20))), new Vector2(0, 0));
            panel2.gameObject.SetActive(true);
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Onselect");
        }

        internal static List<ListInfoBase> accessoriesList = new List<ListInfoBase>();
        private static void GetAccList() {
            if (accessoriesList.Count != 0) {
                return;
            }
            ChaListControl chaListCtrl = Singleton<Manager.Character>.Instance.chaListCtrl;
            for (int i = 121; i < 131; i++) {
                accessoriesList.AddRange(chaListCtrl.GetCategoryInfo((ChaListDefine.CategoryNo)i).Values.ToList());
            }
            Logger.Log(LogLevel.Debug, "[KK_SCLO] AccList Length: " + accessoriesList.Count);
        }

        public static string TryGetResolutionInfo(int id, ChaListDefine.CategoryNo categoryNo) {
            var resolveInfo = UniversalAutoResolver.LoadedResolutionInfo?.ToList()?.FirstOrDefault(x => x.CategoryNo == categoryNo && x.Slot == id);
            if (null != resolveInfo) {
                return Singleton<Manager.Character>.Instance.chaListCtrl.GetListInfo(categoryNo, resolveInfo.LocalSlot)?.Name;
            } else {
                return "";
            }
        }

        private static string[] LoadAccNames(string path) {
            //GetAccList();
            var tmpChaFileCoordinate = new ChaFileCoordinate();
            tmpChaFileCoordinate.LoadFile(path);
            List<string> result = new List<string>();
            ChaListControl chaListControl = Singleton<Manager.Character>.Instance.chaListCtrl;

            result.AddRange(tmpChaFileCoordinate.accessory.parts.Select(x => {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Find id: " + x.id);

                string name = "";
                if (x.id == 0) {
                    name = "空";
                }
                if (null == name || "" == name) {
                    name = chaListControl.GetListInfo((ChaListDefine.CategoryNo)x.type, x.id)?.Name;
                }
                if (null == name || "" == name) {
                    name = TryGetResolutionInfo(x.id, (ChaListDefine.CategoryNo)x.type);
                }
                if (null == name || "" == name) {
                    name = "未識別";
                }
                return name;
            }));

            if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                result.AddRange(MoreAccessories_Support.LoadMoreAccNames(tmpChaFileCoordinate));
            }

            return result.ToArray();
        }

        //Backup
        public static bool OnClickLoadPrefix() {
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Studio Coordinate Load Option Start");

            bool isAllTrueFlag = true;
            bool isAllFalseFlag = true;
            foreach (Toggle tgl in tgls) {
                isAllTrueFlag &= tgl.isOn;
                isAllFalseFlag &= !tgl.isOn;
            }
            foreach (Toggle tgl in tgls2) {
                isAllTrueFlag &= tgl.isOn;
                isAllFalseFlag &= !tgl.isOn;
            }
            if (isAllFalseFlag) {
                Logger.Log(LogLevel.Info, "[KK_SCLO] No Toogle selected, skip loading coordinate");
                tgls = null;
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Studio Coordinate Load Option Finish");
                return false;
            }

            OCIChar[] array = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                               select Studio.Studio.GetCtrlInfo(v) as OCIChar into v
                               where v != null
                               select v).ToArray();
            if (isAllTrueFlag) {
                Logger.Log(LogLevel.Info, "[KK_SCLO] Toggle all true, use original game function");
                foreach (var ocichar in array) {
                    ocichar.LoadClothesFile(charaFileSort.selectPath);
                }
            } else {
                ChaControl tmpChaCtrl = Singleton<Manager.Character>.Instance.CreateFemale(null, -1);
                tmpChaCtrl.nowCoordinate.LoadFile(charaFileSort.selectPath);
                foreach (var ocichar in array) {
                    LoadCoordinates(ocichar.charInfo, tmpChaCtrl);
                }
                Singleton<Manager.Character>.Instance.DeleteChara(tmpChaCtrl);

                Logger.Log(LogLevel.Debug, "[KK_SCLO] Studio Coordinate Load Option Finish");
            }
            return false;
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
                        for (int i = 0; i < tmpChaFileCoordinate.accessory.parts.Length; i++) {
                            if ((bool)tgls2[i]?.isOn) {
                                var tmp = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(tmpChaFileCoordinate.accessory.parts[i]);
                                chaCtrl.nowCoordinate.accessory.parts[i] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tmp);
                                chaCtrl.ChangeAccessory(i, tmpChaFileCoordinate.accessory.parts[i].type, tmpChaFileCoordinate.accessory.parts[i].id, tmpChaFileCoordinate.accessory.parts[i].parentKey, true);
                            }
                            Logger.Log(LogLevel.Debug, $"[KK_SCLO] ->Acc {i} id : {tmpChaFileCoordinate.accessory.parts[i].id}");
                        }
                        Logger.Log(LogLevel.Debug, "[KK_SCLO] ->Change: " + tgl.name);
                    } else if (kind >= 0) {
                        //Change clothes
                        chaCtrl.ChangeClothes(kind, tmpChaFileCoordinate.clothes.parts[kind].id, tmpChaFileCoordinate.clothes.subPartsId[0], tmpChaFileCoordinate.clothes.subPartsId[1], tmpChaFileCoordinate.clothes.subPartsId[2], true);
                        chaCtrl.nowCoordinate.clothes.parts[kind].colorInfo = tmpChaFileCoordinate.clothes.parts[kind].colorInfo;

                        Logger.Log(LogLevel.Debug, "[KK_SCLO] ->Change: " + tgl.name + " / ID: " + tmpChaFileCoordinate.clothes.parts[kind].id);
                    }
                }
            }
            chaCtrl.Reload(false, true, true, true);
            chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType);

            LoadExtData(chaCtrl, tmpChaCtrl);
        }

        private static void LoadExtData(ChaControl chaCtrl, ChaControl tmpChaCtrl) {
            //Backup KCOX
            if (KK_StudioCoordinateLoadOption._isKCOXExist) {
                KCOX_Support.BackupKCOXData(chaCtrl, chaCtrl.nowCoordinate.clothes);
            }

            //Backup MoreAcc
            if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                MoreAccessories_Support.CopyMoreAccessoriesData(chaCtrl.chaFile, tmpChaCtrl.chaFile);
            }

            //Backup ABMX
            if (KK_StudioCoordinateLoadOption._isABMXExist) {
                ABMX_Support.BackupABMXData(chaCtrl);
            }

            //fake load
            using (FileStream fileStream = new FileStream(charaFileSort.selectPath, FileMode.Open, FileAccess.Read)) {
                fakeLoadFlag = true;
                chaCtrl.nowCoordinate.LoadFile(fileStream);
                fakeLoadFlag = false;
            }

            foreach (var tgl in tgls) {
                int kind;
                try {
                    kind = Convert.ToInt32(Enum.Parse(typeof(ClothesKind), tgl.name));
                } catch (ArgumentException) {
                    kind = -1;
                }
                if (!tgl.isOn) {
                    if (kind == 9) {
                        //Rollback MoreAcc
                        if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                            MoreAccessories_Support.CopyMoreAccessoriesData(tmpChaCtrl.chaFile, chaCtrl.chaFile);
                        }
                    } else if (kind >= 0) {
                        //Rollback KCOX
                        if (KK_StudioCoordinateLoadOption._isKCOXExist) {
                            KCOX_Support.RollbackOverlay(true, kind);
                            if (kind == 0) {
                                for (int j = 0; j < 3; j++) {
                                    KCOX_Support.RollbackOverlay(false, j);
                                }
                            }
                        }
                        if (kind == 1) {
                            //Rollback ABMX
                            if (KK_StudioCoordinateLoadOption._isABMXExist) {
                                ABMX_Support.RollbackABMXBone(chaCtrl);
                            }
                        }
                    }
                } else {
                    if (kind == 9) {
                        if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
                            MoreAccessories_Support.CopyMoreAccessoriesData(tmpChaCtrl.chaFile, chaCtrl.chaFile, (ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType, tgls2.Select(x => !x.isOn).Skip(20).ToArray());
                        }
                    }
                }
            }

            KCOX_Support.CleanKCOXBackup();
        }

        private static bool fakeLoadFlag = false;
        [HarmonyPrefix, HarmonyPatch(typeof(ChaFileCoordinate), "LoadBytes")]
        public static bool LoadBytesPrefix(ref bool __result) {
            __result = fakeLoadFlag;
            return !fakeLoadFlag;
        }

        //另一插件(KK_ClothesLoadOption)在和我相同的位置畫Panel，將他Block掉
        //因為他的插件在CharaMaker和Studio皆有功能，僅Studio部分和我重疊，故採此對策
        //若是要選擇用他的插件，直接將我這插件移除即可。
        private static void BlockAnotherPlugin() {
            var anotherPlugin = GameObject.Find("StudioScene/Canvas Main Menu/ClosesLoadOption Panel");
            if (null != anotherPlugin) {
                anotherPlugin.transform.localScale = Vector3.zero;
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Block KK_ClothesLoadOption Panel");
            }
        }
    }
}
