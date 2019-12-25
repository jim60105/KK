using ExtensibleSaveFormat;
using Extension;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_StudioCoordinateLoadOption {

    internal class HairAccessoryCustomizer_Support {
        private static readonly BepInEx.Logging.ManualLogSource Logger = KK_StudioCoordinateLoadOption.Logger;
        private static object SourceHairAccCusController;
        private static object TargetHairAccCusController;
        private static readonly string GUID = "com.deathweasel.bepinex.hairaccessorycustomizer";
        public static Dictionary<int, object> sourceDict = new Dictionary<int, object>();
        public static Dictionary<int, object> targetDict = new Dictionary<int, object>();

        public static bool LoadAssembly() {
            if (null != KK_StudioCoordinateLoadOption.TryGetPluginInstance(GUID, new Version(1, 1, 2))) {
                Logger.LogDebug("Hair Accessory Customizer found");
                return true;
            } else {
                Logger.LogDebug("Load assembly FAILED: Hair Accessory Customizer");
                return false;
            }
        }

        /// <summary>
        /// 檢查是否為頭髮飾品
        /// </summary>
        /// <param name="chaCtrl">對象角色</param>
        /// <param name="index">飾品欄位index</param>
        /// <returns></returns>
        public static bool IsHairAccessory(ChaControl chaCtrl, int index) {
            var HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            return (bool)HairAccCusController.Invoke("IsHairAccessory", new object[] { index });
        }

        /// <summary>
        /// 從ExtendedData取得給定ChaControl的HairAccessories和單一hairAccessoryInfo
        /// </summary>
        /// <param name="chaCtrl">要查詢的ChaControl</param>
        /// <param name="nowcoordinateExtData">nowCoordinate的hairAccessoryInfo</param>
        /// <returns>整個HairAccessories</returns>
        public static Dictionary<int, Dictionary<int, object>> GetExtendedDataFromExtData(ChaControl chaCtrl, out Dictionary<int, object> nowcoordinateExtData) {
            nowcoordinateExtData = null;
            PluginData data = ExtendedSave.GetExtendedDataById(chaCtrl.chaFile, GUID);
            if (data != null && data.data.TryGetValue("HairAccessories", out var loadedHairAccessories) && loadedHairAccessories != null) {
                Dictionary<int, Dictionary<int, object>> result = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<int, object>>>((byte[])loadedHairAccessories);
                result?.TryGetValue(chaCtrl.fileStatus.coordinateType, out nowcoordinateExtData);
                if (null != nowcoordinateExtData) {
                    Logger.LogDebug($"Get {chaCtrl.fileParam.fullname} Hair Accessories: {nowcoordinateExtData.Count} From ExtData");
                    return result;
                }
            }
            Logger.LogDebug($"No Hair Accessories get from {chaCtrl.fileParam.fullname} From ExtData");
            return null;
        }

        /// <summary>
        /// 從HairAccessoryController取得給定ChaControl的HairAccessories和單一hairAccessoryInfo
        /// </summary>
        /// <param name="chaCtrl">要查詢的ChaControl</param>
        /// <param name="nowcoordinateExtData">nowCoordinate的hairAccessoryInfo</param>
        /// <returns>整個HairAccessories</returns>
        public static Dictionary<int, Dictionary<int, object>> GetExtendedDataFromController(ChaControl chaCtrl, out Dictionary<int, object> nowcoordinateExtData) {
            nowcoordinateExtData = null;
            var HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            var HairAccessories = HairAccCusController?.GetField("HairAccessories").ToDictionary<int, object>();
            Dictionary<int, Dictionary<int, object>> result = null;
            if (null != HairAccessories && HairAccessories.Count != 0) {
                result = new Dictionary<int, Dictionary<int, object>>();
                foreach (var kv in HairAccessories) {
                    result.Add(kv.Key, kv.Value.ToDictionary<int, object>());
                }
                result.TryGetValue(chaCtrl.fileStatus.coordinateType, out nowcoordinateExtData);
                Logger.LogDebug($"Get {chaCtrl.fileParam.fullname} Hair Accessories From Controller: {nowcoordinateExtData.Count}");
                Logger.LogDebug($"->{string.Join(",", nowcoordinateExtData.Select(x => x.Key.ToString()).ToArray())}");
                return result;
            }
            Logger.LogDebug($"No Hair Accessories get from {chaCtrl.fileParam.fullname} From Controller");
            return null;
        }

        /// <summary>
        /// 從ExtendedData取得給定ChaControl的HairAccessories和單一hairAccessoryInfo
        /// </summary>
        /// <param name="chaCtrl">要查詢的ChaControl</param>
        /// <param name="nowcoordinateExtData">nowCoordinate的hairAccessoryInfo</param>
        /// <returns>整個HairAccessories</returns>
        public static Dictionary<int, object> GetExtendedDataFromCoordinate(ChaFileCoordinate coordinate) {
            Dictionary<int, object> nowcoordinateExtData;
            PluginData data = ExtendedSave.GetExtendedDataById(coordinate, GUID);
            if (data != null && data.data.TryGetValue("CoordinateHairAccessories", out var loadedHairAccessories) && loadedHairAccessories != null) {
                nowcoordinateExtData = MessagePackSerializer.Deserialize<Dictionary<int, object>>((byte[])loadedHairAccessories);
                if (null == nowcoordinateExtData) {
                    nowcoordinateExtData = new Dictionary<int, object>();
                }
                Logger.LogDebug($"Get {coordinate.coordinateName} Hair Accessories: {nowcoordinateExtData.Count} (1)");
                Logger.LogDebug($"->{string.Join(",", nowcoordinateExtData.Select(x => x.Key.ToString()).ToArray())}");
                return nowcoordinateExtData;
            }
            Logger.LogDebug($"No Hair Accessories get from {coordinate.coordinateFileName} (1)");
            return null;
        }

        /// <summary>
        /// 由Coordinate載入HairAcc至Controller內
        /// </summary>
        /// <param name="chaCtrl">要被設定的ChaControl</param>
        /// <param name="coordinate">要載入的coordibate</param>
        /// <returns></returns>
        public static bool SetExtendedDataFromCoordinate(ChaControl chaCtrl, ChaFileCoordinate coordinate = null) {
            if (null == coordinate) {
                coordinate = chaCtrl.nowCoordinate;
            }
            var HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
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
            var HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            //注意這條是異步執行
            HairAccCusController.Invoke("OnReload", new object[] { 2, false });
        }

        /// <summary>
        /// 由Controller載入HairAcc至ExtData內
        /// </summary>
        /// <param name="chaCtrl">要被設定的ChaControl</param>
        /// <returns></returns>
        public static void SetExtendedDataFromController(ChaControl chaCtrl) {
            var HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            HairAccCusController.Invoke("OnCardBeingSaved", new object[] { 0 });
        }

        /// <summary>
        /// 將dict存入ChaControl ExtendedData
        /// </summary>
        /// <param name="chaCtrl">目標ChaControl</param>
        /// <param name="dict">要存入的dict</param>
        public static void SaveExtendedData(ChaControl chaCtrl, Dictionary<int, object> dict) {
            var HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            PluginData data = new PluginData();
            var allExtData = GetExtendedDataFromExtData(chaCtrl, out _);
            if (dict.Count == 0 && (null == allExtData || allExtData.Count == 0)) {
                data.data.Add("HairAccessories", null);
            } else {
                if (null == allExtData) {
                    allExtData = new Dictionary<int, Dictionary<int, object>>();
                }
                allExtData[chaCtrl.fileStatus.coordinateType] = dict;
                data.data.Add("HairAccessories", MessagePackSerializer.Serialize(allExtData));
            }
            ExtendedSave.SetExtendedDataById(chaCtrl.chaFile, GUID, data);
        }

        /// <summary>
        /// 將dict存入Coordinate ExtendedData
        /// </summary>
        /// <param name="coordinate">目標Coordinate</param>
        /// <param name="dict">要存入的dict</param>
        public static void SaveExtendedData(ChaFileCoordinate coordinate, Dictionary<int, object> dict) {
            var data = new PluginData();
            if (dict.Count > 0)
                data.data.Add("CoordinateHairAccessories", MessagePackSerializer.Serialize(dict));
            else
                data.data.Add("CoordinateHairAccessories", null);
            ExtendedSave.SetExtendedDataById(coordinate, GUID, data);
        }

        /// <summary>
        /// 檢核Reload是否完成，這是因為異步流程所需
        /// </summary>
        /// <param name="chaCtrl">檢核的ChaControl</param>
        /// <param name="coordinate">判斷基礎的Coordinate，留空取chaCtrl.nowCoordinate</param>
        /// <returns>檢核通過</returns>
        public static bool CheckReloadState(ChaControl chaCtrl, ChaFileCoordinate coordinate = null) {
            if (!KK_StudioCoordinateLoadOption._isHairAccessoryCustomizerExist) {
                return true;
            }
            if (null == coordinate) {
                coordinate = chaCtrl.nowCoordinate;
            }

            var ext = GetExtendedDataFromCoordinate(chaCtrl.nowCoordinate);
            if (null != ext && ext.Count > 0) {
                GetExtendedDataFromController(chaCtrl, out var ext2);
                if (null != ext2 && ext2.Count == ext.Count) {
                    foreach (var kv in ext) {
                        if (!ext2.ContainsKey(kv.Key)) {
                            SetExtendedDataFromCoordinate(chaCtrl);
                            return false;
                        }
                    }
                } else {
                    SetExtendedDataFromCoordinate(chaCtrl);
                    return false;
                }
            }
            var HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            HairAccCusController.Invoke("UpdateAccessories", new object[] { true });
            return true;
        }

        /// <summary>
        /// 拷貝整個髮飾品資料
        /// </summary>
        /// <param name="sourceCtrl">來源ChaControl</param>
        /// <param name="targetCtrl">目標ChaControl</param>
        /// <returns>拷貝成功</returns>
        public static bool CopyAllHairAcc(ChaControl sourceCtrl = null, ChaControl targetCtrl = null) {
            Dictionary<int, object> dict = null;
            if (sourceCtrl == null && targetCtrl == null) {
                dict = sourceDict;
            } else if (sourceCtrl == null || targetCtrl == null) {
                Logger.LogError($"CopyAllHairAcc input not correct!");
                return false;
            }
            if (null != dict || (null != GetExtendedDataFromExtData(sourceCtrl, out dict) && null != dict)) {
                for (int i = 0; i < dict.Count; i++) {
                    dict[dict.Keys.ElementAt(i)] = CopyHairInfo(dict.Values.ElementAt(i));
                }
                SaveExtendedData(targetCtrl, dict);
                targetCtrl.ChangeAccessory(true);
                Logger.LogDebug($"Copy all HairAcc ({dict.Count}): {sourceCtrl.fileParam.fullname} -> {targetCtrl.fileParam.fullname} ");
                return true;
            } else {
                Logger.LogDebug($"CopyAllHairAcc input not found!");
                return false;
            }
        }

        public static void GetControllerAndHairDict(ChaControl sourceChaCtrl, ChaControl targetChaCtrl = null) {
            CleanHairAccBackup();
            SourceHairAccCusController = sourceChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            TargetHairAccCusController = targetChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            if (null == SourceHairAccCusController || null == TargetHairAccCusController) {
                Logger.LogDebug("No Hair Accessory Customizer Controller found");
                return;
            }
            Logger.LogDebug("Source-----");
            GetExtendedDataFromExtData(sourceChaCtrl, out sourceDict);
            Logger.LogDebug("Target-----");
            GetExtendedDataFromExtData(targetChaCtrl, out targetDict);
            //Logger.LogDebug("-->Backup End");
        }

        public static void CopyHairAcc(ChaControl sourceChaCtrl, int sourceSlot, ChaControl targetChaCtrl, int targetSlot) {
            GetControllerAndHairDict(sourceChaCtrl, targetChaCtrl);

            if ((bool)SourceHairAccCusController.Invoke("IsHairAccessory", new object[] { sourceSlot })) {
                if (null == sourceDict || !sourceDict.TryGetValue(sourceSlot, out var sourceHairInfo)) {
                    Logger.LogError($"-->Copy Hair Acc FAILED: Source Accessory {sourceSlot} Extended Data NOT FOUND.");
                    Logger.LogDebug("-->Copy End");
                    return;
                }
                if (null == targetDict) {
                    targetDict = new Dictionary<int, object>();
                } else if (targetDict.ContainsKey(targetSlot)) {
                    targetDict.Remove(targetSlot);
                }
                //targetDict.Add(targetSlot, CopyHairInfo(sourceHairInfo));
                targetDict.Add(targetSlot, (sourceHairInfo));
                sourceDict.Remove(sourceSlot);

                SaveExtendedData(sourceChaCtrl, sourceDict);
                SaveExtendedData(targetChaCtrl, targetDict);
                Logger.LogDebug($"-->Copy Hair Acc Finish: {sourceChaCtrl.fileParam.fullname} {sourceSlot} -> {targetChaCtrl.fileParam.fullname} {targetSlot}");
                Logger.LogDebug($"-->Hair Acc: {targetDict.Count} : {string.Join(",", targetDict.Select(x => x.Key.ToString()).ToArray())}");
            } else {
                TargetHairAccCusController.Invoke("RemoveHairAccessoryInfo", new object[] { targetSlot });
                Logger.LogDebug($"-->Clear Hair Acc Finish: Slot {targetSlot}");
            }
        }

        /// <summary>
        /// 拷貝HairInfo
        /// </summary>
        /// <param name="sourceHairInfo">拷貝對象</param>
        /// <returns>拷貝結果</returns>
        public static object CopyHairInfo(object sourceHairInfo) {
            return new Dictionary<object, object> {
                { "HairGloss",sourceHairInfo.ToDictionary<object, object>()["HairGloss"] },
                { "ColorMatch",sourceHairInfo.ToDictionary<object, object>()["ColorMatch"] },
                { "OutlineColor",sourceHairInfo.ToDictionary<object, object>()["OutlineColor"]},
                { "AccessoryColor",sourceHairInfo.ToDictionary<object, object>()["AccessoryColor"]},
                { "HairLength", sourceHairInfo.ToDictionary<object, object>()["HairLength"] }
            };
        }

        public static void CleanHairAccBackup() {
            SourceHairAccCusController = null;
            TargetHairAccCusController = null;
            sourceDict = null;
            targetDict = null;
        }
    }
}