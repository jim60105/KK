using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExtensibleSaveFormat;
using Extension;
using HarmonyLib;
using Illusion.Extensions;
using MessagePack;
using UnityEngine;

namespace CoordinateLoadOption {
    internal class HairAccessoryCustomizer : CharaCustomFunctionController_Support {
        public override string GUID => "com.deathweasel.bepinex.hairaccessorycustomizer";
        public override string ControllerName => "HairAccessoryController";
        public override string CCFCName => "HairAccessoryCustomizer";

        internal new Dictionary<int, object> SourceBackup { get => base.SourceBackup?.ToDictionary<int, object>(); set => base.SourceBackup = value; }
        internal new Dictionary<int, object> TargetBackup { get => base.TargetBackup?.ToDictionary<int, object>(); set => base.TargetBackup = value; }

        public HairAccessoryCustomizer(ChaControl chaCtrl) : base(chaCtrl) => isExist = CoordinateLoadOption._isHairAccessoryCustomizerExist;

        internal static Type HairAccessoryControllerType;

        public override bool LoadAssembly() {
            bool loadSuccess = LoadAssembly(out string path, new Version(1, 1, 2));
            if (loadSuccess && !path.IsNullOrEmpty()) {
                HairAccessoryControllerType = Assembly.LoadFrom(path).GetType("KK_Plugins.HairAccessoryCustomizer")?.GetNestedType("HairAccessoryController");
            }
            return loadSuccess;
        }

        #region Patches
        internal static bool UpdateBlock = false;
        public static bool UpdateAccessoriesPrefix() =>
            //Logger.LogWarning("Trigger Update Block " + UpdateBlock);
            !UpdateBlock;

        public static void Patch(Harmony harmonyInstance) {
            harmonyInstance.Patch(HairAccessoryControllerType.GetMethod("UpdateAccessory", AccessTools.all),
                prefix: new HarmonyMethod(typeof(HairAccessoryCustomizer), nameof(HairAccessoryCustomizer.UpdateAccessoriesPrefix)));
            harmonyInstance.Patch(HairAccessoryControllerType.GetMethod("UpdateAccessories", AccessTools.all),
                prefix: new HarmonyMethod(typeof(HairAccessoryCustomizer), nameof(HairAccessoryCustomizer.UpdateAccessoriesPrefix)));
        }
        #endregion

        /// <summary>
        /// 從ExtendedData取得給定ChaControl的HairAccessories和單一hairAccessoryInfo
        /// </summary>
        /// <param name="chaCtrl">要查詢的ChaControl</param>
        /// <param name="nowcoordinateExtData">nowCoordinate的hairAccessoryInfo</param>
        /// <returns>整個HairAccessories</returns>
        public Dictionary<int, Dictionary<int, object>> GetDataFromExtData(ChaControl chaCtrl) {
            Dictionary<int, Dictionary<int, object>> result = null;
            PluginData data = ExtendedSave.GetExtendedDataById(chaCtrl.chaFile, GUID);
            if (data != null && data.data.TryGetValue("HairAccessories", out object loadedHairAccessories) && loadedHairAccessories is byte[] loadedBA) {
                result = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<int, object>>>(loadedBA);
            }
            if (null != result) {
                Logger.LogDebug($"Get {chaCtrl.fileParam.fullname} Hair Accessories From ExtData");
            } else {
                Logger.LogDebug($"No Hair Accessories get from {chaCtrl.fileParam.fullname}'s ExtData");
            }
            return result;
        }

        public override object GetDataFromController(ChaControl chaCtrl) {
            MonoBehaviour controller = GetController(chaCtrl);
            object HairAccessories = controller?.GetField("HairAccessories");

            if (null != HairAccessories && HairAccessories.Count() != 0) {
                Logger.LogDebug($"Get {chaCtrl.fileParam.fullname} Hair Accessories From Controller");
            }
            return HairAccessories;
        }

