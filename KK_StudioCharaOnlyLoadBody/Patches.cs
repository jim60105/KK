using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

namespace KK_StudioCharaOnlyLoadBody
{
    class Patches
    {
        internal static void InitPatch(HarmonyInstance harmony)
        {
            harmony.Patch(typeof(CharaList).GetMethod("InitCharaList", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(InitCharaListPostfix), null), null);

            //Change the interactable of the button
            harmony.Patch(typeof(CharaList).GetMethod("OnDelete", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(SetKeepCoorButtonInteractable), null), null);
            harmony.Patch(typeof(CharaList).GetMethod("OnDeselect", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(SetKeepCoorButtonInteractable), null), null);
            harmony.Patch(typeof(CharaList).GetMethod("OnSelect", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(SetKeepCoorButtonInteractable), null), null);
            harmony.Patch(typeof(CharaList).GetMethod("OnSelectChara", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(SetKeepCoorButtonInteractable), null), null);
            harmony.Patch(typeof(CharaList).GetMethod("OnSort", AccessTools.all), null, new HarmonyMethod(typeof(Patches), nameof(SetKeepCoorButtonInteractable), null), null);

            harmony.Patch(typeof(OCIChar).GetMethod("ChangeChara", AccessTools.all), new HarmonyMethod(typeof(Patches), nameof(ChangeCharaPrefix), null), null, null);
        }

        //Draw the buttons, and add my trigger function
        private static GameObject[] btn = new GameObject[2];
        public static void InitCharaListPostfix(CharaList __instance)
        {
            var original = GameObject.Find("StudioScene/Canvas Main Menu/01_Add/" + __instance.name + "/Button Change");
            int i = (String.Equals(__instance.name, "00_Female") ? 1 : 0);
            btn[i] = UnityEngine.Object.Instantiate(original, original.transform.parent);
            btn[i].name = "Button Keep Coordinate Change";
            btn[i].transform.position += new Vector3(0, -25, 0);
            btn[i].transform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(180, -401), new Vector2(390, -380));
            btn[i].GetComponent<Image>().sprite = Extension.Extension.LoadNewSprite("KK_StudioCharaOnlyLoadBody.Resources.buttonChange.png", 183, 20);

            btn[i].GetComponent<Button>().onClick.RemoveAllListeners();
            btn[i].GetComponent<Button>().onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
            btn[i].GetComponent<Button>().onClick.AddListener(() =>
            {
                var charaFileSort = __instance.GetPrivate("charaFileSort") as CharaFileSort;
                var chaFileControl = new ChaFileControl();
                var fullPath = chaFileControl.ConvertCharaFilePath(charaFileSort.selectPath, (byte)i, false);
                chaFileControl = null;
                OCIChar[] array = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                                   select Studio.Studio.GetCtrlInfo(v) as OCIChar into v
                                   where v != null
                                   select v).ToArray();
                foreach (var ocichar in array)
                {
                    ChaControl chaCtrl = ocichar.charInfo;
                    foreach (OCIChar.BoneInfo boneInfo in (from v in ocichar.listBones
                                                           where v.boneGroup == OIBoneInfo.BoneGroup.Hair
                                                           select v).ToList<OCIChar.BoneInfo>())
                    {
                        Singleton<GuideObjectManager>.Instance.Delete(boneInfo.guideObject, true);
                    }
                    ocichar.listBones = (from v in ocichar.listBones
                                         where v.boneGroup != OIBoneInfo.BoneGroup.Hair
                                         select v).ToList<OCIChar.BoneInfo>();
                    int[] array2 = (from b in ocichar.oiCharInfo.bones
                                    where b.Value.@group == OIBoneInfo.BoneGroup.Hair
                                    select b.Key).ToArray<int>();
                    for (int j = 0; j < array2.Length; j++)
                    {
                        ocichar.oiCharInfo.bones.Remove(array2[j]);
                    }
                    ocichar.hairDynamic = null;

                    string oldName = ocichar.charInfo.chaFile.parameter.fullname;

                    if (!LoadFile(chaCtrl, fullPath) || !FixSideloader(chaCtrl.chaFile) || !LoadExtendedData(ocichar, charaFileSort.selectPath, (byte)i) || !UpdateTreeNodeObjectName(ocichar))
                    {
                        Logger.Log(LogLevel.Error, "[KK_SCOLB] Load Body FAILED");
                    }
                    chaCtrl.Reload(false, false, false, false);

                    AddObjectAssist.InitHairBone(ocichar, Singleton<Info>.Instance.dicBoneInfo);
                    ocichar.hairDynamic = AddObjectFemale.GetHairDynamic(ocichar.charInfo.objHair);
                    ocichar.InitFK(null);
                    foreach (var __AnonType in FKCtrl.parts.Select((OIBoneInfo.BoneGroup p, int i2) => new
                    {
                        p,
                        i2
                    }))
                    {
                        ocichar.ActiveFK(__AnonType.p, ocichar.oiCharInfo.activeFK[__AnonType.i2], ocichar.oiCharInfo.activeFK[__AnonType.i2]);
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

                    Logger.Log(LogLevel.Info, $"[KK_SCOLB] Load Body Finish: {oldName} -> {ocichar.charInfo.chaFile.parameter.fullname}");
                }
            });
            SetKeepCoorButtonInteractable(__instance);
        }

