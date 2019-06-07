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
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Extension;
using Harmony;
using MessagePack;
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
        internal const string PLUGIN_VERSION = "19.06.07.0";

        public void Awake() {
            UIUtility.Init();
            HarmonyInstance harmonyInstance = HarmonyInstance.Create(GUID);
            harmonyInstance.PatchAll(typeof(Patches));
            harmonyInstance.PatchAll(typeof(MoreAccessories_Support));
            harmonyInstance.Patch(typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("Init", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(Patches.InitPostfix), null), null);
            harmonyInstance.Patch(typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("OnClickLoad", AccessTools.all), new HarmonyMethod(typeof(Patches), nameof(Patches.OnClickLoadPrefix), null), null, null);
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
        private static MPCharCtrl mpCharCtrl;
        private static Toggle[] toggleList = null;

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
            Image panel = UIUtility.CreatePanel("TooglePanel", charaFileSort.root.parent.parent.parent);
            Button btnAll = UIUtility.CreateButton("BtnAll", panel.transform, "All");
            btnAll.GetComponentInChildren<Text>(true).color = Color.white;
            btnAll.GetComponent<Image>().color = Color.gray;
            btnAll.transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -30), new Vector2(140f, -5f));
            btnAll.GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(5f, 1f), new Vector2(-5f, -2f));

            //Draw Toggles
            Toggle[] tgls = new Toggle[ClothesKindArray.Length];
            for (int i = 0; i < ClothesKindArray.Length; i++) {
                tgls[i] = UIUtility.CreateToggle(ClothesKindArray.GetValue(i).ToString(), panel.transform, ClothesKindName.GetValue(i).ToString());
                tgls[i].GetComponentInChildren<Text>(true).alignment = TextAnchor.UpperLeft;
                tgls[i].GetComponentInChildren<Text>(true).color = Color.white;
                tgls[i].transform.SetRect(Vector2.up, Vector2.up, new Vector2(5f, -60f - 25f * i), new Vector2(140f, -35f - 25f * i));
                tgls[i].GetComponentInChildren<Text>(true).transform.SetRect(Vector2.zero, new Vector2(1f, 1f), new Vector2(20.09f, 2.5f), new Vector2(-5.13f, -0.5f));
            }

            panel.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(405f, 52.5f), new Vector2(150f, 340f));
            panel.GetComponent<Image>().color = new Color32(80, 80, 80, 220);

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
                    if (!tgls[i].isOn) {
                        flag = true;
                    }
                    tgls[i].isOn = true;
                }
                if (!flag) {
                    for (int j = 0; j < tgls.Length; j++) {
                        tgls[j].isOn = false;
                    }
                }
            });
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Draw Panel Finish");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MPCharCtrl), "OnClickRoot")]
        public static void OnClickRootPostfix(MPCharCtrl __instance, int _idx) {
            if (_idx > 0 && __instance != null) {
                mpCharCtrl = __instance;
            }
        }

        //Backup
        public static bool OnClickLoadPrefix() {
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Studio Coordinate Load Option Start");
            toggleList = charaFileSort.root.parent.parent.parent.GetComponentsInChildren<Toggle>();

            bool flag = true;
            bool flag2 = false;
            foreach (Toggle tgl in toggleList) {
                flag &= tgl.isOn;
                flag2 |= tgl.isOn;
            }
            if (flag) {
                Logger.Log(LogLevel.Info, "[KK_SCLO] Toggle all true, use original game function");
                toggleList = null;
                return true;
            }
            if (!flag2) {
                Logger.Log(LogLevel.Info, "[KK_SCLO] No Toogle selected, skip loading coordinate");
                toggleList = null;
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Studio Coordinate Load Option Finish");
                return false;
            }
            LoadCoordinates();
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Studio Coordinate Load Option Finish");
            return false;
        }

        private static void LoadCoordinates() {
            ChaControl chaCtrl = mpCharCtrl.ociChar.charInfo;
            ChaControl tmpChaCtrl = Singleton<Manager.Character>.Instance.CreateFemale(null, -1);
            ChaFileCoordinate tmpChaFileCoordinate = tmpChaCtrl.nowCoordinate;

            tmpChaFileCoordinate.LoadFile(charaFileSort.selectPath);

            foreach (var tgl in toggleList) {
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
                        chaCtrl.nowCoordinate.accessory = new ChaFileAccessory();
                        for (int i = 0; i < tmpChaFileCoordinate.accessory.parts.Length; i++) {
                            var tmp = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(tmpChaFileCoordinate.accessory.parts[i]);
                            chaCtrl.nowCoordinate.accessory.parts[i] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tmp);
                            chaCtrl.ChangeAccessory(i, tmpChaFileCoordinate.accessory.parts[i].type, tmpChaFileCoordinate.accessory.parts[i].id, tmpChaFileCoordinate.accessory.parts[i].parentKey, true);
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

            Singleton<Manager.Character>.Instance.DeleteChara(tmpChaCtrl);
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

            foreach (var tgl in toggleList) {
                if (!tgl.isOn) {
                    object tmpToggleType = null;
                    int kind;
                    try {
                        tmpToggleType = Enum.Parse(typeof(ClothesKind), tgl.name);
                        kind = Convert.ToInt32(tmpToggleType);
                    } catch (ArgumentException) {
                        kind = -1;
                    }

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
