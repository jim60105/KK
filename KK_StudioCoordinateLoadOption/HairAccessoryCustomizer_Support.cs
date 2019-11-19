using ExtensibleSaveFormat;
using Extension;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KK_StudioCoordinateLoadOption {
    class HairAccessoryCustomizer_Support {
        private static object SourceHairAccCusController;
        private static object TargetHairAccCusController;
        private static readonly string GUID = "com.deathweasel.bepinex.hairaccessorycustomizer";
        public static Dictionary<int, object> sourceDict = new Dictionary<int, object>();
        public static Dictionary<int, object> targetDict = new Dictionary<int, object>();
        private static Type HairAccessoryInfoType = null;

        public static bool LoadAssembly() {
            string path = KK_StudioCoordinateLoadOption.TryGetPluginInstance(GUID, new Version(1, 1, 2))?.Info.Location;
            //HairAccessoryInfoType = Assembly.LoadFrom(path).GetType("KK_Plugins.HairAccessoryCustomizer.HairAccessoryController").GetNestedType("HairAccessoryInfo", BindingFlags.NonPublic);
            if (null != path) {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("Hair Accessory Customizer found");
                return true;
            } else {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("Load assembly FAILED: Hair Accessory Customizer");
                return false;
            }
        }

        public static bool GetExtendedDataToDictionary(ChaControl chaCtrl, ref Dictionary<int, object> dict) {
            PluginData data = ExtendedSave.GetExtendedDataById(chaCtrl.nowCoordinate, GUID);
            if (data != null && data.data.TryGetValue("CoordinateHairAccessories", out var loadedHairAccessories1) && loadedHairAccessories1 != null) {
                dict = MessagePackSerializer.Deserialize<Dictionary<int, object>>((byte[])loadedHairAccessories1);
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"Get {chaCtrl.fileParam.fullname} Hair Accessories: {dict.Count}");
                return true;
            }
            return false;
        }

        public static void SetExtendedDataFromDictionary(ChaControl chaCtrl, Dictionary<int, object> dict) {
            PluginData data = new PluginData();
            if (dict.Count > 0) {
                data.data.Add("CoordinateHairAccessories", MessagePackSerializer.Serialize(dict));
            } else {
                data.data.Add("CoordinateHairAccessories", null);
            }
            ExtendedSave.SetExtendedDataById(chaCtrl.nowCoordinate, GUID, data);
        }

        public static void CopyAllHairAcc(ChaControl sourceCtrl, ChaControl targetCtrl) {
            Dictionary<int, object> dict = new Dictionary<int, object>();
            GetExtendedDataToDictionary(sourceCtrl, ref dict);
            SetExtendedDataFromDictionary(targetCtrl, dict);
        }

        public static void GetHairAccDict(ChaControl sourceChaCtrl, ChaControl targetChaCtrl) {
            CleanHairAccBackup();
            SourceHairAccCusController = sourceChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            TargetHairAccCusController = targetChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            HairAccessoryInfoType = SourceHairAccCusController.GetType().GetNestedType("HairAccessoryInfo", BindingFlags.NonPublic);
            if (null == SourceHairAccCusController || null == TargetHairAccCusController) {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("No Hair Accessory Customizer Controller found");
                return;
            }
            if (null == HairAccessoryInfoType) {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("No Hair Accessory Info Type found");
                return;
            }

            KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Backup Start");
            GetExtendedDataToDictionary(sourceChaCtrl, ref sourceDict);
            GetExtendedDataToDictionary(targetChaCtrl, ref targetDict);
            KK_StudioCoordinateLoadOption.Logger.LogDebug("-----");
            SourceHairAccCusController.Invoke("LoadCoordinateData", new object[] { sourceChaCtrl.nowCoordinate });
            SourceHairAccCusController.Invoke("LoadData");

            targetChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType);
            targetChaCtrl.ChangeCoordinateTypeAndReload(false);
            TargetHairAccCusController.Invoke("OnCoordinateBeingSaved", new object[] { targetChaCtrl.nowCoordinate });
            TargetHairAccCusController.Invoke("OnCardBeingSaved", new object[] { 0 });

            GetExtendedDataToDictionary(sourceChaCtrl, ref sourceDict);
            GetExtendedDataToDictionary(targetChaCtrl, ref targetDict);
            KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Backup End");
        }

        public static void CopyHairAcc(ChaControl sourceChaCtrl, int sourceSlot, ChaControl targetChaCtrl, int targetSlot) {
            KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Copy Start");
            GetHairAccDict(sourceChaCtrl, targetChaCtrl);
            //KK_StudioCoordinateLoadOption.Logger.LogDebug("-----");

            TargetHairAccCusController.Invoke("InitHairAccessoryInfo", new object[] { targetSlot });
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
                targetChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType);
                targetChaCtrl.ChangeCoordinateTypeAndReload(false);

                //TODO
                //應該是這部分有問題，tmp需要深拷貝
                //TargetHairAccCusController.Invoke("InitHairAccessoryInfo", new object[] { targetSlot });
                GetExtendedDataToDictionary(sourceChaCtrl, ref sourceDict);
                GetExtendedDataToDictionary(targetChaCtrl, ref targetDict);

                if (!sourceDict.TryGetValue(sourceSlot, out var sourceHairInfo)) {
                    KK_StudioCoordinateLoadOption.Logger.LogError($"-->Copy Hair Acc FAILED: Source Accessory {sourceSlot} Extended Data NOT FOUND.");
                    KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Copy End");
                    return;
                }
                //if (!targetDict.TryGetValue(targetSlot, out var targetHairInfo)) {
                //    targetDict.Add(targetSlot, Activator.CreateInstance(HairAccessoryInfoType));
                //    //KK_StudioCoordinateLoadOption.Logger.LogError($"-->Copy Hair Acc FAILED: Target Accessory {targetSlot} Extended Data NOT FOUND.");
                //    //KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Copy End");
                //    //return;
                //}
                if (targetDict.ContainsKey(targetSlot)) {
                    targetDict.Remove(targetSlot);
                }
                targetDict.Add(targetSlot, Activator.CreateInstance(HairAccessoryInfoType));
                var targetHairInfo = targetDict[targetSlot];
                targetHairInfo.SetField("HairGloss",(bool) sourceHairInfo.GetField("HairGloss",HairAccessoryInfoType),HairAccessoryInfoType);
                targetHairInfo.SetField("ColorMatch",(bool) sourceHairInfo.GetField("ColorMatch",HairAccessoryInfoType),HairAccessoryInfoType);
                targetHairInfo.SetField("OutlineColor",(Color) sourceHairInfo.GetField("OutlineColor",HairAccessoryInfoType),HairAccessoryInfoType);
                targetHairInfo.SetField("AccessoryColor",(Color) sourceHairInfo.GetField("AccessoryColor",HairAccessoryInfoType),HairAccessoryInfoType);
                targetHairInfo.SetField("HairLength",(float) sourceHairInfo.GetField("HairLength",HairAccessoryInfoType),HairAccessoryInfoType);

                SetExtendedDataFromDictionary(targetChaCtrl, targetDict);

                targetChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType);
                targetChaCtrl.ChangeCoordinateTypeAndReload(false);
                //targetChaCtrl.Reload(false, true, false, true);

                //GetExtendedDataToDictionary(sourceChaCtrl, ref sourceDict);
                //GetExtendedDataToDictionary(targetChaCtrl, ref targetDict);

                SourceHairAccCusController.Invoke("UpdateAccessory", new object[] { sourceSlot, false });
                TargetHairAccCusController.Invoke("UpdateAccessory", new object[] { targetSlot, false });
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"-->Copy Hair Acc Finish: {sourceChaCtrl.fileParam.fullname} {sourceSlot} -> {targetChaCtrl.fileParam.fullname} {targetSlot}");
            } else {
                TargetHairAccCusController.Invoke("RemoveHairAccessoryInfo", new object[] { targetSlot });
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"-->Clear Hair Acc Finish: Slot {targetSlot}");
            }
            KK_StudioCoordinateLoadOption.Logger.LogDebug("-->Copy End");
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