        //Not tested
        private object CopyHairAccData_ControllerData(object HairAccessories) {
            Dictionary<int, Dictionary<int, object>> result = null;
            if (null != HairAccessories && HairAccessories.Count() != 0) {
                result = new Dictionary<int, Dictionary<int, object>>();
                foreach (KeyValuePair<int, object> kv in HairAccessories.ToDictionary<int, object>()) {
                    if (null != kv.Value) {
                        result[kv.Key] = new Dictionary<int, object>();
                        foreach (KeyValuePair<int, object> kv2 in kv.Value.ToDictionary<int, object>()) {
                            result[kv.Key][kv2.Key] = new Dictionary<object, object> {
                                { "HairGloss",kv2.Value.GetField("HairGloss") },
                                { "ColorMatch",kv2.Value.GetField("ColorMatch") },
                                { "OutlineColor", kv2.Value.GetField("OutlineColor")},
                                { "AccessoryColor", kv2.Value.GetField("AccessoryColor")},
                                { "HairLength", kv2.Value.GetField("HairLength") }
                            };
                        }
                    }
                }
                Logger.LogDebug($"Get Hair Accessories From Controller");
            } else {
                Logger.LogDebug($"No Hair Accessories get from Controller");
            }
            return result;
        }

        /// <summary>
        /// 從HairAccessoriesData取得單一服裝的HairAccessories
        /// </summary>
        /// <param name="HairAccessories">來源的HairAccessoriesData</param>
        /// <param name="coordinateType">要取得的CoordinateType</param>
        /// <param name="coordinateData">out 單一服裝的HairAccessories</param>
        /// <returns>取得成功與否</returns>
        public bool GetCoordinateData(object HairAccessories, int coordinateType, out Dictionary<int, object> coordinateData) {
            coordinateData = null;
            if (HairAccessories is IDictionary _h
                && _h.Count != 0
                && _h.TryGetValue(coordinateType, out object _c)
                && _c is IDictionary) {
                coordinateData = _c.ToDictionary<int, object>();
                Logger.LogDebug($"->{string.Join(",", coordinateData.Select(x => x.Key.ToString()).ToArray())}");
                return true;
            } else {
                Logger.LogDebug($"Cannot get HairAccInfo on coordinate");
                return false;
            }
        }

        /// <summary>
        /// 將HairAccCusController.HairAccessories[coordinateType]中，所有HairAccData之ColorMatch都取消。只在Maker中有效。
        /// </summary>
        public void DisableColorMatches() => DisableColorMatches(DefaultChaCtrl);
        public void DisableColorMatches(ChaControl chaCtrl) {
            if (GetCoordinateData(
                    GetDataFromController(chaCtrl),
                    chaCtrl.fileStatus.coordinateType,
                    out Dictionary<int, object> hairAcc)
                && null != hairAcc) {
                foreach (KeyValuePair<int, object> kvp in hairAcc) {
                    GetController(chaCtrl).Invoke("SetColorMatch", new object[] { false, kvp.Key });
                }
            }
        }

        /// <summary>
        /// 從ExtendedData取得給定ChaControl的HairAccessories和單一hairAccessoryInfo
        /// </summary>
        /// <param name="chaCtrl">要查詢的ChaControl</param>
        /// <param name="nowcoordinateExtData">nowCoordinate的hairAccessoryInfo</param>
        /// <returns>整個HairAccessories</returns>
        public Dictionary<int, object> GetDataFromCoordinate(ChaFileCoordinate coordinate) {
            Dictionary<int, object> coordinateData;
            PluginData data = ExtendedSave.GetExtendedDataById(coordinate, GUID);
            if (data != null && data.data.TryGetValue("CoordinateHairAccessories", out object loadedHairAccessories) && loadedHairAccessories != null) {
                coordinateData = MessagePackSerializer.Deserialize<Dictionary<int, object>>((byte[])loadedHairAccessories);
                if (null == coordinateData) {
                    coordinateData = new Dictionary<int, object>();
                }
                Logger.LogDebug($"Get Hair Accessories from coordinate {coordinate.coordinateName}: {coordinateData.Count} (1)");
                Logger.LogDebug($"->{string.Join(",", coordinateData.Select(x => x.Key.ToString()).ToArray())}");
                return coordinateData;
            }
            Logger.LogDebug($"No Hair Accessories get from coordinate {coordinate.coordinateFileName}");
            return null;
        }