        //Sync my button state with the original change button
        private static void SetKeepCoorButtonInteractable(CharaList __instance)
        {
            int i = (String.Equals(__instance.name, "00_Female") ? 1 : 0);
            btn[i].GetComponent<Button>().interactable = ((Button)__instance.GetPrivate("buttonChange")).interactable;
        }

        //Main load body function
        public static bool LoadFile(ChaControl chaCtrl, string fullPath)
        {
            if (!File.Exists(fullPath))
            {
                return false;
            }
            using (FileStream fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    long pngSize = PngFile.GetPngSize(binaryReader);
                    if (pngSize != 0L)
                    {
                        binaryReader.BaseStream.Seek(pngSize, SeekOrigin.Current);

                        if (binaryReader.BaseStream.Length - binaryReader.BaseStream.Position == 0L)
                        {
                            return false;
                        }
                    }
                    try
                    {
                        var loadProductNo = binaryReader.ReadInt32();
                        if (loadProductNo > 100)
                        {
                            return false;
                        }
                        string a = binaryReader.ReadString();
                        if (a != "【KoiKatuChara】")
                        {
                            return false;
                        }
                        var loadVersion = new Version(binaryReader.ReadString());
                        if (0 > ChaFileDefine.ChaFileVersion.CompareTo(loadVersion))
                        {
                            return false;
                        }
                        int num = binaryReader.ReadInt32();
                        if (num != 0)
                        {
                            var facePngData = binaryReader.ReadBytes(num);
                        }
                        int count = binaryReader.ReadInt32();
                        byte[] bytes = binaryReader.ReadBytes(count);
                        BlockHeader blockHeader = MessagePackSerializer.Deserialize<BlockHeader>(bytes);
                        long num2 = binaryReader.ReadInt64();
                        long position = binaryReader.BaseStream.Position;

                        BlockHeader.Info info = blockHeader.SearchInfo(ChaFileCustom.BlockName);
                        if (info != null)
                        {
                            Version version = new Version(info.version);
                            if (0 <= ChaFileDefine.ChaFileCustomVersion.CompareTo(version))
                            {
                                binaryReader.BaseStream.Seek(position + info.pos, SeekOrigin.Begin);
                                byte[] data = binaryReader.ReadBytes((int)info.size);
                                chaCtrl.chaFile.SetCustomBytes(data, version);
                            }
                        }

                        info = blockHeader.SearchInfo(ChaFileParameter.BlockName);
                        if (info != null)
                        {
                            Version value = new Version(info.version);
                            if (0 <= ChaFileDefine.ChaFileParameterVersion.CompareTo(value))
                            {
                                binaryReader.BaseStream.Seek(position + info.pos, SeekOrigin.Begin);
                                byte[] parameterBytes = binaryReader.ReadBytes((int)info.size);
                                chaCtrl.chaFile.SetParameterBytes(parameterBytes);
                            }
                        }

                        binaryReader.BaseStream.Seek(position + num2, SeekOrigin.Begin);
                    }
                    catch (EndOfStreamException)
                    {
                        return false;
                    }
                    Logger.Log(LogLevel.Debug, "[KK_SCOLB] Load Raw Body Finish");
                    return true;
                }
            }
        }

        //Clear sideloader extended data which stored on the ChaFile
        public static bool FixSideloader(ChaFile file)
        {
            //Get these codes from Sideloader.AutoResolver.Hooks.ExtendedCardLoad
            var extData = ExtendedSave.GetExtendedDataById(file, UniversalAutoResolver.UARExtID);
            List<ResolveInfo> extInfo = null;

            if (extData != null && extData.data.ContainsKey("info"))
            {
                var tmpExtInfo = (object[])extData.data["info"];
                extInfo = tmpExtInfo.Select(x => ResolveInfo.Unserialize((byte[])x)).ToList();

                Logger.Log(LogLevel.Debug, $"[KK_SCOLB] Sideloader marker found, external info count: {extInfo.Count}");
                Logger.Log(LogLevel.Debug, "[KK_SCOLB] Clean them");
                extInfo = null;

                ExtendedSave.SetExtendedDataById(file, UniversalAutoResolver.UARExtID, null);
            }
            Logger.Log(LogLevel.Debug, "[KK_SCOLB] Sideloader Extended Data is Clean");

            IterateCardPrefixes(UniversalAutoResolver.ResolveStructure);

            Logger.Log(LogLevel.Debug, "[KK_SCOLB] Fix Sideloader mods Finish");
            return true;

            void IterateCardPrefixes(Action<Dictionary<CategoryProperty, StructValue<int>>, object, ICollection<ResolveInfo>, string> action)
            {
                action(StructReference.ChaFileFaceProperties, file.custom.face, extInfo, "");
                action(StructReference.ChaFileBodyProperties, file.custom.body, extInfo, "");
                action(StructReference.ChaFileHairProperties, file.custom.hair, extInfo, "");
                action(StructReference.ChaFileMakeupProperties, file.custom.face.baseMakeup, extInfo, "");
            }
        }

        //Load ExtendedData from the new character card
        public static bool LoadExtendedData(OCIChar ocichar, string file, byte sex)
        {
            string[] copyList = { "com.deathweasel.bepinex.bodyshaders", "com.deathweasel.bepinex.uncensorselector" };

            ChaFileControl tmpChaFile = new ChaFileControl();
            tmpChaFile.LoadCharaFile(file, sex, true, true);

            foreach (var ext in copyList)
            {
                ExtendedSave.SetExtendedDataById(ocichar.charInfo.chaFile, ext, ExtendedSave.GetExtendedDataById(tmpChaFile, ext));
                Logger.Log(LogLevel.Debug, "[KK_SCOLB] Change Extended Data: " + ext);
            }

            return true;

            //ReadUncensorData(ocichar.charInfo.chaFile);
            ////Only used for uncensor selector debug logging
            //void ReadUncensorData(ChaFile chaFile)
            //{
            //    string BodyGUID = null;
            //    string PenisGUID = null;
            //    string BallsGUID = null;
            //    var data = ExtendedSave.GetExtendedDataById(chaFile, "com.deathweasel.bepinex.uncensorselector");
            //    if (data != null)
            //    {
            //        if (data.data.TryGetValue("BodyGUID", out var loadedUncensorGUID) && loadedUncensorGUID != null)
            //        {
            //            BodyGUID = loadedUncensorGUID.ToString();
            //            Logger.Log(LogLevel.Debug, "[KK_SCOLB] BodyGUID: " + BodyGUID);
            //        }
            //        if (data.data.TryGetValue("PenisGUID", out var loadedPenisGUID) && loadedPenisGUID != null)
            //        {
            //            PenisGUID = loadedPenisGUID.ToString();
            //            Logger.Log(LogLevel.Debug, "[KK_SCOLB] PenisGUID: " + PenisGUID);
            //        }
            //        if (data.data.TryGetValue("BallsGUID", out var loadedBallsGUID) && loadedBallsGUID != null)
            //        {
            //            BallsGUID = loadedBallsGUID.ToString();
            //            Logger.Log(LogLevel.Debug, "[KK_SCOLB] BallsGUID: " + BallsGUID);
            //        }
            //    }
            //}
        }

        //Update the name on the right panel
        public static bool UpdateTreeNodeObjectName(OCIChar oCIChar)
        {
            oCIChar.charInfo.name = oCIChar.charInfo.chaFile.parameter.fullname;
            oCIChar.treeNodeObject.textName = oCIChar.charInfo.chaFile.parameter.fullname;
            Logger.Log(LogLevel.Debug, "[KK_SCOLB] Set Name to: " + oCIChar.charInfo.chaFile.parameter.fullname);

            return true;
        }

        //Some plugins hook on this function, so call it to trigger them. (Example: KKAPI.Chara.OnReload)
        private static bool fakeChangeCharaFlag = false;
        private static bool ChangeCharaPrefix(OCIChar __instance)
        {
            //HSPE will fail without renewing fbsCtrl. He use this as a dictionary key.
            if (fakeChangeCharaFlag)
            {
                var oldFbsCtrl = __instance.charInfo.fbsCtrl;
                var newFbsCtrl = new FaceBlendShape();
                if (null != oldFbsCtrl)
                {
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
                chaInfo.SetPrivateProperty("fbsCtrl", newFbsCtrl);
            }

            return !fakeChangeCharaFlag;
        }
    }
}
