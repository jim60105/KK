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
using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using Extension;
using Harmony;
using MessagePack;
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
        internal const string PLUGIN_VERSION = "19.06.06.1";

        public void Awake() => HarmonyInstance.Create(GUID).PatchAll(typeof(Patches));
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
                if (!LoadFile(chaCtrl, fullPath) || !FixSideloader(chaCtrl.chaFile) || !LoadExtendedData(ocichar, charaFileSort.selectPath, (byte)sex) || !UpdateTreeNodeObjectName(ocichar)) {
                    Logger.Log(LogLevel.Error, "[KK_SCOLB] Load Body FAILED");
                }
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

        //讀取檔案
        public static bool LoadFile(ChaControl chaCtrl, string fullPath) {
            if (!File.Exists(fullPath)) {
                return false;
            }
            using (FileStream fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read)) {
                using (BinaryReader binaryReader = new BinaryReader(fileStream)) {
                    long pngSize = PngFile.GetPngSize(binaryReader);
                    if (pngSize != 0L) {
                        binaryReader.BaseStream.Seek(pngSize, SeekOrigin.Current);

                        if (binaryReader.BaseStream.Length - binaryReader.BaseStream.Position == 0L) {
                            return false;
                        }
                    }
                    try {
                        var loadProductNo = binaryReader.ReadInt32();
                        if (loadProductNo > 100) {
                            return false;
                        }
                        string a = binaryReader.ReadString();
                        if (a != "【KoiKatuChara】") {
                            return false;
                        }
                        var loadVersion = new Version(binaryReader.ReadString());
                        if (0 > ChaFileDefine.ChaFileVersion.CompareTo(loadVersion)) {
                            return false;
                        }
                        int num = binaryReader.ReadInt32();
                        if (num != 0) {
                            var facePngData = binaryReader.ReadBytes(num);
                        }
                        int count = binaryReader.ReadInt32();
                        byte[] bytes = binaryReader.ReadBytes(count);
                        BlockHeader blockHeader = MessagePackSerializer.Deserialize<BlockHeader>(bytes);
                        long num2 = binaryReader.ReadInt64();
                        long position = binaryReader.BaseStream.Position;

                        BlockHeader.Info info = blockHeader.SearchInfo(ChaFileCustom.BlockName);
                        if (info != null) {
                            Version version = new Version(info.version);
                            if (0 <= ChaFileDefine.ChaFileCustomVersion.CompareTo(version)) {
                                binaryReader.BaseStream.Seek(position + info.pos, SeekOrigin.Begin);
                                byte[] data = binaryReader.ReadBytes((int)info.size);
                                chaCtrl.chaFile.SetCustomBytes(data, version);
                            }
                        }

                        info = blockHeader.SearchInfo(ChaFileParameter.BlockName);
                        if (info != null) {
                            Version value = new Version(info.version);
                            if (0 <= ChaFileDefine.ChaFileParameterVersion.CompareTo(value)) {
                                binaryReader.BaseStream.Seek(position + info.pos, SeekOrigin.Begin);
                                byte[] parameterBytes = binaryReader.ReadBytes((int)info.size);
                                chaCtrl.chaFile.SetParameterBytes(parameterBytes);
                            }
                        }

                        binaryReader.BaseStream.Seek(position + num2, SeekOrigin.Begin);
                    } catch (EndOfStreamException) {
                        return false;
                    }
                    Logger.Log(LogLevel.Debug, "[KK_SCOLB] Load Raw Body Finish");
                    return true;
                }
            }
        }

        //清除角色身上的sideloader資料
        public static bool FixSideloader(ChaFile file) {
            //Get these codes from Sideloader.AutoResolver.Hooks.ExtendedCardLoad
            var extData = ExtendedSave.GetExtendedDataById(file, UniversalAutoResolver.UARExtID);
            List<ResolveInfo> extInfo = null;

            if (extData != null && extData.data.ContainsKey("info") && extData.data["info"] is object[] tmpExtInfo) {
                extInfo = tmpExtInfo.Select(x => ResolveInfo.Unserialize(x as byte[])).ToList();

                Logger.Log(LogLevel.Debug, $"[KK_SCOLB] Sideloader marker found, external info count: {extInfo.Count}");
                Logger.Log(LogLevel.Debug, "[KK_SCOLB] Clean them");
                extInfo = null;

                ExtendedSave.SetExtendedDataById(file, UniversalAutoResolver.UARExtID, null);
            }
            Logger.Log(LogLevel.Debug, "[KK_SCOLB] Sideloader Extended Data is Clean");

            IterateCardPrefixes(UniversalAutoResolver.ResolveStructure);

            Logger.Log(LogLevel.Debug, "[KK_SCOLB] Fix Sideloader mods Finish");
            return true;

            void IterateCardPrefixes(Action<Dictionary<CategoryProperty, StructValue<int>>, object, ICollection<ResolveInfo>, string> action) {
                action(StructReference.ChaFileFaceProperties, file.custom.face, extInfo, "");
                action(StructReference.ChaFileBodyProperties, file.custom.body, extInfo, "");
                action(StructReference.ChaFileHairProperties, file.custom.hair, extInfo, "");
                action(StructReference.ChaFileMakeupProperties, file.custom.face.baseMakeup, extInfo, "");
            }
        }

        //載入擴充資料
        public static bool LoadExtendedData(OCIChar ocichar, string file, byte sex) {
            string[] copyList = { "com.deathweasel.bepinex.bodyshaders", "com.deathweasel.bepinex.uncensorselector", "KKABMPlugin.ABMData" };

            ChaFileControl tmpChaFile = new ChaFileControl();
            tmpChaFile.LoadCharaFile(file, sex);

            foreach (var ext in copyList) {
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
                    default:
                        ExtendedSave.SetExtendedDataById(ocichar.charInfo.chaFile, ext, ExtendedSave.GetExtendedDataById(tmpChaFile, ext));
                        Logger.Log(LogLevel.Debug, "[KK_SCOLB] Change Extended Data: " + ext);
                        break;
                }
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