        /// <summary>
        /// 將dict存入Coordinate ExtendedData
        /// </summary>
        /// <param name="dict">要存入的dict</param>
        public void SetDataToCoordinate() => SetDataToCoordinate(TargetBackup);
        public void SetDataToCoordinate(Dictionary<int, object> dict) => SetDataToCoordinate(DefaultChaCtrl.nowCoordinate, dict);
        public void SetDataToCoordinate(ChaFileCoordinate coordinate, Dictionary<int, object> dict) {
            PluginData data = new PluginData();
            if ((null == dict || dict.Count == 0)) {
                data = null;
            } else {
                data.data.Add("CoordinateHairAccessories", MessagePackSerializer.Serialize(dict));
            }

            ExtendedSave.SetExtendedDataById(coordinate, GUID, data);
        }

        /// <summary>
        /// 由Coordinate載入HairAcc至Controller內，注意這條是異步執行
        /// </summary>
        /// <param name="chaCtrl">要被設定的ChaControl</param>
        /// <param name="coordinate">要載入的coordibate</param>
        /// <returns></returns>
        public new void SetControllerFromCoordinate(ChaControl chaCtrl, ChaFileCoordinate coordinate = null)
            => base.SetControllerFromCoordinate(chaCtrl, coordinate);

        /// <summary>
        /// 由ExtData載入HairAcc至Controller內，注意這條是異步執行
        /// </summary>
        /// <param name="chaCtrl">要被設定的ChaControl</param>
        /// <returns></returns>
        public new void SetControllerFromExtData(ChaControl chaCtrl)
            => base.SetControllerFromExtData(chaCtrl);

        /// <summary>
        /// 將dict存入ChaControl ExtendedData
        /// </summary>
        public void SetToExtData() => SetToExtData(TargetBackup);
        public void SetToExtData(Dictionary<int, object> dict) => SetToExtData(DefaultChaCtrl, dict);
        public void SetToExtData(ChaControl chaCtrl, Dictionary<int, object> dict) {
            PluginData data = new PluginData();
            Dictionary<int, Dictionary<int, object>> allExtData = GetDataFromExtData(chaCtrl);
            int coorType = chaCtrl.fileStatus.coordinateType;
            if ((null == dict || dict.Count == 0) && (null == allExtData || allExtData.Count == 0)) {
                data = null;
            } else {
                if (null == allExtData) {
                    allExtData = new Dictionary<int, Dictionary<int, object>>();
                    Logger.LogDebug($"HairAccCustomizer info not found while saving.");
                }
                allExtData[coorType] = dict;
                data.data.Add("HairAccessories", MessagePackSerializer.Serialize(allExtData));
            }

            ExtendedSave.SetExtendedDataById(chaCtrl.chaFile, GUID, data);
        }

        public void SetToExtData(ChaControl chaCtrl, Dictionary<int, Dictionary<int, object>> allExtData) {
            if (null != chaCtrl)
            {
                PluginData data = null;
                if (null != allExtData)
                {
                    data = new PluginData();
                    data.data.Add("HairAccessories", MessagePackSerializer.Serialize(allExtData));
                }
                ExtendedSave.SetExtendedDataById(chaCtrl.chaFile, GUID, data);
            }
        }

        //Not Tested
        public void ClearHairAccOnController(ChaControl chaCtrl, int? coordinateIndex = null) {
            if (null == coordinateIndex) {
                coordinateIndex = chaCtrl.fileStatus.coordinateType;
            }

            //Dictionary<int, object> HairAccessories = HairAccCusController?.GetField("HairAccessories").ToDictionary<int, object>();
            Dictionary<int, object> HairAccessories = GetDataFromController(chaCtrl).ToDictionary<int, object>();
            if (null != HairAccessories && HairAccessories.Remove(coordinateIndex.Value)) {
                GetController(chaCtrl).SetField("HairAccessories", HairAccessories);
                Logger.LogDebug($"Remove {chaCtrl.fileParam.fullname}: {coordinateIndex} : Hair Accessories From Controller");
            } else {
                Logger.LogDebug($"{chaCtrl.fileParam.fullname}'s Hair Accessories already empty in Controller");
            }
        }

