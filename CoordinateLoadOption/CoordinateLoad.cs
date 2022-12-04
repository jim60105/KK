using BepInEx.Logging;
using ChaCustom;
using CoordinateLoadOption.OtherPlugin;
using CoordinateLoadOption.OtherPlugin.CharaCustomFunctionController;
using ExtensibleSaveFormat;
using Extension;
using MessagePack;
using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CoordinateLoadOption
{
    using CLO = CoordinateLoadOption;
    partial class CoordinateLoad
    {
        private static readonly ManualLogSource Logger = CLO.Logger;
        internal static int totalAmount = 0;
        internal static Queue<OCIChar> oCICharQueue = new Queue<OCIChar>();
        internal static int finishedCount = 0;
        internal static ChaControl tmpChaCtrl;
        private static ChaFileCoordinate backupTmpCoordinate;
        private static int forceCleanCount = CLO.FORCECLEANCOUNT;
        private static HairAccessoryCustomizer hairacc;
        internal static bool addAccModeFlag = true;

        internal static void Update()
        {
            if (null != tmpChaCtrl)
            {
                forceCleanCount--;
                if (forceCleanCount <= 0)
                {
                    End(forceClean: true);
                }
            }
        }

        internal static void MakeTmpChara(Action<object> callback)
        {
            forceCleanCount = CLO.FORCECLEANCOUNT;

            ChaControl chaCtrl = null;
            OCIChar ocichar = null;
            if (CLO.insideStudio)
            {
                if (oCICharQueue.Count != 0)
                {
                    ocichar = oCICharQueue.Dequeue();

                    //Bone
                    foreach (OCIChar.BoneInfo boneInfo in ocichar.listBones.Where(b => b.boneGroup == OIBoneInfo.BoneGroup.Hair))
                    {
                        Singleton<GuideObjectManager>.Instance.Delete(boneInfo.guideObject, true);
                    }
                    ocichar.listBones = ocichar.listBones.Where(b => b.boneGroup != OIBoneInfo.BoneGroup.Hair).ToList<OCIChar.BoneInfo>();
                    ocichar.hairDynamic = null;
                    ocichar.skirtDynamic = null;
                    chaCtrl = ocichar.charInfo;
                }
                // 如果是ReverseHairAcc功能，chaCtrl就會保持null
                // 後續用到chaCtrl都應該檢核
            }
            else
            {
                chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
            }

            hairacc = new HairAccessoryCustomizer(chaCtrl);
            if (CLO._isHairAccessoryCustomizerExist && null != chaCtrl)
            {
                hairacc.GetControllerAndBackupData(targetChaCtrl: chaCtrl);
                HairAccessoryCustomizer.UpdateBlock = true;
            }

            backupTmpCoordinate = new ChaFileCoordinate();
            backupTmpCoordinate.LoadFile(Patches.coordinatePath);

            //丟到Camera外面就看不見了
            tmpChaCtrl = Manager.Character.CreateFemale(Camera.main.gameObject, -1);
            tmpChaCtrl.gameObject.transform.localPosition = new Vector3(-100, -100, -100);
            tmpChaCtrl.Load(true);
            tmpChaCtrl.fileParam.lastname = "黑肉";
            tmpChaCtrl.fileParam.firstname = "舔舔";
            tmpChaCtrl.fileStatus.coordinateType = 0;
            if (hairacc.isExist && null != chaCtrl)
            {
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

            IEnumerator LoadTmpChara()
            {
                //KCOX在讀衣裝前需要先做Reload初始化
                tmpChaCtrl.Reload();
                yield return new WaitUntil(delegate { return CheckPluginPrepared(); });

                tmpChaCtrl.nowCoordinate.LoadFile(Patches.coordinatePath);
                tmpChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)tmpChaCtrl.fileStatus.coordinateType); //0
                tmpChaCtrl.Reload(false, true, true, true);

                forceCleanCount = CLO.FORCECLEANCOUNT;

                yield return new WaitUntil(delegate { return CheckPluginPrepared(backupTmpCoordinate); });

                callback?.Invoke((object)ocichar ?? chaCtrl);
            }

            bool CheckPluginPrepared(ChaFileCoordinate backCoordinate = null) =>
                null != tmpChaCtrl &&
                new KCOX(tmpChaCtrl).CheckControllerPrepared() &&
                (null == backCoordinate || new HairAccessoryCustomizer(tmpChaCtrl).CheckControllerPrepared(backCoordinate)) &&
                new MaterialEditor(tmpChaCtrl).CheckControllerPrepared();
        }

        internal static void ChangeCoordinate(object OcicharOrChaCtrl)
        {
            tmpChaCtrl.StopAllCoroutines();

            ChaControl chaCtrl;
            OCIChar ocichar = null;
            try
            {
                if (CLO.insideStudio)
                {
                    ocichar = OcicharOrChaCtrl as OCIChar;
                    chaCtrl = ocichar.charInfo;
                }
                else
                {
                    chaCtrl = OcicharOrChaCtrl as ChaControl;
                }
            }
            catch (Exception) { return; };
            chaCtrl.StopAllCoroutines();

            // 檢查自己身上是否有要綁定飾品的插件資料
            foreach (var guid in CLO.pluginBoundAccessories)
            {
                if (!Patches.boundAcc && null != ExtendedSave.GetExtendedDataById(chaCtrl.nowCoordinate, guid))
                {
                    Patches.boundAcc = true;

                    if (Patches.tgls[9].isOn && Patches.tgls2.ToList().Any(tg => !tg.isOn))
                    {
                        foreach (var tg in Patches.tgls2)
                        {
                            tg.isOn = true;
                            tg.interactable = false;
                        }

                        Logger.Log(LogLevel.Message | LogLevel.Warning, $"The accessories option is disabled due to the plugin data ({guid}) found on your character {chaCtrl.fileParam.fullname}");
                        Logger.Log(LogLevel.Message | LogLevel.Warning, $"If you surely want to apply accessories, please check the option panel and press the Load button again.");
                        End();
                        return;
                    }
                    break;
                }
            }

            KCOX kcox = new KCOX(chaCtrl);
            ABMX abmx = new ABMX(chaCtrl);
            MaterialEditor me = new MaterialEditor(chaCtrl);

            #region Main Load Coordinate
            Helper.PrintClothStatus(chaCtrl.nowCoordinate.clothes, "Before");
            Helper.PrintAccStatus(chaCtrl.nowCoordinate.accessory.parts, "Before");
            foreach (Toggle tgl in Patches.tgls)
            {
                object tmpToggleType = null;
                int kind = -2;
                try
                {
                    tmpToggleType = Enum.Parse(typeof(CLO.ClothesKind), tgl.name);
                    kind = Convert.ToInt32(tmpToggleType);
                }
                catch (ArgumentException)
                {
                    kind = -1;
                }
                if (tgl.isOn)
                {
                    if (kind == 9)
                    {
                        //Copy accessories
                        ChaFileAccessory.PartsInfo[] chaCtrlAccParts = chaCtrl.nowCoordinate.accessory.parts;
                        ChaFileAccessory.PartsInfo[] tmpCtrlAccParts = tmpChaCtrl.nowCoordinate.accessory.parts;

                        if (Patches.boundAcc)
                        {
                            ClearAccessories(chaCtrl);
                        }

                        ChangeAccessories(tmpChaCtrl, tmpCtrlAccParts, chaCtrl, ref chaCtrlAccParts);
                        chaCtrl.nowCoordinate.accessory.parts = chaCtrlAccParts;
                        if (CLO._isMoreAccessoriesExist)
                        {
                            MoreAccessories.ArraySync(chaCtrl);
                            MoreAccessories.Update();
                        }
                        Logger.LogDebug("->Changed: " + tgl.name);
                        Logger.LogDebug($"Acc Count : {chaCtrl.nowCoordinate.accessory.parts.Length}");
                    }
                    else if (kind >= 0)
                    {
                        //Change clothes
                        byte[] tmp = MessagePackSerializer.Serialize<ChaFileClothes.PartsInfo>(tmpChaCtrl.nowCoordinate.clothes.parts[kind]);
                        chaCtrl.nowCoordinate.clothes.parts[kind] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(tmp);
                        chaCtrl.ChangeClothesNoAsync(kind: kind,
                                                     id: tmpChaCtrl.nowCoordinate.clothes.parts[kind].id,
                                                     subId01: tmpChaCtrl.nowCoordinate.clothes.subPartsId[0],
                                                     subId02: tmpChaCtrl.nowCoordinate.clothes.subPartsId[1],
                                                     subId03: tmpChaCtrl.nowCoordinate.clothes.subPartsId[2],
                                                     forceChange: true,
                                                     update: true);

                        if (kcox.isExist)
                            kcox.CopyKCOXData(tmpChaCtrl, kind);

                        if (me.isExist)
                            me.CopyMaterialEditorData(tmpChaCtrl, kind, kind, chaCtrl.objClothes[kind], MaterialEditor.ObjectType.Clothing);

                        Logger.LogDebug("->Changed: " + tgl.name + " / ID: " + chaCtrl.nowCoordinate.clothes.parts[kind].id);
                    }
                }
            }
            #endregion

            #region Save and Reload
            //HairAcc
            if (hairacc.isExist)
            {
                //寫入 (即使未載入Acc，也需要將一開始的備份寫回)
                hairacc.SetToExtData();
                hairacc.SetDataToCoordinate();

                HairAccessoryCustomizer.UpdateBlock = false;

                //Load to controller (Maker只有從Coordinate存才能運作)
                hairacc.SetControllerFromExtData();
                hairacc.SetControllerFromCoordinate();
            }

            //ABMX
            if (abmx.isExist)
            {
                if (Patches.readABMX) abmx.CopyABMXData(tmpChaCtrl);
            }

            // 處理要綁定飾品的插件資料
            if (Patches.boundAcc && Patches.tgls[9].isOn)
            {
                foreach (var guid in CLO.pluginBoundAccessories)
                {
                    ExtendedSave.SetExtendedDataById(chaCtrl.nowCoordinate, guid, ExtendedSave.GetExtendedDataById(backupTmpCoordinate, guid));
                }
            }

            string tempPath = Path.GetTempFileName();
            tempPath = Path.ChangeExtension(tempPath, ".tmp");  // For safety
            chaCtrl.nowCoordinate.SaveFile(tempPath);

            chaCtrl.nowCoordinate.LoadFile(tempPath);

            chaCtrl.Reload(false, true, true, true);
            chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)chaCtrl.chaFile.status.coordinateType);

            if (!CLO.insideStudio)
                Singleton<CustomBase>.Instance.updateCustomUI = true;
