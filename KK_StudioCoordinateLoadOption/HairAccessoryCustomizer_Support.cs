using ExtensibleSaveFormat;
using Extension;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_StudioCoordinateLoadOption {

    internal class HairAccessoryCustomizer_Support {
        private static readonly BepInEx.Logging.ManualLogSource Logger = KK_StudioCoordinateLoadOption.Logger;
        private static object SourceHairAccCusController;
        private static object TargetHairAccCusController;
        private static readonly string GUID = "com.deathweasel.bepinex.hairaccessorycustomizer";
        public static Dictionary<int, object> sourceHairBackup = new Dictionary<int, object>();
        public static Dictionary<int, object> targetHairBackup = new Dictionary<int, object>();

        public static bool LoadAssembly() {
            if (null != Extension.Extension.TryGetPluginInstance(GUID, new Version(1, 1, 2))) {
                Logger.LogDebug("Hair Accessory Customizer found");
                return true;
            } else {
                Logger.LogDebug("Load assembly FAILED: Hair Accessory Customizer");
                return false;
            }
        }

        /// <summary>
        /// 從ExtendedData取得給定ChaControl的HairAccessories和單一hairAccessoryInfo
        /// </summary>
        /// <param name="chaCtrl">要查詢的ChaControl</param>
        /// <param name="nowcoordinateExtData">nowCoordinate的hairAccessoryInfo</param>
        /// <returns>整個HairAccessories</returns>
        public static Dictionary<int, Dictionary<int, object>> GetDataFromExtData(ChaControl chaCtrl, out Dictionary<int, object> nowcoordinateExtData) {
            nowcoordinateExtData = null;
            PluginData data = ExtendedSave.GetExtendedDataById(chaCtrl.chaFile, GUID);
            if (data != null && data.data.TryGetValue("HairAccessories", out object loadedHairAccessories) && loadedHairAccessories is byte[] loadedBA) {
                Dictionary<int, Dictionary<int, object>> result = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<int, object>>>(loadedBA);
                result?.TryGetValue(chaCtrl.fileStatus.coordinateType, out nowcoordinateExtData);
                if (null != nowcoordinateExtData) {
                    Logger.LogDebug($"Get {chaCtrl.fileParam.fullname} Hair Accessories From ExtData: {nowcoordinateExtData.Count}");
                    Logger.LogDebug($"->{string.Join(",", nowcoordinateExtData.Select(x => x.Key.ToString()).ToArray())}");
                } else {
                    Logger.LogDebug($"Get {chaCtrl.fileParam.fullname} Hair Accessories From ExtData, but no HairAccInfo on current coordinate");
                }
                return result;
            }
            Logger.LogDebug($"No Hair Accessories get from {chaCtrl.fileParam.fullname}'s ExtData");
            return null;
        }

        /// <summary>
        /// 從HairAccessoryController取得給定ChaControl的HairAccessories和單一hairAccessoryInfo
        /// </summary>
        /// <param name="chaCtrl">要查詢的ChaControl</param>
        /// <param name="nowcoordinateExtData">nowCoordinate的hairAccessoryInfo</param>
        /// <returns>整個HairAccessories</returns>
        public static Dictionary<int, Dictionary<int, object>> GetDataFromController(ChaControl chaCtrl, out Dictionary<int, object> nowcoordinateExtData) {
            nowcoordinateExtData = null;
            MonoBehaviour HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            Dictionary<int, object> HairAccessories = HairAccCusController?.GetField("HairAccessories").ToDictionary<int, object>();
            Dictionary<int, Dictionary<int, object>> result = null;
            if (null != HairAccessories && HairAccessories.Count != 0) {
                result = new Dictionary<int, Dictionary<int, object>>();
                foreach (KeyValuePair<int, object> kv in HairAccessories) {
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
                result.TryGetValue(chaCtrl.fileStatus.coordinateType, out nowcoordinateExtData);
                if (null != nowcoordinateExtData) {
                    Logger.LogDebug($"Get {chaCtrl.fileParam.fullname} Hair Accessories From Controller: {nowcoordinateExtData.Count}");
                    Logger.LogDebug($"->{string.Join(",", nowcoordinateExtData.Select(x => x.Key.ToString()).ToArray())}");
                } else {
                    Logger.LogDebug($"Get {chaCtrl.fileParam.fullname} Hair Accessories From Controller, but no HairAccInfo on current coordinate");
                }
                return result;
            }
            Logger.LogDebug($"No Hair Accessories get from {chaCtrl.fileParam.fullname}'s Controller");
            nowcoordinateExtData = null;
            return null;
        }

        /// <summary>
        /// 從ExtendedData取得給定ChaControl的HairAccessories和單一hairAccessoryInfo
        /// </summary>
        /// <param name="chaCtrl">要查詢的ChaControl</param>
        /// <param name="nowcoordinateExtData">nowCoordinate的hairAccessoryInfo</param>
        /// <returns>整個HairAccessories</returns>
        public static Dictionary<int, object> GetDataFromCoordinate(ChaFileCoordinate coordinate) {
            Dictionary<int, object> nowcoordinateExtData;
            PluginData data = ExtendedSave.GetExtendedDataById(coordinate, GUID);
            if (data != null && data.data.TryGetValue("CoordinateHairAccessories", out object loadedHairAccessories) && loadedHairAccessories != null) {
                nowcoordinateExtData = MessagePackSerializer.Deserialize<Dictionary<int, object>>((byte[])loadedHairAccessories);
                if (null == nowcoordinateExtData) {
                    nowcoordinateExtData = new Dictionary<int, object>();
                }
                Logger.LogDebug($"Get Hair Accessories from coordinate {coordinate.coordinateName}: {nowcoordinateExtData.Count} (1)");
                Logger.LogDebug($"->{string.Join(",", nowcoordinateExtData.Select(x => x.Key.ToString()).ToArray())}");
                return nowcoordinateExtData;
            }
            Logger.LogDebug($"No Hair Accessories get from coordinate {coordinate.coordinateFileName}");
            return null;
        }

        /// <summary>
        /// 由Coordinate載入HairAcc至Controller內
        /// </summary>
        /// <param name="chaCtrl">要被設定的ChaControl</param>
        /// <param name="coordinate">要載入的coordibate</param>
        /// <returns></returns>
        public static bool SetControllerFromCoordinate(ChaControl chaCtrl, ChaFileCoordinate coordinate = null) {
            if (null == coordinate) {
                coordinate = chaCtrl.nowCoordinate;
            }
            MonoBehaviour HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            if (null != HairAccCusController) {
                //注意這條是異步執行
                HairAccCusController.Invoke("OnCoordinateBeingLoaded", new object[] { coordinate, false });
                return true;
            } else {
                Logger.LogDebug($"No HairAccController found!");
            }
            return false;
        }

        /// <summary>
        /// 由ExtData載入HairAcc至Controller內，注意這條是異步執行
        /// </summary>
        /// <param name="chaCtrl">要被設定的ChaControl</param>
        /// <returns></returns>
        public static void SetControllerFromExtData(ChaControl chaCtrl) {
            MonoBehaviour HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            //注意這條是異步執行
            HairAccCusController.Invoke("OnReload", new object[] { 2, false });
        }

        /// <summary>
        /// 由Controller載入HairAcc至ExtData內
        /// </summary>
        /// <param name="chaCtrl">要被設定的ChaControl</param>
        /// <returns></returns>
        public static void SetExtDataFromController(ChaControl chaCtrl) {
            MonoBehaviour HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().First(x => Equals(x.GetType().Name, "HairAccessoryController"));
            HairAccCusController.Invoke("OnCardBeingSaved", new object[] { 1 });
        }

        /// <summary>
        /// 將dict存入ChaControl ExtendedData
        /// </summary>
        /// <param name="chaCtrl">目標ChaControl</param>
        /// <param name="dict">要存入的dict</param>
        public static void SetToExtData(ChaControl chaCtrl, Dictionary<int, object> dict) {
            PluginData data = new PluginData();
            Dictionary<int, Dictionary<int, object>> allExtData = GetDataFromExtData(chaCtrl, out _);
            int coorType = chaCtrl.fileStatus.coordinateType;
            if ((null == dict || dict.Count == 0) && (null == allExtData || allExtData.Count == 0)) {
                data = null;
            } else {
                if (null == allExtData) {
                    allExtData = new Dictionary<int, Dictionary<int, object>>();
                    Logger.LogDebug($"HairAccCustomizer info not found while saving.");
                }
                if (allExtData.ContainsKey(coorType)) {
                    allExtData[coorType].Clear();
                    allExtData.Remove(coorType);
                }
                if (null != dict) {
                    allExtData[coorType] = dict;
                }
                data.data.Add("HairAccessories", MessagePackSerializer.Serialize(allExtData));
            }
            ExtendedSave.SetExtendedDataById(chaCtrl.chaFile, GUID, data);
        }

        public static void ClearHairAccOnController(ChaControl chaCtrl, int? coordinateIndex = null) {
            if (null == coordinateIndex) {
                coordinateIndex = chaCtrl.fileStatus.coordinateType;
            }

            MonoBehaviour HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            //Dictionary<int, object> HairAccessories = HairAccCusController?.GetField("HairAccessories").ToDictionary<int, object>();
            object HairAccessories = HairAccCusController?.GetField("HairAccessories");
            if (HairAccessories is IDictionary h) {
                h.Remove(coordinateIndex);
                HairAccCusController.SetField("HairAccessories", h);
                Logger.LogDebug($"Remove {chaCtrl.fileParam.fullname} {Enum.GetName(typeof(ChaFileDefine.CoordinateType), coordinateIndex)} Hair Accessories From Controller");
            } else {
                Logger.LogDebug($"{chaCtrl.fileParam.fullname}'s Hair Accessories already empty in Controller");
            }
        }

        /// <summary>
        /// 檢核LoadState，這是因為異步流程所需，檢查Extdata是否已從Coordinate載入完成
        /// </summary>
        /// <param name="chaCtrl">檢核的ChaControl</param>
        /// <returns>檢核通過</returns>
        public static bool CheckControllerPrepared(ChaControl chaCtrl, bool doReload = false) {
            if (!KK_StudioCoordinateLoadOption._isHairAccessoryCustomizerExist) {
                return true;
            }
            ChaFileCoordinate coordinate = chaCtrl.nowCoordinate;
            bool? flag = true;

            Dictionary<int, object> dataFromCoorExt = GetDataFromCoordinate(coordinate);
            GetDataFromController(chaCtrl, out Dictionary<int, object> dataFromCon);

            //過濾假的HairAccInfo
            if (null != dataFromCoorExt) {
                foreach (KeyValuePair<int, object> rk in dataFromCoorExt.Where(x => null == Patches.GetChaAccessoryComponent(chaCtrl, x.Key)?.gameObject.GetComponent<ChaCustomHairComponent>()).ToList()) {
                    dataFromCoorExt.Remove(rk.Key);
                }
                Logger.LogDebug($"Test with {dataFromCoorExt.Count} HairAcc after remove fake HairAccData {string.Join(",", dataFromCoorExt.Select(x => x.Key.ToString()).ToArray())}");
            }
            if (null != dataFromCon) {
                foreach (KeyValuePair<int, object> rk in dataFromCon.Where(x => null == Patches.GetChaAccessoryComponent(chaCtrl, x.Key)?.gameObject.GetComponent<ChaCustomHairComponent>()).ToList()) {
                    dataFromCon.Remove(rk.Key);
                }
                Logger.LogDebug($"Test with {dataFromCon.Count} HairAcc after remove fake HairAccData {string.Join(",", dataFromCon.Select(x => x.Key.ToString()).ToArray())}");
            }

            if (null != dataFromCoorExt && dataFromCoorExt.Count > 0) {
                if (null != dataFromCon && dataFromCon.Count == dataFromCoorExt.Count) {
                    foreach (KeyValuePair<int, object> kv in dataFromCoorExt) {
                        if (dataFromCon.ContainsKey(kv.Key)) {
                            continue;
                        } else { flag = false; break; }
                    }
                } else { flag = false; }
            } else {
                //No data from coordinate extData 
                if (null != dataFromCon && dataFromCon.Count != 0) {
                    flag = false;
                } else {
                    flag = null;
                }
            }

            if (null == flag) {
                return true;
            } else if (true == flag) {
                MonoBehaviour HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
                HairAccCusController.Invoke("UpdateAccessories", new object[] { true });
                return true;
            } else {
                if (doReload) {
                    SetControllerFromCoordinate(chaCtrl, coordinate);
                }
                return false;
            }
        }

        /// <summary>
        /// 拷貝整個髮飾品資料
        /// </summary>
        /// <param name="sourceCtrl">來源ChaControl</param>
        /// <param name="targetCtrl">目標ChaControl</param>
        /// <returns>拷貝成功</returns>
        public static bool CopyAllHairAcc(ChaControl sourceCtrl, ChaControl targetCtrl) {
            if (sourceCtrl == null || targetCtrl == null) {
                Logger.LogError($"CopyAllHairAcc input not correct!");
                return false;
            }

            if (null != GetDataFromExtData(sourceCtrl, out Dictionary<int, object> dict) && null != dict) {
                for (int i = 0; i < dict.Count; i++) {
                    dict[dict.Keys.ElementAt(i)] = CopyHairAccInfoObject(dict.Values.ElementAt(i));
                    //dict[dict.Keys.ElementAt(i)] = (dict.Values.ElementAt(i));
                }
                SetToExtData(targetCtrl, dict);

                targetCtrl.ChangeAccessory(true);
                Logger.LogDebug($"Copy all HairAcc ({dict.Count}): {sourceCtrl.fileParam.fullname} -> {targetCtrl.fileParam.fullname} ");
                return true;
            } else {
                Logger.LogDebug($"CopyAllHairAcc input not found!");
                return false;
            }
        }

        /// <summary>
        /// 載入CopyHairAcc所用的來源和目標
        /// </summary>
        /// <param name="sourceChaCtrl">來源</param>
        /// <param name="targetChaCtrl">目標</param>
        public static void GetControllerAndBackupData(ChaControl sourceChaCtrl, ChaControl targetChaCtrl = null) {
            if (null != sourceChaCtrl) {
                SetExtDataFromController(sourceChaCtrl);
                ClearHairAccBackup();
                SourceHairAccCusController = sourceChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
                if (null == SourceHairAccCusController) {
                    Logger.LogDebug("No Source Hair Accessory Customizer Controller found");
                    return;
                }
                Logger.LogDebug("Source-----");
                GetDataFromExtData(sourceChaCtrl, out sourceHairBackup);
            }

            if (null != targetChaCtrl) {
                SetExtDataFromController(targetChaCtrl);
                TargetHairAccCusController = targetChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
                if (null == TargetHairAccCusController) {
                    Logger.LogDebug("No Target Hair Accessory Customizer Controller found");
                    return;
                }
                Logger.LogDebug("Target-----");
                GetDataFromExtData(targetChaCtrl, out targetHairBackup);
            }
        }

        /// <summary>
        /// 拷貝HairAcc資料
        /// </summary>
        /// <param name="sourceChaCtrl"></param>
        /// <param name="sourceSlot"></param>
        /// <param name="targetChaCtrl"></param>
        /// <param name="targetSlot"></param>
        public static void CopyHairAcc(ChaControl sourceChaCtrl, int sourceSlot, ChaControl targetChaCtrl, int targetSlot) {
            if ((bool)SourceHairAccCusController.Invoke("IsHairAccessory", new object[] { sourceSlot })) {
                if (null == sourceHairBackup || !sourceHairBackup.TryGetValue(sourceSlot, out object sourceHairInfo)) {
                    Logger.LogError($"-->Copy Hair Acc FAILED: Source Accessory {sourceSlot} Extended Data NOT FOUND.");
                    Logger.LogDebug("-->Copy End");
                    return;
                }
                if (null == targetHairBackup) {
                    targetHairBackup = new Dictionary<int, object>();
                } else if (targetHairBackup.ContainsKey(targetSlot)) {
                    targetHairBackup.Remove(targetSlot);
                }
                targetHairBackup.Add(targetSlot, CopyHairAccInfoObject(sourceHairInfo));
                //targetDict.Add(targetSlot, (sourceHairInfo));

                Logger.LogDebug($"-->Copy Hair Acc Finish: {sourceChaCtrl.fileParam.fullname} {sourceSlot} -> {targetChaCtrl.fileParam.fullname} {targetSlot}");
                Logger.LogDebug($"-->Hair Acc: {targetHairBackup.Count} : {string.Join(",", targetHairBackup.Select(x => x.Key.ToString()).ToArray())}");
            } else {
                TargetHairAccCusController.Invoke("RemoveHairAccessoryInfo", new object[] { targetSlot });
                Logger.LogDebug($"-->Clear Hair Acc Finish: Slot {targetSlot}");
            }
        }

        /// <summary>
        /// 返回拷貝的HairInfo
        /// </summary>
        /// <param name="sourceHairInfo"></param>
        /// <returns></returns>
        public static object CopyHairAccInfoObject(object sourceHairInfo) {
            return new Dictionary<object, object> {
                { "HairGloss",sourceHairInfo.ToDictionary<object, object>()["HairGloss"] },
                { "ColorMatch",sourceHairInfo.ToDictionary<object, object>()["ColorMatch"] },
                { "OutlineColor",sourceHairInfo.ToDictionary<object, object>()["OutlineColor"]},
                { "AccessoryColor",sourceHairInfo.ToDictionary<object, object>()["AccessoryColor"]},
                { "HairLength", sourceHairInfo.ToDictionary<object, object>()["HairLength"] }
            };
        }

        public static void ClearHairAccBackup() {
            SourceHairAccCusController = null;
            TargetHairAccCusController = null;
            sourceHairBackup = null;
            targetHairBackup = null;
        }
    }
}