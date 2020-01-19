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
using ExtensibleSaveFormat;
using Extension;
using HarmonyLib;
using Illusion.Extensions;
using Sideloader.AutoResolver;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace KK_StudioCharaOnlyLoadBody {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioCharaOnlyLoadBody : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Chara Only Load Body";
        internal const string GUID = "com.jim60105.kk.studiocharaonlyloadbody";
        internal const string PLUGIN_VERSION = "20.01.20.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.3.7";

        public static ConfigEntry<string> ExtendedDataToCopySetting { get; private set; }
        public static string[] ExtendedDataToCopy;

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            HarmonyWrapper.PatchAll(typeof(Patches));

            string[] SampleArray = {
                "KSOX",
                "com.deathweasel.bepinex.uncensorselector",
                "KKABMPlugin.ABMData",
                "com.bepis.sideloader.universalautoresolver"
            };

            //config.ini設定
            ExtendedDataToCopySetting = Config.Bind<string>("Config", "ExtendedData To Copy", string.Join(";", SampleArray), "If you want to load the ExtendedData when you load the body, add the ExtendedData ID.");
            ExtendedDataToCopySetting.SettingChanged += delegate {
                ExtendedDataToCopy = ExtendedDataToCopySetting.Value.Split(';');
            };
            ExtendedDataToCopy = ExtendedDataToCopySetting.Value.Split(';');
            Patches.Awake();
        }

    }

    class Patches {
        private static readonly ManualLogSource Logger = KK_StudioCharaOnlyLoadBody.Logger;
        private static readonly GameObject[] btn = new GameObject[2];
        internal static Type MoreAccessories = null;

        internal static void Awake() {
            string path = Extension.Extension.TryGetPluginInstance("com.joan6694.illusionplugins.moreaccessories")?.Info.Location;
            if (null != path && path.Length != 0) {
                Assembly ass = Assembly.LoadFrom(path);
                MoreAccessories = ass.GetType("MoreAccessoriesKOI.MoreAccessories");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitCharaList")]
        public static void InitCharaListPostfix(CharaList __instance) {
            //繪製UI
            GameObject original = GameObject.Find("StudioScene/Canvas Main Menu/01_Add/" + __instance.name + "/Button Change");
            int i = (string.Equals(__instance.name, "00_Female") ? 1 : 0);
            btn[i] = UnityEngine.Object.Instantiate(original, original.transform.parent);
            btn[i].name = "Button Keep Coordinate Change";
            btn[i].transform.position += new Vector3(0, -25, 0);
            btn[i].transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(180, -401), new Vector2(390, -380));

            //希望將來可以用文字UI，而不是內嵌於圖片
            switch (Application.systemLanguage) {
                case SystemLanguage.Chinese:
                    btn[i].GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaOnlyLoadBody.Resources.buttonChange.png", 183, 20);
                    break;
                case SystemLanguage.Japanese:
                    btn[i].GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaOnlyLoadBody.Resources.buttonChange_JP.png", 183, 20);
                    break;
                default:
                    btn[i].GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaOnlyLoadBody.Resources.buttonChange_EN.png", 183, 20);
                    break;
            }

            //Button Onclick
            btn[i].GetComponent<Button>().onClick.RemoveAllListeners();
            btn[i].GetComponent<Button>().onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
            btn[i].GetComponent<Button>().onClick.AddListener(() => OnButtonClick(__instance, i));

            //同步按鈕狀態
            SetKeepCoorButtonInteractable(__instance);
        }

        //按鈕邏輯
        private static void OnButtonClick(CharaList __instance, int sex) {
            CharaFileSort charaFileSort = __instance.GetField("charaFileSort") as CharaFileSort;
            ChaFileControl chaFileControl = new ChaFileControl();
            string fullPath = chaFileControl.ConvertCharaFilePath(charaFileSort.selectPath, (byte)sex, false);
            chaFileControl = null;
            OCIChar[] array = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                               select Studio.Studio.GetCtrlInfo(v) as OCIChar into v
                               where v != null
                               select v).ToArray();
            foreach (OCIChar ocichar in array) {
                ChaControl chaCtrl = ocichar.charInfo;
                foreach (OCIChar.BoneInfo boneInfo in (from v in ocichar.listBones
                                                       where v.boneGroup == OIBoneInfo.BoneGroup.Hair
                                                       select v).ToList<OCIChar.BoneInfo>()) {
                    Singleton<GuideObjectManager>.Instance.Delete(boneInfo.guideObject, true);
                }
                ocichar.listBones = (from v in ocichar.listBones
                                     where v.boneGroup != OIBoneInfo.BoneGroup.Hair
                                     select v).ToList<OCIChar.BoneInfo>();
                int[] array2 = (from b in ocichar.oiCharInfo.bones
                                where b.Value.@group == OIBoneInfo.BoneGroup.Hair
                                select b.Key).ToArray<int>();
                for (int j = 0; j < array2.Length; j++) {
                    ocichar.oiCharInfo.bones.Remove(array2[j]);
                }
                ocichar.hairDynamic = null;
                ocichar.skirtDynamic = null;

                string oldName = ocichar.charInfo.chaFile.parameter.fullname;

                ChaControl tmpCtrl = Singleton<Manager.Character>.Instance.CreateFemale(null, -1);
                if (null != MoreAccessories) {
                    CopyAllMoreAccessoriesData(ocichar.charInfo, tmpCtrl);
                }
                //Main Load Control
                if (chaCtrl.chaFile.LoadFileLimited(fullPath, (byte)sex, true, true, true, true, false) || !LoadExtendedData(ocichar, charaFileSort.selectPath, (byte)sex) || !UpdateTreeNodeObjectName(ocichar)) {
                    Logger.LogError("Load Body FAILED");
                } else {
                    if (null != MoreAccessories) {
                        CopyAllMoreAccessoriesData(tmpCtrl, ocichar.charInfo);
                    }
                }
                Logger.LogWarning("Errors may appear below.");
                Logger.LogWarning("Because I removed a dummy used to copy MoreAcc, and some plugins didn't catch related errors.");
                Logger.LogWarning("(｢･ω･)｢ 阿拉花瓜");
                Singleton<Manager.Character>.Instance.DeleteChara(tmpCtrl);

                ocichar.charInfo.AssignCoordinate((ChaFileDefine.CoordinateType)ocichar.charInfo.fileStatus.coordinateType);
                chaCtrl.Reload(false, false, false, false);

                AddObjectAssist.InitHairBone(ocichar, Singleton<Info>.Instance.dicBoneInfo);
                ocichar.hairDynamic = AddObjectFemale.GetHairDynamic(ocichar.charInfo.objHair);
                ocichar.skirtDynamic = AddObjectFemale.GetSkirtDynamic(ocichar.charInfo.objClothes);
                ocichar.InitFK(null);
                foreach (var tmp in FKCtrl.parts.Select((OIBoneInfo.BoneGroup p, int i2) => new { p, i2 })) {
                    ocichar.ActiveFK(tmp.p, ocichar.oiCharInfo.activeFK[tmp.i2], ocichar.oiCharInfo.activeFK[tmp.i2]);
                }
                ocichar.ActiveKinematicMode(OICharInfo.KinematicMode.FK, ocichar.oiCharInfo.enableFK, true);
                ocichar.UpdateFKColor(new OIBoneInfo.BoneGroup[]
                {
                        OIBoneInfo.BoneGroup.Hair
                });
                ocichar.ChangeEyesOpen(ocichar.charFileStatus.eyesOpenMax);
                ocichar.ChangeBlink(ocichar.charFileStatus.eyesBlink);
                ocichar.ChangeMouthOpen(ocichar.oiCharInfo.mouthOpen);

                fakeChangeCharaFlag = true;
                ocichar.ChangeChara(charaFileSort.selectPath);
                fakeChangeCharaFlag = false;

                Logger.LogInfo($"Load Body: {oldName} -> {ocichar.charInfo.chaFile.parameter.fullname}");
            }

        }

        //將我的按鈕和官方的變更按鈕同步狀態
        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "OnDelete")]
        public static void OnDelete(CharaList __instance) {
            SetKeepCoorButtonInteractable(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "OnDeselect")]
        public static void OnDeselect(CharaList __instance) {
            SetKeepCoorButtonInteractable(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "OnSelect")]
        public static void OnSelect(CharaList __instance) {
            SetKeepCoorButtonInteractable(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "OnSelectChara")]
        public static void OnSelectChara(CharaList __instance) {
            SetKeepCoorButtonInteractable(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "OnSort")]
        public static void OnSort(CharaList __instance) {
            SetKeepCoorButtonInteractable(__instance);
        }

        private static void SetKeepCoorButtonInteractable(CharaList __instance) {
            if (null != __instance) {
                int i = (string.Equals(__instance.name, "00_Female") ? 1 : 0);
                if (null != btn[i] && null != btn[i].GetComponent<Button>() && null != __instance.GetField("buttonChange")) {
                    btn[i].GetComponent<Button>().interactable = ((Button)__instance.GetField("buttonChange")).interactable;
                }
            }
        }

        //載入擴充資料
        public static bool LoadExtendedData(OCIChar ocichar, string file, byte sex) {
            ChaFileControl tmpChaFile = new ChaFileControl();
            tmpChaFile.LoadCharaFile(file, sex);

            foreach (string ext in KK_StudioCharaOnlyLoadBody.ExtendedDataToCopy) {
                switch (ext) {
                    case "KKABMPlugin.ABMData":
                        //取得BoneController
                        object BoneController = ocichar.charInfo.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KKABMX.Core"));
                        if (null == BoneController) {
                            Logger.LogDebug("No ABMX BoneController found");
                            break;
                        }

                        //建立重用function
                        void GetModifiers(Action<object> action) {
                            foreach (string boneName in (IEnumerable<string>)BoneController.Invoke("GetAllPossibleBoneNames")) {
                                object modifier = BoneController.Invoke("GetModifier", new object[] { boneName });
                                if (null != modifier) {
                                    action(modifier);
                                }
                            }
                        }

                        //取得舊角色衣服ABMX數據
                        List<object> previousModifier = new List<object>();
                        GetModifiers(x => {
                            if ((bool)x.Invoke("IsCoordinateSpecific")) {
                                previousModifier.Add(x);
                            }
                        });

                        //將擴充資料由暫存複製到角色身上
                        ExtendedSave.SetExtendedDataById(ocichar.charInfo.chaFile, ext, ExtendedSave.GetExtendedDataById(tmpChaFile, ext));

                        //把擴充資料載入ABMX插件
                        BoneController.Invoke("OnReload", new object[] { 2, false });

                        //清理新角色數據，將衣服數據刪除
                        List<object> newModifiers = new List<object>();
                        int i = 0;
                        GetModifiers(x => {
                            if ((bool)x.Invoke("IsCoordinateSpecific")) {
                                Logger.LogDebug("Clean new coordinate ABMX BoneData: " + (string)x.GetProperty("BoneName"));
                                x.Invoke("MakeNonCoordinateSpecific");
                                object y = x.Invoke("GetModifier", new object[] { (ChaFileDefine.CoordinateType)0 });
                                y.Invoke("Clear");
                                x.Invoke("MakeCoordinateSpecific");    //保險起見以免後面沒有成功清除
                                i++;
                            } else {
                                newModifiers.Add(x);
                            }
                        });

                        //將舊的衣服數據合併回到角色身上
                        i = 0;
                        foreach (object modifier in previousModifier) {
                            string bonename = (string)modifier.GetProperty("BoneName");
                            if (!newModifiers.Any(x => string.Equals(bonename, (string)x.GetProperty("BoneName")))) {
                                BoneController.Invoke("AddModifier", new object[] { modifier });
                                Logger.LogDebug("Rollback cooridnate ABMX BoneData: " + bonename);
                            } else {
                                Logger.LogError("Duplicate coordinate ABMX BoneData: " + bonename);
                            }
                            i++;
                        }
                        Logger.LogDebug($"Merge {i} previous ABMX Bone Modifiers");

                        //重整
                        BoneController.SetProperty("NeedsFullRefresh", true);
                        BoneController.SetProperty("NeedsBaselineUpdate", true);
                        BoneController.Invoke("LateUpdate");

                        //把ABMX的數據存進擴充資料
                        BoneController.Invoke("OnCardBeingSaved", new object[] { 1 });
                        BoneController.Invoke("OnReload", new object[] { 2, false });

                        //列出角色身上所有ABMX數據
                        Logger.LogDebug("--List all exist ABMX BoneData--");
                        foreach (string boneName in (IEnumerable<string>)BoneController.Invoke("GetAllPossibleBoneNames", null)) {
                            object modifier = BoneController.Invoke("GetModifier", new object[] { boneName });
                            if (null != modifier) {
                                Logger.LogDebug(boneName);
                            }
                        }
                        Logger.LogDebug("--List End--");
                        break;
                    case "com.bepis.sideloader.universalautoresolver":
                        //判斷CategoryNo分類function
                        bool isBelongsToCharaBody(ChaListDefine.CategoryNo categoryNo) {
                            Type StructReference = typeof(UniversalAutoResolver).Assembly.GetType("Sideloader.AutoResolver.StructReference");
                            return StructReference.GetProperty("ChaFileFaceProperties", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).GetValue(StructReference, null).ToDictionary<object, object>().Keys.Any(x => (ChaListDefine.CategoryNo)x.GetField("Category") == categoryNo) ||
                                StructReference.GetProperty("ChaFileBodyProperties", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).GetValue(StructReference, null).ToDictionary<object, object>().Keys.Any(x => (ChaListDefine.CategoryNo)x.GetField("Category") == categoryNo) ||
                                StructReference.GetProperty("ChaFileHairProperties", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).GetValue(StructReference, null).ToDictionary<object, object>().Keys.Any(x => (ChaListDefine.CategoryNo)x.GetField("Category") == categoryNo) ||
                                StructReference.GetProperty("ChaFileMakeupProperties", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).GetValue(StructReference, null).ToDictionary<object, object>().Keys.Any(x => (ChaListDefine.CategoryNo)x.GetField("Category") == categoryNo);
                        }

                        //extInfo整理
                        int cleanExtData(ref PluginData tmpExtData, bool keepBodyData) {
                            tmpExtData = ExtendedSave.GetExtendedDataById(ocichar.charInfo.chaFile, ext);
                            if (tmpExtData != null && tmpExtData.data.ContainsKey("info")) {
                                if (tmpExtData.data.TryGetValue("info", out object tmpExtInfo)) {
                                    if (null != tmpExtInfo as object[]) {
                                        List<object> tmpExtList = new List<object>(tmpExtInfo as object[]);
                                        Logger.LogDebug($"Sideloader count: {tmpExtList.Count}");
                                        ResolveInfo tmpResolveInfo;
                                        for (int j = 0; j < tmpExtList.Count;) {
                                            tmpResolveInfo = (ResolveInfo)typeof(ResolveInfo).InvokeMember("Deserialize", BindingFlags.Default | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod, null, null, new object[] { (byte[])tmpExtList[j] });
                                            if (keepBodyData == isBelongsToCharaBody(tmpResolveInfo.CategoryNo)) {
                                                Logger.LogDebug($"Add Sideloader info: {tmpResolveInfo.GUID} : {tmpResolveInfo.Property} : {tmpResolveInfo.Slot}");
                                                j++;
                                            } else {
                                                Logger.LogDebug($"Remove Sideloader info: {tmpResolveInfo.GUID} : {tmpResolveInfo.Property} : {tmpResolveInfo.Slot}");
                                                tmpExtList.RemoveAt(j);
                                            }
                                        }
                                        tmpExtData.data["info"] = tmpExtList.ToArray();
                                        return tmpExtList.Count;
                                    }
                                }
                            }
                            return 0;
                        }

                        //提出角色身上原始的Sideloader extData
                        PluginData oldExtData = ExtendedSave.GetExtendedDataById(ocichar.charInfo.chaFile, ext);
                        Logger.LogDebug($"Get Old Sideloader Start");
                        int L1 = cleanExtData(ref oldExtData, false);
                        Logger.LogDebug($"Get Old Sideloader: {L1}");

                        //將擴充資料由暫存複製到角色身上
                        ExtendedSave.SetExtendedDataById(ocichar.charInfo.chaFile, ext, ExtendedSave.GetExtendedDataById(tmpChaFile, ext));

                        //清理新角色數據
                        PluginData newExtData = ExtendedSave.GetExtendedDataById(ocichar.charInfo.chaFile, ext);
                        Logger.LogDebug($"Get New Sideloader Start");
                        int L2 = cleanExtData(ref newExtData, true);
                        Logger.LogDebug($"Get New Sideloader: {L2}");

                        //合併新舊數據
                        object[] tmpObj = new object[L1 + L2];
                        (oldExtData?.data?["info"] as object[])?.CopyTo(tmpObj, 0);
                        (newExtData?.data?["info"] as object[])?.CopyTo(tmpObj, L1);
                        PluginData extData = null;
                        if (tmpObj.Length != 0) {
                            extData = new PluginData {
                                data = new Dictionary<string, object> {
                                    ["info"] = tmpObj
                                }
                            };
                        }

                        //儲存
                        ExtendedSave.SetExtendedDataById(ocichar.charInfo.chaFile, ext, extData);
                        Logger.LogDebug($"Merge and Save Sideloader: {tmpObj.Length}");

                        //調用原始sideloader載入hook function
                        typeof(UniversalAutoResolver).GetNestedType("Hooks", BindingFlags.NonPublic)
                            .InvokeMember("ExtendedCardLoad", BindingFlags.Default | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod, null, null,
                                new object[] { ocichar.charInfo.chaFile });

                        Logger.LogDebug("Sideloader Data Loaded");
                        break;
                    default:
                        ExtendedSave.SetExtendedDataById(ocichar.charInfo.chaFile, ext, ExtendedSave.GetExtendedDataById(tmpChaFile, ext));
                        Logger.LogDebug($"Change Extended Data: {ext}");
                        break;
                }
                MonoBehaviour KCOXController = ocichar.charInfo.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KoiClothesOverlayX"));
                KCOXController?.Invoke("OnCardBeingSaved", new object[] { 1 });
            }


            return true;
        }

        private static bool fakeCopyCall = false;
        [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), "CopyAll")]
        public static bool CopyAllPrefix() {
            //Logger.LogDebug("Block Origin Copy?:"+fakeCopyCall);
            return !fakeCopyCall;
        }

        /// <summary>
        /// 將所有的MoreAccessories飾品由來源對象複製到目標對象
        /// </summary>
        /// <param name="oriChaCtrl">來源對象</param>
        /// <param name="targetChaCtrl">目標對象</param>
        public static void CopyAllMoreAccessoriesData(ChaControl oriChaCtrl, ChaControl targetChaCtrl) {
            fakeCopyCall = true;
            targetChaCtrl.chaFile.CopyAll(oriChaCtrl.chaFile);
            fakeCopyCall = false;

            MoreAccessories.InvokeMember("Update",
                BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null),
                null);

            Logger.LogDebug("Copy MoreAccessories Finish");
        }

        //右側選單的名字更新
        public static bool UpdateTreeNodeObjectName(OCIChar oCIChar) {
            oCIChar.charInfo.name = oCIChar.charInfo.chaFile.parameter.fullname;
            oCIChar.charInfo.chaFile.SetProperty("charaFileName", oCIChar.charInfo.chaFile.parameter.fullname);
            oCIChar.treeNodeObject.textName = oCIChar.charInfo.chaFile.parameter.fullname;
            Logger.LogDebug("Set Name to: " + oCIChar.charInfo.chaFile.parameter.fullname);

            return true;
        }

        //Some plugins hook on this function, so call it to trigger them. (Example: KKAPI.Chara.OnReload)
        private static bool fakeChangeCharaFlag = false;
        [HarmonyPrefix, HarmonyPatch(typeof(OCIChar), "ChangeChara")]
        public static bool ChangeCharaPrefix(OCIChar __instance) {
            return !fakeChangeCharaFlag;
        }
    }
}