        /// <summary>
        /// 檢核LoadState，這是因為異步流程所需，檢查Extdata是否已從Coordinate載入完成
        /// </summary>
        /// <returns>檢核通過</returns>
        public bool CheckControllerPrepared(ChaFileCoordinate backCoordinate) => CheckControllerPrepared(DefaultChaCtrl, backCoordinate);
        public bool CheckControllerPrepared(ChaControl chaCtrl, ChaFileCoordinate backCoordinate)
            => base.CheckControllerPrepared(chaCtrl, (_) => {
                if (!CoordinateLoadOption._isHairAccessoryCustomizerExist || null == backCoordinate) {
                    return true;
                }
                bool? flag = true;

                //Dictionary<int, object> dataFromChaCtrlExt = GetDataFromCoordinate(chaCtrl.nowCoordinate);
                Dictionary<int, object> dataFromBackCoor = GetDataFromCoordinate(backCoordinate);
                GetCoordinateData(GetDataFromController(chaCtrl), chaCtrl.fileStatus.coordinateType, out Dictionary<int, object> dataFromCon);

                //過濾假的HairAccInfo
                if (null != dataFromBackCoor) {
                    foreach (KeyValuePair<int, object> rk in dataFromBackCoor.Where(x => null == chaCtrl.GetAccessoryComponent(x.Key)?.gameObject.GetComponent<ChaCustomHairComponent>()).ToList()) {
                        dataFromBackCoor.Remove(rk.Key);
                    }
                    Logger.LogDebug($"Test with {dataFromBackCoor.Count} HairAcc after remove fake HairAccData {string.Join(",", dataFromBackCoor.Select(x => x.Key.ToString()).ToArray())}");
                }
                if (null != dataFromCon) {
                    foreach (KeyValuePair<int, object> rk in dataFromCon.Where(x => null == chaCtrl.GetAccessoryComponent(x.Key)?.gameObject.GetComponent<ChaCustomHairComponent>()).ToList()) {
                        dataFromCon.Remove(rk.Key);
                    }
                    Logger.LogDebug($"Test with {dataFromCon.Count} HairAcc after remove fake HairAccData {string.Join(",", dataFromCon.Select(x => x.Key.ToString()).ToArray())}");
                }

                if (null != dataFromCon && dataFromCon.Count > 0) {
                    //若現正選中的飾品是髮飾品，則Controller上會有data
                    //故Controller上有可能會比衣裝中多出一個選中的髮飾品資料
                    if (null != dataFromBackCoor && (dataFromBackCoor.Count == dataFromCon.Count || dataFromBackCoor.Count == dataFromCon.Count - 1)) {
                        foreach (KeyValuePair<int, object> kv in dataFromCon) {
                            if (dataFromBackCoor.ContainsKey(kv.Key) || kv.Key == Singleton<ChaCustom.CustomBase>.Instance.selectSlot) {
                                continue;
                            } else { flag = false; break; }
                        }
                    } else { flag = false; }
                } else {
                    //No data from coordinate extData 
                    if (null != dataFromBackCoor && dataFromBackCoor.Count != 0) {
                        flag = false;
                    } else {
                        flag = null;
                    }
                }

                return flag ?? true;
            });

        /// <summary>
        /// 拷貝整個髮飾品資料
        /// </summary>
        /// <param name="sourceCtrl">來源ChaControl</param>
        /// <param name="targetCtrl">目標ChaControl</param>
        /// <returns>拷貝成功</returns>
        public bool CopyAllHairAccExtdata(ChaControl sourceCtrl, ChaControl targetCtrl) {
            if (sourceCtrl == null || targetCtrl == null) {
                Logger.LogError($"CopyAllHairAcc input not correct!");
                return false;
            }

            Dictionary<int, Dictionary<int, object>> allExtData = GetDataFromExtData(sourceCtrl);
            SetToExtData(targetCtrl, allExtData);

            Logger.LogDebug($"Copy all HairAcc by extdata: {sourceCtrl.fileParam.fullname} -> {targetCtrl.fileParam.fullname} ");
            return true;
        }

        /// <summary>
        /// 拷貝整個髮飾品資料 (RefCopy)
        /// </summary>
        /// <param name="sourceCtrl">來源ChaControl</param>
        /// <param name="targetCtrl">目標ChaControl</param>
        /// <returns>拷貝成功</returns>
        public bool CopyHairAccBetweenControllers(ChaControl sourceCtrl, ChaControl targetCtrl) {
            if (sourceCtrl == null || targetCtrl == null) {
                Logger.LogError($"CopyAllHairAcc input not correct!");
                return false;
            }

            MonoBehaviour SourceHairAccCusController = GetController(sourceCtrl);
            MonoBehaviour TargerHairAccCusController = GetController(targetCtrl);

            TargerHairAccCusController.SetField("HairAccessories", SourceHairAccCusController.GetField("HairAccessories"));

            Logger.LogDebug($"Copy(Ref) all HairAcc in the Controller: {sourceCtrl.fileParam.fullname} -> {targetCtrl.fileParam.fullname} ");
            return true;
        }

