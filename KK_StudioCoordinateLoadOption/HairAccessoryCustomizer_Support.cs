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

        public static Dictionary<int, Dictionary<int, object>> GetExtendedDataToDictionary(ChaControl chaCtrl, out Dictionary<int, object> nowcoordinateExtData) {
            nowcoordinateExtData = null;
            //if (null == HairAccessoryInfoType) {
            //    var tmpHairAccCusController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            //    HairAccessoryInfoType = tmpHairAccCusController.GetType().GetNestedType("HairAccessoryInfo", HarmonyLib.AccessTools.all);
            //    if (null == HairAccessoryInfoType) {
            //        KK_StudioCoordinateLoadOption.Logger.LogDebug("No Hair Accessory Info Type found");
            //        return null;
            //    }
            //}

            PluginData data = ExtendedSave.GetExtendedDataById(chaCtrl.chaFile, GUID);
            if (data != null && data.data.TryGetValue("HairAccessories", out var loadedHairAccessories1) && loadedHairAccessories1 != null) {
                var result = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<int, object>>>((byte[])loadedHairAccessories1);
                result.TryGetValue(chaCtrl.fileStatus.coordinateType,out nowcoordinateExtData);
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"Get {chaCtrl.fileParam.fullname} Hair Accessories: {nowcoordinateExtData.Count}");
                return result;
            }
            return null;
        }

        public static void SetExtendedDataFromDictionary(ChaControl chaCtrl, Dictionary<int, object> dict) {
            var allExtData = GetExtendedDataToDictionary(chaCtrl,out _);
            allExtData[chaCtrl.fileStatus.coordinateType] = dict;
            PluginData data = new PluginData();
            data.data.Add("HairAccessories", MessagePackSerializer.Serialize(allExtData));
            ExtendedSave.SetExtendedDataById(chaCtrl.chaFile, GUID, data);
        }

        public static void CopyAllHairAcc(ChaControl sourceCtrl, ChaControl targetCtrl) {
            Dictionary<int, object> dict = new Dictionary<int, object>();
            if (null!=GetExtendedDataToDictionary(sourceCtrl, out dict)) {
                for(int i = 0; i < dict.Count; i++) {
                    dict[dict.Keys.ElementAt(i)] = CopyHairInfo(dict.Values.ElementAt(i));
                }
                //foreach (var kv in dict) {
                //    dict[kv.Key] = CopyHairInfo(kv.Value);
                //}
            }
            SetExtendedDataFromDictionary(targetCtrl, dict);
            KK_StudioCoordinateLoadOption.Logger.LogDebug($"Copy all ({dict.Count}) HairAcc");
        }

        public static void GetHairAccDict(ChaControl sourceChaCtrl, ChaControl targetChaCtrl) {
            CleanHairAccBackup();
            SourceHairAccCusController = sourceChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            TargetHairAccCusController = targetChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            if (null == SourceHairAccCusController || null == TargetHairAccCusController) {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("No Hair Accessory Customizer Controller found");
                return;
            }

            KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Backup Start");
            //GetExtendedDataToDictionary(sourceChaCtrl, ref sourceDict);
            //GetExtendedDataToDictionary(targetChaCtrl, ref targetDict);
            //KK_StudioCoordinateLoadOption.Logger.LogDebug("-----");
            //SourceHairAccCusController.Invoke("LoadCoordinateData", new object[] { sourceChaCtrl.nowCoordinate });
            //SourceHairAccCusController.Invoke("LoadData");
            //sourceChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType);
            //sourceChaCtrl.ChangeCoordinateTypeAndReload(false);


            //targetChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType);
            //targetChaCtrl.ChangeCoordinateTypeAndReload(false);
            //TargetHairAccCusController.Invoke("OnCoordinateBeingSaved", new object[] { targetChaCtrl.nowCoordinate });
            //TargetHairAccCusController.Invoke("OnCardBeingSaved", new object[] { 0 });

            KK_StudioCoordinateLoadOption.Logger.LogDebug("Source-----");
            GetExtendedDataToDictionary(sourceChaCtrl, out sourceDict);
            KK_StudioCoordinateLoadOption.Logger.LogDebug("Target-----");
            GetExtendedDataToDictionary(targetChaCtrl, out targetDict);
            KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Backup End");
        }

        public static void CopyHairAcc(ChaControl sourceChaCtrl, int sourceSlot, ChaControl targetChaCtrl, int targetSlot) {
            KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Copy Start");
            GetHairAccDict(sourceChaCtrl, targetChaCtrl);
            //KK_StudioCoordinateLoadOption.Logger.LogDebug("-----");

            if ((bool)SourceHairAccCusController.Invoke("IsHairAccessory", new object[] { sourceSlot })) {
                ChaAccessoryComponent cusAcsCmp = Patches.GetChaAccessoryComponent(targetChaCtrl, targetSlot);
                ChaCustomHairComponent chaCusHairCom = Patches.GetChaAccessoryComponent(sourceChaCtrl, sourceSlot).gameObject.GetComponent<ChaCustomHairComponent>();
                if (null != chaCusHairCom && null != cusAcsCmp) {
                    var c = cusAcsCmp.gameObject.GetComponent<ChaCustomHairComponent>();
                    while (null != c) {
                        c.transform.SetParent(null);
                        UnityEngine.GameObject.DestroyImmediate(c);
                        c = cusAcsCmp.gameObject.GetComponent<ChaCustomHairComponent>();
                    }
                    UnityEngine.Object.Instantiate(chaCusHairCom).transform.SetParent(cusAcsCmp.transform);
                } else {
                    KK_StudioCoordinateLoadOption.Logger.LogError($"-->Copy Hair Acc FAILED: Source ChaCustomHairComponent or Target ChaAccessoryComponent NOT FOUND.");
                    KK_StudioCoordinateLoadOption.Logger.LogError($"-->Error Info: {sourceChaCtrl.fileParam.fullname} {sourceSlot} -> {targetChaCtrl.fileParam.fullname} {targetSlot}");
                }

                //----------
                //targetChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType);  //從nowCoordinate到chaFile
                targetChaCtrl.ChangeCoordinateTypeAndReload(false); //從chaFile讀出來給nowCoordinate

                //KK_StudioCoordinateLoadOption.Logger.LogDebug($"Init Target:{TargetHairAccCusController.Invoke("InitHairAccessoryInfo", new object[] { targetSlot })}");
                //TargetHairAccCusController.Invoke("InitHairAccessoryInfo", new object[] { targetSlot });
                GetExtendedDataToDictionary(sourceChaCtrl, out sourceDict);
                GetExtendedDataToDictionary(targetChaCtrl, out targetDict);

                if (null== sourceDict || !sourceDict.TryGetValue(sourceSlot, out var sourceHairInfo)) {
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
                }else if (targetDict.ContainsKey(targetSlot)) {
                    targetDict.Remove(targetSlot);
                }
                targetDict.Add(targetSlot, CopyHairInfo(sourceHairInfo));

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

                SetExtendedDataFromDictionary(targetChaCtrl, targetDict);
                //TargetHairAccCusController.Invoke("LoadData");

                //targetChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType);
                //targetChaCtrl.ChangeCoordinateTypeAndReload(false);
                //targetChaCtrl.Reload(false, true, false, true);

                //GetExtendedDataToDictionary(sourceChaCtrl, ref sourceDict);
                //GetExtendedDataToDictionary(targetChaCtrl, ref targetDict);

                //SourceHairAccCusController.Invoke("UpdateAccessory", new object[] { sourceSlot, false });
                //TargetHairAccCusController.Invoke("UpdateAccessory", new object[] { targetSlot, false });
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"-->Copy Hair Acc Finish: {sourceChaCtrl.fileParam.fullname} {sourceSlot} -> {targetChaCtrl.fileParam.fullname} {targetSlot}");
            } else {
                TargetHairAccCusController.Invoke("RemoveHairAccessoryInfo", new object[] { targetSlot });
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"-->Clear Hair Acc Finish: Slot {targetSlot}");
            }
            KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Copy End");
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
            KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Clean HairAccBackup");
        }
    }
}