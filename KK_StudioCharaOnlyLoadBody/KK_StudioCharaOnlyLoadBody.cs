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
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using Extension;
using Harmony;
using Illusion.Extensions;
using Sideloader.AutoResolver;
using Studio;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace KK_StudioCharaOnlyLoadBody {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioCharaOnlyLoadBody : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Chara Only Load Body";
        internal const string GUID = "com.jim60105.kk.studiocharaonlyloadbody";
        internal const string PLUGIN_VERSION = "19.06.23.0";

        public void Awake() {
            HarmonyInstance.Create(GUID).PatchAll(typeof(Patches));
            BepInEx.Config.ReloadConfig();
            ExtendedDataToCopy = BepInEx.Config.GetEntry("ExtendedDataToCopy", string.Join(";", ExtendedDataToCopy), GUID).Split(';');
        }

        public static string[] ExtendedDataToCopy = {
            "KSOX",
            "com.deathweasel.bepinex.bodyshaders",
            "com.deathweasel.bepinex.uncensorselector",
            "KKABMPlugin.ABMData",
            "com.bepis.sideloader.universalautoresolver"
        };
    }

    class Patches {
        private static GameObject[] btn = new GameObject[2];
        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "InitCharaList")]
        public static void InitCharaListPostfix(CharaList __instance) {
            //繪製UI
            var original = GameObject.Find("StudioScene/Canvas Main Menu/01_Add/" + __instance.name + "/Button Change");
            int i = (String.Equals(__instance.name, "00_Female") ? 1 : 0);
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
            var charaFileSort = __instance.GetField("charaFileSort") as CharaFileSort;
            var chaFileControl = new ChaFileControl();
            var fullPath = chaFileControl.ConvertCharaFilePath(charaFileSort.selectPath, (byte)sex, false);
            chaFileControl = null;
            OCIChar[] array = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                               select Studio.Studio.GetCtrlInfo(v) as OCIChar into v
                               where v != null
                               select v).ToArray();
            foreach (var ocichar in array) {
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

                //Main Load Control
                if (chaCtrl.chaFile.LoadFileLimited(fullPath, (byte)sex, true, true, true, true, false) || !LoadExtendedData(ocichar, charaFileSort.selectPath, (byte)sex) || !UpdateTreeNodeObjectName(ocichar)) {
                    Logger.Log(LogLevel.Error, "[KK_SCOLB] Load Body FAILED");
                }
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

                Logger.Log(LogLevel.Info, $"[KK_SCOLB] Load Body: {oldName} -> {ocichar.charInfo.chaFile.parameter.fullname}");
            }

        }

        //將我的按鈕和官方的變更按鈕同步狀態
        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "OnDelete")]
        public static void OnDelete(CharaList __instance) => SetKeepCoorButtonInteractable(__instance);

        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "OnDeselect")]
        public static void OnDeselect(CharaList __instance) => SetKeepCoorButtonInteractable(__instance);

        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "OnSelect")]
        public static void OnSelect(CharaList __instance) => SetKeepCoorButtonInteractable(__instance);

        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "OnSelectChara")]
        public static void OnSelectChara(CharaList __instance) => SetKeepCoorButtonInteractable(__instance);

        [HarmonyPostfix, HarmonyPatch(typeof(CharaList), "OnSort")]
        public static void OnSort(CharaList __instance) => SetKeepCoorButtonInteractable(__instance);

        private static void SetKeepCoorButtonInteractable(CharaList __instance) {
            int i = (String.Equals(__instance.name, "00_Female") ? 1 : 0);
            btn[i].GetComponent<Button>().interactable = ((Button)__instance.GetField("buttonChange")).interactable;
        }

        //載入擴充資料
        public static bool LoadExtendedData(OCIChar ocichar, string file, byte sex) {
            ChaFileControl tmpChaFile = new ChaFileControl();
            tmpChaFile.LoadCharaFile(file, sex);

            foreach (var ext in KK_StudioCharaOnlyLoadBody.ExtendedDataToCopy) {
                switch (ext) {
                    case "KKABMPlugin.ABMData":
                        //取得BoneController
                        object BoneController = ocichar.charInfo.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KKABMX.Core"));
                        if (null == BoneController) {
                            Logger.Log(LogLevel.Debug, "[KK_SCOLB] No ABMX BoneController found");
                            break;
                        }

                        //建立重用function
                        void GetModifiers(Action<object> action) {
                            foreach (string boneName in (IEnumerable<string>)BoneController.Invoke("GetAllPossibleBoneNames")) {
                                var modifier = BoneController.Invoke("GetModifier", new object[] { boneName });
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
                                Logger.Log(LogLevel.Debug, "[KK_SCOLB] Clean new coordinate ABMX BoneData: " + (string)x.GetProperty("BoneName"));
                                x.Invoke("MakeNonCoordinateSpecific");
                                var y = x.Invoke("GetModifier", new object[] { (ChaFileDefine.CoordinateType)0 });
                                y.Invoke("Clear");
                                x.Invoke("MakeCoordinateSpecific");    //保險起見以免後面沒有成功清除
                                i++;
                            } else {
                                newModifiers.Add(x);
                            }
                        });
                        BoneController.Invoke("ModifiersPurgeEmpty");

                        //將舊的衣服數據合併回到角色身上
                        i = 0;
                        foreach (var modifier in previousModifier) {
                            string bonename = (string)modifier.GetProperty("BoneName");
                            if (!newModifiers.Any(x => String.Equals(bonename, (string)x.GetProperty("BoneName")))) {
                                BoneController.Invoke("AddModifier", new object[] { modifier });
                                Logger.Log(LogLevel.Debug, "[KK_SCOLB] Rollback cooridnate ABMX BoneData: " + bonename);
                            } else {
                                Logger.Log(LogLevel.Error, "[KK_SCOLB] Duplicate coordinate ABMX BoneData: " + bonename);
                            }
                            i++;
                        }
                        Logger.Log(LogLevel.Debug, $"[KK_SCOLB] Merge {i} previous ABMX Bone Modifiers");

                        //重整
                        BoneController.SetProperty("NeedsFullRefresh", true);
                        BoneController.SetProperty("NeedsBaselineUpdate", true);
                        BoneController.Invoke("LateUpdate");

                        //把ABMX的數據存進擴充資料
                        BoneController.Invoke("OnCardBeingSaved", new object[] { 1 });
                        BoneController.Invoke("OnReload", new object[] { 2, false });

                        //列出角色身上所有ABMX數據
                        Logger.Log(LogLevel.Debug, "[KK_SCOLB] --List all exist ABMX BoneData--");
                        foreach (string boneName in (IEnumerable<string>)BoneController.Invoke("GetAllPossibleBoneNames", null)) {
                            var modifier = BoneController.Invoke("GetModifier", new object[] { boneName });
                            if (null != modifier) {
                                Logger.Log(LogLevel.Debug, "[KK_SCOLB] " + boneName);
                            }
                        }
                        Logger.Log(LogLevel.Debug, "[KK_SCOLB] --List End--");
                        break;
                    case "com.bepis.sideloader.universalautoresolver":
                        //判斷CategoryNo分類function
                        bool isBelongsToCharaBody(ChaListDefine.CategoryNo categoryNo) =>
                            StructReference.ChaFileFaceProperties.Keys.Any(x => x.Category == categoryNo) ||
                            StructReference.ChaFileBodyProperties.Keys.Any(x => x.Category == categoryNo) ||
                            StructReference.ChaFileHairProperties.Keys.Any(x => x.Category == categoryNo) ||
                            StructReference.ChaFileMakeupProperties.Keys.Any(x => x.Category == categoryNo);

                        //extInfo整理
                        int cleanExtData(ref PluginData tmpExtData, bool keepBodyData) {
                            tmpExtData = ExtendedSave.GetExtendedDataById(ocichar.charInfo.chaFile, ext);
                            if (tmpExtData != null && tmpExtData.data.ContainsKey("info")) {
                                if (tmpExtData.data.TryGetValue("info", out object tmpExtInfo)) {
                                    if (null != tmpExtInfo as object[])
                                    {
                                        List<object> tmpExtList = new List<object>(tmpExtInfo as object[]);
                                        Logger.Log(LogLevel.Debug, $"[KK_SCOLB] Sideloader count: {tmpExtList.Count}");
                                        ResolveInfo tmpResolveInfo;
                                        for (int j = 0; j < tmpExtList.Count;)
                                        {
                                            tmpResolveInfo = ResolveInfo.Unserialize((byte[])tmpExtList[j]);
                                            if (keepBodyData == isBelongsToCharaBody(tmpResolveInfo.CategoryNo))
                                            {
                                                Logger.Log(LogLevel.Debug, $"[KK_SCOLB] Add Sideloader info: {tmpResolveInfo.GUID} : {tmpResolveInfo.Property} : {tmpResolveInfo.Slot}");
                                                j++;
                                            }
                                            else
                                            {
                                                Logger.Log(LogLevel.Debug, $"[KK_SCOLB] Remove Sideloader info: {tmpResolveInfo.GUID} : {tmpResolveInfo.Property} : {tmpResolveInfo.Slot}");
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
                        Logger.Log(LogLevel.Debug, $"[KK_SCOLB] Get Old Sideloader Start");
                        int L1 = cleanExtData(ref oldExtData, false);
                        Logger.Log(LogLevel.Debug, $"[KK_SCOLB] Get Old Sideloader: {L1}");

                        //將擴充資料由暫存複製到角色身上
                        ExtendedSave.SetExtendedDataById(ocichar.charInfo.chaFile, ext, ExtendedSave.GetExtendedDataById(tmpChaFile, ext));

                        //清理新角色數據
                        PluginData newExtData = ExtendedSave.GetExtendedDataById(ocichar.charInfo.chaFile, ext);
                        Logger.Log(LogLevel.Debug, $"[KK_SCOLB] Get New Sideloader Start");
                        int L2 = cleanExtData(ref newExtData, true);
                        Logger.Log(LogLevel.Debug, $"[KK_SCOLB] Get New Sideloader: {L2}");

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
                        Logger.Log(LogLevel.Debug, $"[KK_SCOLB] Merge and Save Sideloader: {tmpObj.Length}");

                        //調用原始sideloader載入hook function
                        typeof(Sideloader.AutoResolver.Hooks).InvokeMember("ExtendedCardLoad", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.InvokeMethod, null, null, new[] { (ChaFile)ocichar.charInfo.chaFile });

                        Logger.Log(LogLevel.Debug, "[KK_SCOLB] Sideloader Data Loaded");
                        break;
                    default:
                        ExtendedSave.SetExtendedDataById(ocichar.charInfo.chaFile, ext, ExtendedSave.GetExtendedDataById(tmpChaFile, ext));
                        Logger.Log(LogLevel.Debug, "[KK_SCOLB] Change Extended Data: " + ext);
                        break;
                }
                var KCOXController = ocichar.charInfo.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KoiClothesOverlayX"));
                KCOXController?.Invoke("OnCardBeingSaved", new object[] { 1 });
            }
            return true;
        }

        //右側選單的名字更新
        public static bool UpdateTreeNodeObjectName(OCIChar oCIChar) {
            oCIChar.charInfo.name = oCIChar.charInfo.chaFile.parameter.fullname;
            oCIChar.charInfo.chaFile.SetProperty("charaFileName", oCIChar.charInfo.chaFile.parameter.fullname);
            oCIChar.treeNodeObject.textName = oCIChar.charInfo.chaFile.parameter.fullname;
            Logger.Log(LogLevel.Debug, "[KK_SCOLB] Set Name to: " + oCIChar.charInfo.chaFile.parameter.fullname);

            return true;
        }

        //Some plugins hook on this function, so call it to trigger them. (Example: KKAPI.Chara.OnReload)
        private static bool fakeChangeCharaFlag = false;
        [HarmonyPrefix, HarmonyPatch(typeof(OCIChar), "ChangeChara")]
        public static bool ChangeCharaPrefix(OCIChar __instance) {
            //HSPE will fail without renewing fbsCtrl. He use this as a dictionary key.
            if (fakeChangeCharaFlag) {
                var oldFbsCtrl = __instance.charInfo.fbsCtrl;
                var newFbsCtrl = new FaceBlendShape();
                if (null != oldFbsCtrl) {
                    newFbsCtrl.BlinkCtrl = oldFbsCtrl.BlinkCtrl;
                    newFbsCtrl.EyebrowCtrl = oldFbsCtrl.EyebrowCtrl;
                    newFbsCtrl.EyeLookController = oldFbsCtrl.EyeLookController;
                    newFbsCtrl.EyeLookDownCorrect = oldFbsCtrl.EyeLookDownCorrect;
                    newFbsCtrl.EyeLookSideCorrect = oldFbsCtrl.EyeLookSideCorrect;
                    newFbsCtrl.EyeLookUpCorrect = oldFbsCtrl.EyeLookUpCorrect;
                    newFbsCtrl.EyesCtrl = oldFbsCtrl.EyesCtrl;
                    newFbsCtrl.MouthCtrl = oldFbsCtrl.MouthCtrl;
                }
                ChaInfo chaInfo = __instance.charInfo;
                chaInfo.SetProperty("fbsCtrl", newFbsCtrl);
            }

            return !fakeChangeCharaFlag;
        }
    }
}
