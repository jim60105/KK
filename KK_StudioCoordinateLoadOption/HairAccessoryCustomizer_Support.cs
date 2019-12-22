using ExtensibleSaveFormat;
using Extension;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_StudioCoordinateLoadOption {

    internal class HairAccessoryCustomizer_Support {
        private static object SourceHairAccCusController;
        private static object TargetHairAccCusController;
        private static readonly string GUID = "com.deathweasel.bepinex.hairaccessorycustomizer";
        public static Dictionary<int, object> sourceDict = new Dictionary<int, object>();
        public static Dictionary<int, object> targetDict = new Dictionary<int, object>();
        //private static Type HairAccessoryInfoType = null;

        public static bool LoadAssembly() {
            if (null != KK_StudioCoordinateLoadOption.TryGetPluginInstance(GUID, new Version(1, 1, 2))) {
                //MessagePack.Resolvers.CompositeResolver.RegisterAndSetAsDefault(MessagePack.Unity.UnityResolver.Instance, MessagePack.Resolvers.StandardResolver.Instance);
                KK_StudioCoordinateLoadOption.Logger.LogDebug("Hair Accessory Customizer found");
                return true;
            } else {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("Load assembly FAILED: Hair Accessory Customizer");
                return false;
            }
        }

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
                    KK_StudioCoordinateLoadOption.Logger.LogDebug($"Get {chaCtrl.fileParam.fullname} Hair Accessories: {nowcoordinateExtData.Count} (1)");
                    return result;
                }
            }
            KK_StudioCoordinateLoadOption.Logger.LogDebug($"No Hair Accessories get from {chaCtrl.fileParam.fullname} (1)");
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
            var HairAccessories = HairAccCusController.GetField("HairAccessories").ToDictionary<int, object>();
            Dictionary<int, Dictionary<int, object>> result = null;
            if (null != HairAccessories && HairAccessories.Count != 0) {
                result = new Dictionary<int, Dictionary<int, object>>();
                foreach (var kv in HairAccessories) {
                    result.Add(kv.Key, kv.Value.ToDictionary<int, object>());
                }
                result.TryGetValue(chaCtrl.fileStatus.coordinateType, out nowcoordinateExtData);
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"Get {chaCtrl.fileParam.fullname} Hair Accessories: {nowcoordinateExtData.Count} (2)");
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"->{string.Join(",", nowcoordinateExtData.Select(x => x.Key.ToString()).ToArray())}");
                return result;
            }
            KK_StudioCoordinateLoadOption.Logger.LogDebug($"No Hair Accessories get from {chaCtrl.fileParam.fullname} (2)");
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
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"Get {coordinate.coordinateName} Hair Accessories: {nowcoordinateExtData.Count} (1)");
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"->{string.Join(",", nowcoordinateExtData.Select(x => x.Key.ToString()).ToArray())}");
                return nowcoordinateExtData;
            }
            KK_StudioCoordinateLoadOption.Logger.LogDebug($"No Hair Accessories get from {coordinate.coordinateFileName} (1)");
            return null;
        }

        public static bool SetExtendedDataFromCoordinate(ChaControl chaCtrl, ChaFileCoordinate coordinate = null) {
            if (null == coordinate) {
                coordinate = chaCtrl.nowCoordinate;
            }
            var HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            if (null != HairAccCusController) {
                //HairAccCusController.Invoke("LoadCoordinateData", new object[] { coordinate });
                HairAccCusController.Invoke("OnCoordinateBeingLoaded", new object[] { coordinate, false });
                //HairAccCusController.Invoke("LoadData");
                //chaCtrl.ChangeCoordinateTypeAndReload(false);
                //HairAccCusController.Invoke("OnCoordinateBeingSaved", new object[] {chaCtrl.nowCoordinate });
                //HairAccCusController.Invoke("OnCardBeingSaved", new object[] { 0 });
                return true;
            } else {
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"No HairAccController found!");
            }
            return false;
        }

        public static void SetControllerFromExtData(ChaControl chaCtrl) {
            var HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            HairAccCusController.Invoke("OnReload", new object[] { 2, false });
        }

        public static void SetExtendedDataFromController(ChaControl chaCtrl) {
            var HairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            HairAccCusController.Invoke("OnCardBeingSaved", new object[] { 0 });
        }

        public static void SaveExtendedDataToChaCtrl(ChaControl chaCtrl, Dictionary<int, object> dict) {
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

        public static void SaveExtendedDataToCoordinate(ChaFileCoordinate coordinate, Dictionary<int, object> dict) {
            var data = new PluginData();
            if (dict.Count > 0)
                data.data.Add("CoordinateHairAccessories", MessagePackSerializer.Serialize(dict));
            else
                data.data.Add("CoordinateHairAccessories", null);
            ExtendedSave.SetExtendedDataById(coordinate, GUID, data);
        }

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

        public static bool CopyAllHairAcc(ChaControl sourceCtrl = null, ChaControl targetCtrl = null) {
            //Dictionary<int, Dictionary<int, object>> sourceDictionary = sourceDict;
            //Dictionary<int, Dictionary<int, object>> targetDictionary = targetDict;
            Dictionary<int, object> dict = null;
            if (sourceCtrl == null && targetCtrl == null) {
                dict = sourceDict;
            } else if (sourceCtrl == null || targetCtrl == null) {
                KK_StudioCoordinateLoadOption.Logger.LogError($"CopyAllHairAcc input not correct!");
                return false;
            }
            if (null != dict || (null != GetExtendedDataFromExtData(sourceCtrl, out dict) && null != dict)) {
                for (int i = 0; i < dict.Count; i++) {
                    dict[dict.Keys.ElementAt(i)] = CopyHairInfo(dict.Values.ElementAt(i));
                }
                SaveExtendedDataToChaCtrl(targetCtrl, dict);
                targetCtrl.ChangeAccessory(true);
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"Copy all ({dict.Count}) HairAcc finish");
                return true;
            } else {
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"CopyAllHairAcc input not found!");
                return false;
            }
        }

        public static void GetControllerAndHairDict(ChaControl sourceChaCtrl, ChaControl targetChaCtrl = null) {
            CleanHairAccBackup();
            SourceHairAccCusController = sourceChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            TargetHairAccCusController = targetChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            if (null == SourceHairAccCusController || null == TargetHairAccCusController) {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("No Hair Accessory Customizer Controller found");
                return;
            }
            KK_StudioCoordinateLoadOption.Logger.LogDebug("Source-----");
            GetExtendedDataFromExtData(sourceChaCtrl, out sourceDict);
            KK_StudioCoordinateLoadOption.Logger.LogDebug("Target-----");
            GetExtendedDataFromExtData(targetChaCtrl, out targetDict);
            //KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Backup End");
        }

        public static void CopyHairAcc(ChaControl sourceChaCtrl, int sourceSlot, ChaControl targetChaCtrl, int targetSlot) {
            //KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Copy Start");
            GetControllerAndHairDict(sourceChaCtrl, targetChaCtrl);
            //KK_StudioCoordinateLoadOption.Logger.LogDebug("-----");

            if ((bool)SourceHairAccCusController.Invoke("IsHairAccessory", new object[] { sourceSlot })) {
                //ChaAccessoryComponent cusAcsCmp = Patches.GetChaAccessoryComponent(targetChaCtrl, targetSlot);
                //ChaCustomHairComponent chaCusHairCom = Patches.GetChaAccessoryComponent(sourceChaCtrl, sourceSlot).gameObject.GetComponent<ChaCustomHairComponent>();
                //if (null != chaCusHairCom && null != cusAcsCmp) {
                //    var c = cusAcsCmp.gameObject.GetComponent<ChaCustomHairComponent>();
                //    while (null != c) {
                //        c.transform.SetParent(null);
                //        UnityEngine.GameObject.DestroyImmediate(c);
                //        c = cusAcsCmp.gameObject.GetComponent<ChaCustomHairComponent>();
                //    }
                //    /*UnityEngine.Object.Instantiate*/(chaCusHairCom).transform.SetParent(cusAcsCmp.transform);
                //} else {
                //    KK_StudioCoordinateLoadOption.Logger.LogError($"-->Copy Hair Acc FAILED: Source ChaCustomHairComponent or Target ChaAccessoryComponent NOT FOUND.");
                //    KK_StudioCoordinateLoadOption.Logger.LogError($"-->Error Info: {sourceChaCtrl.fileParam.fullname} {sourceSlot} -> {targetChaCtrl.fileParam.fullname} {targetSlot}");
                //}

                //----------
                //targetChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType);  //從nowCoordinate到chaFile
                //targetChaCtrl.ChangeCoordinateTypeAndReload(false); //從chaFile讀出來給nowCoordinate

                //KK_StudioCoordinateLoadOption.Logger.LogDebug($"Init Target:{TargetHairAccCusController.Invoke("InitHairAccessoryInfo", new object[] { targetSlot })}");
                //TargetHairAccCusController.Invoke("InitHairAccessoryInfo", new object[] { targetSlot });
                //GetExtendedDataToDictionary(sourceChaCtrl, out sourceDict);
                //GetExtendedDataToDictionary(targetChaCtrl, out targetDict);

                if (null == sourceDict || !sourceDict.TryGetValue(sourceSlot, out var sourceHairInfo)) {
                    KK_StudioCoordinateLoadOption.Logger.LogError($"-->Copy Hair Acc FAILED: Source Accessory {sourceSlot} Extended Data NOT FOUND.");
                    KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Copy End");
                    return;
                }

                //Color color = new Color((float)v.GetProperty("r"), (float)v.GetProperty("g"),(float) v.GetProperty("b"),(float) v.GetProperty("a"));
                //KK_StudioCoordinateLoadOption.Logger.LogInfo(color);

                //if (!targetDict.TryGetValue(targetSlot, out var targetHairInfo)) {
                //    targetDict.Add(targetSlot, Activator.CreateInstance(HairAccessoryInfoType));
                //    //KK_StudioCoordinateLoadOption.Logger.LogError($"-->Copy Hair Acc FAILED: Target Accessory {targetSlot} Extended Data NOT FOUND.");
                //    //KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Copy End");
                //    //return;
                //}
                if (null == targetDict) {
                    targetDict = new Dictionary<int, object>();
                } else if (targetDict.ContainsKey(targetSlot)) {
                    targetDict.Remove(targetSlot);
                }
                //targetDict.Add(targetSlot, CopyHairInfo(sourceHairInfo));
                targetDict.Add(targetSlot, (sourceHairInfo));
                sourceDict.Remove(sourceSlot);

                //Dictionary<object, object> tmpDict = new Dictionary<object, object> {
                //    { "HairGloss",sourceHairInfo.ToDictionary<object, object>()["HairGloss"] },
                //    { "ColorMatch",sourceHairInfo.ToDictionary<object, object>()["ColorMatch"] },
                //    { "OutlineColor",sourceHairInfo.ToDictionary<object, object>()["OutlineColor"]},
                //    { "AccessoryColor",sourceHairInfo.ToDictionary<object, object>()["AccessoryColor"]},
                //    //{ "OutlineColor", (UnityEngine.Color) MessagePackSerializer.Deserialize<Color>(MessagePackSerializer.Serialize(v))},
                //    //{ "AccessoryColor",(UnityEngine.Color) MessagePackSerializer.Deserialize<Color>(MessagePackSerializer.Serialize(v))},
                //    { "HairLength", sourceHairInfo.ToDictionary<object, object>()["HairLength"] }
                //};
                //targetDict.Add(targetSlot, tmpDict);

                //KK_StudioCoordinateLoadOption.Logger.LogInfo(MessagePackSerializer.Deserialize<Color>(MessagePackSerializer.Serialize(sourceHairInfo.ToDictionary<object, object>()["AccessoryColor"])).ToString());
                //targetDict.Add(targetSlot, Activator.CreateInstance(HairAccessoryInfoType));
                //var targetHairInfo = targetDict[targetSlot];
                //targetHairInfo.SetField("HairGloss", (bool)sourceHairInfo.ToDictionary<object,object>()["HairGloss"], HairAccessoryInfoType);
                //targetHairInfo.SetField("ColorMatch", (bool)sourceHairInfo.GetField("ColorMatch", HairAccessoryInfoType), HairAccessoryInfoType);
                //targetHairInfo.SetField("OutlineColor", (Color)sourceHairInfo.GetField("OutlineColor", HairAccessoryInfoType), HairAccessoryInfoType);
                //targetHairInfo.SetField("AccessoryColor", (Color)sourceHairInfo.GetField("AccessoryColor", HairAccessoryInfoType), HairAccessoryInfoType);
                //targetHairInfo.SetField("HairLength", (float)sourceHairInfo.GetField("HairLength", HairAccessoryInfoType), HairAccessoryInfoType);

                SaveExtendedDataToChaCtrl(sourceChaCtrl, sourceDict);
                SaveExtendedDataToChaCtrl(targetChaCtrl, targetDict);
                //SaveExtendedDataToCoordinate(sourceChaCtrl.nowCoordinate, sourceDict);
                //SaveExtendedDataToCoordinate(targetChaCtrl.nowCoordinate, targetDict);
                //TargetHairAccCusController.Invoke("LoadData");

                //targetChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType);
                //targetChaCtrl.ChangeCoordinateTypeAndReload(false);
                //targetChaCtrl.Reload(false, true, false, true);

                //GetExtendedDataToDictionary(sourceChaCtrl, ref sourceDict);
                //GetExtendedDataToDictionary(targetChaCtrl, ref targetDict);

                //SourceHairAccCusController.Invoke("UpdateAccessory", new object[] { sourceSlot, false });
                //TargetHairAccCusController.Invoke("UpdateAccessory", new object[] { targetSlot, false });
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"-->Copy Hair Acc Finish: {sourceChaCtrl.fileParam.fullname} {sourceSlot} -> {targetChaCtrl.fileParam.fullname} {targetSlot}");
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"->Hair Acc: {targetDict.Count} : {string.Join(",", targetDict.Select(x => x.Key.ToString()).ToArray())}");
            } else {
                TargetHairAccCusController.Invoke("RemoveHairAccessoryInfo", new object[] { targetSlot });
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"-->Clear Hair Acc Finish: Slot {targetSlot}");
            }
            //KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Copy End");
        }

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
            //KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Clean HairAccBackup");
        }
    }
}