#if !DEBUG
            File.Delete(tempPath);
#endif
            Helper.PrintClothStatus(chaCtrl.nowCoordinate.clothes, "After");
            Helper.PrintAccStatus(chaCtrl.nowCoordinate.accessory.parts, "After");
            #endregion

            finishedCount++;

            if (CLO.insideStudio && null != ocichar)
            {
                //Bone & FK,IK
                chaCtrl.UpdateBustSoftnessAndGravity();
                AddObjectAssist.InitHairBone(ocichar, Singleton<Info>.Instance.dicBoneInfo);
                ocichar.hairDynamic = AddObjectFemale.GetHairDynamic(ocichar.charInfo.objHair);
                ocichar.skirtDynamic = AddObjectFemale.GetSkirtDynamic(ocichar.charInfo.objClothes);
                ocichar.InitFK(null);
                foreach (var tmp in FKCtrl.parts.Select((OIBoneInfo.BoneGroup p, int i2) => new { p, i2 }))
                {
                    ocichar.ActiveFK(tmp.p, ocichar.oiCharInfo.activeFK[tmp.i2], ocichar.oiCharInfo.activeFK[tmp.i2]);
                }
                ocichar.ActiveKinematicMode(OICharInfo.KinematicMode.FK, ocichar.oiCharInfo.enableFK, true);
                ocichar.UpdateFKColor(new OIBoneInfo.BoneGroup[] { OIBoneInfo.BoneGroup.Hair });
                //State
                ocichar.ChangeEyesOpen(ocichar.charFileStatus.eyesOpenMax);
                ocichar.ChangeBlink(ocichar.charFileStatus.eyesBlink);
                ocichar.ChangeMouthOpen(ocichar.oiCharInfo.mouthOpen);

                Logger.LogInfo($"Loaded: {finishedCount}/{totalAmount}");
            }
            else
            {
                Singleton<CustomBase>.Instance.updateCustomUI = true;
            }

            forceCleanCount = CLO.FORCECLEANCOUNT;
            End();
        }

        private static void End(bool forceClean = false)
        {
            hairacc.ClearBackup();
            hairacc = null;

            tmpChaCtrl.StopAllCoroutines();
            backupTmpCoordinate = null;
            Manager.Character.DeleteChara(tmpChaCtrl);
            tmpChaCtrl = null;
            Logger.LogDebug($"Delete Temp Chara");

            if (oCICharQueue.Count > 0 && !forceClean && CLO.insideStudio)
            {
                MakeTmpChara(ChangeCoordinate);
            }
            else
            {
                oCICharQueue.Clear();
                totalAmount = 0;
                Logger.LogInfo($"Load End");

                if (forceClean)
                {
                    Logger.Log(LogLevel.Message | LogLevel.Error, "Coordinate Load ended unexpectedly.");
                    Logger.Log(LogLevel.Message | LogLevel.Error, "Please call the original game function manually instead. Close the \"Show Selection\" panel to do so.");
                    Logger.LogMessage("Also, please check output_log for more information.");

                    if (!CLO.insideStudio)
                    {
                        string msg = StringResources.StringResourcesManager.GetString("makerWarning");
                        Logger.LogWarning(msg);
                    }

                    Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.cancel);
                }
                else
                {
                    Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_s);
                }
            }
        }
    }
}