        /// <summary>
        /// 載入CopyHairAcc所用的來源和目標
        /// </summary>
        /// <param name="sourceChaCtrl">來源</param>
        /// <param name="targetChaCtrl">目標</param>
        public void GetControllerAndBackupData(ChaControl sourceChaCtrl = null, ChaFileCoordinate sourceCoordinate = null, ChaControl targetChaCtrl = null) {
            base.GetControllerAndBackupData(sourceChaCtrl, targetChaCtrl);

            if (null != sourceChaCtrl) {
                SetExtDataFromController(sourceChaCtrl);
                //Source是tmpChara，其資料來自讀入coordinate
                SourceBackup = GetDataFromCoordinate(sourceCoordinate ?? sourceChaCtrl.nowCoordinate);
            }

            if (null != targetChaCtrl) {
                SetExtDataFromController(targetChaCtrl);
                Dictionary<int, Dictionary<int, object>> HairAccessories = GetDataFromExtData(targetChaCtrl);
                GetCoordinateData(HairAccessories, targetChaCtrl.fileStatus.coordinateType, out Dictionary<int, object> _coordinateData);
                TargetBackup = _coordinateData;
            }
        }

        /// <summary>
        /// 拷貝HairAcc資料
        /// </summary>
        /// <param name="sourceChaCtrl"></param>
        /// <param name="sourceSlot"></param>
        /// <param name="targetChaCtrl"></param>
        /// <param name="targetSlot"></param>
        public void CopyHairAcc(ChaControl sourceChaCtrl, int sourceSlot, ChaControl targetChaCtrl, int targetSlot) {
            if (sourceChaCtrl != SourceChaCtrl) GetControllerAndBackupData(sourceChaCtrl: sourceChaCtrl);
            if (targetChaCtrl != TargetChaCtrl) GetControllerAndBackupData(targetChaCtrl: targetChaCtrl);

            if ((bool)SourceController.Invoke("IsHairAccessory", new object[] { sourceSlot })) {
                if (null == SourceBackup || !SourceBackup.TryGetValue(sourceSlot, out object sourceHairInfo)) {
                    Logger.LogError($"-->Copy Hair Acc FAILED: Source Accessory {sourceSlot} Extended Data NOT FOUND.");
                    Logger.LogDebug("-->Copy End");
                    return;
                }

                Dictionary<int, object> _TargetBackup = TargetBackup;
                if (null == _TargetBackup) { _TargetBackup = new Dictionary<int, object>(); }
                _TargetBackup[targetSlot] = CopyHairAccInfoObject_ExtData(sourceHairInfo);
                TargetBackup = _TargetBackup;

                Logger.LogDebug($"-->Copy Hair Acc Finish: {sourceChaCtrl.fileParam.fullname} {sourceSlot} -> {targetChaCtrl.fileParam.fullname} {targetSlot}");
                Logger.LogDebug($"-->Hair Acc: {TargetBackup.Count} : {string.Join(",", TargetBackup.Select(x => x.Key.ToString()).ToArray())}");
            } else {
                TargetController.Invoke("RemoveHairAccessoryInfo", new object[] { targetSlot });
                Logger.LogDebug($"-->Clear Hair Acc Finish: Slot {targetSlot}");
            }
        }

        /// <summary>
        /// 返回拷貝的HairInfo
        /// </summary>
        /// <param name="sourceHairInfo"></param>
        /// <returns></returns>
        public static object CopyHairAccInfoObject_ExtData(object sourceHairInfo) {
            return new Dictionary<object, object> {
                { "HairGloss",sourceHairInfo.ToDictionary<object, object>()["HairGloss"] },
                { "ColorMatch",sourceHairInfo.ToDictionary<object, object>()["ColorMatch"] },
                { "OutlineColor",sourceHairInfo.ToDictionary<object, object>()["OutlineColor"]},
                { "AccessoryColor",sourceHairInfo.ToDictionary<object, object>()["AccessoryColor"]},
                { "HairLength", sourceHairInfo.ToDictionary<object, object>()["HairLength"] }
            };
        }
    }
}