using ExtensibleSaveFormat;
using Extension;
using MessagePack;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_StudioCoordinateLoadOption {
    class HairAccessoryCustomizer_Support {
        private static object SourceHairAccCusController;
        private static object TargetHairAccCusController;
        private static readonly string GUID = "com.deathweasel.bepinex.hairaccessorycustomizer";
        private static Dictionary<int, object> sourceDict = new Dictionary<int, object>();
        private static Dictionary<int, object> targetDict = new Dictionary<int, object>();

        public static bool LoadAssembly() {
            if (null != KK_StudioCoordinateLoadOption.TryGetPluginInstance(GUID, new System.Version(1, 1, 2))) {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("Hair Accessory Customizer found");
                return true;
            } else {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("Load assembly FAILED: Hair Accessory Customizer");
                return false;
            }
        }

        public static void GetHairAccDict(ChaControl sourceChaCtrl, ChaControl targetChaCtrl) {
            CleanHairAccBackup();
            SourceHairAccCusController = sourceChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            TargetHairAccCusController = targetChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            if (null == SourceHairAccCusController || null == TargetHairAccCusController) {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("No Hair Accessory Customizer Controller found");
                return;
            }

            KK_StudioCoordinateLoadOption.Logger.LogDebug("*Backup Start");
            GetExtendedDataToDictionary(sourceChaCtrl, ref sourceDict);
            GetExtendedDataToDictionary(targetChaCtrl, ref targetDict);
            SourceHairAccCusController.Invoke("LoadCoordinateData", new object[] { sourceChaCtrl.nowCoordinate });
            SourceHairAccCusController.Invoke("LoadData");

            //targetChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType);
            targetChaCtrl.Reload(false, true, false, true);
            TargetHairAccCusController.Invoke("OnCoordinateBeingSaved", new object[] { targetChaCtrl.nowCoordinate });
            TargetHairAccCusController.Invoke("OnCardBeingSaved", new object[] { 0 });

            KK_StudioCoordinateLoadOption.Logger.LogDebug("*---");
            GetExtendedDataToDictionary(sourceChaCtrl, ref sourceDict);
            GetExtendedDataToDictionary(targetChaCtrl, ref targetDict);
            KK_StudioCoordinateLoadOption.Logger.LogDebug("*Backup End");
        }

        public static void GetExtendedDataToDictionary(ChaControl chaCtrl, ref Dictionary<int, object> dict) {
            PluginData data = ExtendedSave.GetExtendedDataById(chaCtrl.nowCoordinate, GUID);
            if (data != null && data.data.TryGetValue("CoordinateHairAccessories", out var loadedHairAccessories1) && loadedHairAccessories1 != null) {
                dict = MessagePackSerializer.Deserialize<Dictionary<int, object>>((byte[])loadedHairAccessories1);
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"Get {chaCtrl.fileParam.fullname} Hair Accessories: {dict.Count}");
            }
        }

        //public static void CopyHairAcc(ChaControl sourceCtrl, ChaControl targetCtrl) {
        //    int accAmount = 20;
        //    if (KK_StudioCoordinateLoadOption._isMoreAccessoriesExist) {
        //        accAmount = MoreAccessories_Support.GetAccessoriesAmount(sourceCtrl.chaFile);
        //    }
        //    sourceCtrl.ChangeCoordinateTypeAndReload(false);
        //    targetCtrl.ChangeCoordinateTypeAndReload(false);
        //    for (int i = 0; i < accAmount; i++) {
        //        CopyHairAcc(sourceCtrl, i, targetCtrl, i);
        //    }
        //}

        public static void CopyHairAcc(ChaControl sourceChaCtrl, int sourceSlot, ChaControl targetChaCtrl, int targetSlot) {
            KK_StudioCoordinateLoadOption.Logger.LogDebug("*Copy Start");
            if (null == SourceHairAccCusController || null == TargetHairAccCusController) {
                GetHairAccDict(sourceChaCtrl, targetChaCtrl);
            }
            GetExtendedDataToDictionary(sourceChaCtrl, ref sourceDict);
            GetExtendedDataToDictionary(targetChaCtrl, ref targetDict);

            TargetHairAccCusController.Invoke("InitHairAccessoryInfo", new object[] { targetSlot });
            if ((bool)SourceHairAccCusController.Invoke("IsHairAccessory", new object[] { sourceSlot })) {
                
                if(!sourceDict.TryGetValue(sourceSlot, out var tmp)) {
                    KK_StudioCoordinateLoadOption.Logger.LogError($">Copy Hair Acc FAILED: Source Accessory {sourceSlot} Extended Data NOT FOUND.");
                    KK_StudioCoordinateLoadOption.Logger.LogDebug("*Copy End");
                    return;
                }
                if (targetDict.ContainsKey(targetSlot)) {
                    targetDict.Remove(targetSlot);
                }
                targetDict.Add(targetSlot, tmp);

                PluginData data2 = new PluginData();
                if (targetDict.Count > 0) {
                    data2.data.Add("CoordinateHairAccessories", MessagePackSerializer.Serialize(targetDict));
                } else {
                    data2.data.Add("CoordinateHairAccessories", null);
                }
                ExtendedSave.SetExtendedDataById(targetChaCtrl.nowCoordinate, GUID, data2);

                TargetHairAccCusController.Invoke("LoadCoordinateData", new object[] { targetChaCtrl.nowCoordinate });
                TargetHairAccCusController.Invoke("OnCardBeingSaved", new object[] { 0 });

                targetChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType);
                targetChaCtrl.Reload(false, true, false, true);

                ChaAccessoryComponent cusAcsCmp = Patches.GetChaAccessoryComponent(targetChaCtrl, targetSlot);
                ChaCustomHairComponent chaCusHairCom = Patches.GetChaAccessoryComponent(sourceChaCtrl, sourceSlot).gameObject.GetComponent<ChaCustomHairComponent>();
                if (null != chaCusHairCom  && null!= cusAcsCmp) {
                    UnityEngine.Object.Instantiate(chaCusHairCom).transform.SetParent(cusAcsCmp.transform);
                } else {
                    KK_StudioCoordinateLoadOption.Logger.LogError($">Copy Hair Acc FAILED: Source ChaCustomHairComponent or Target ChaAccessoryComponent NOT FOUND.");
                    KK_StudioCoordinateLoadOption.Logger.LogError($">Error Info: {sourceChaCtrl.fileParam.fullname} {sourceSlot} -> {targetChaCtrl.fileParam.fullname} {targetSlot}");
                }

                targetChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType);
                targetChaCtrl.ChangeCoordinateTypeAndReload(false);

                GetExtendedDataToDictionary(sourceChaCtrl, ref sourceDict);
                GetExtendedDataToDictionary(targetChaCtrl, ref targetDict);

                KK_StudioCoordinateLoadOption.Logger.LogDebug($">Copy Hair Acc Finish: {sourceChaCtrl.fileParam.fullname} {sourceSlot} -> {targetChaCtrl.fileParam.fullname} {targetSlot}");
            } else {
                TargetHairAccCusController.Invoke("RemoveHairAccessoryInfo", new object[] { targetSlot });
                KK_StudioCoordinateLoadOption.Logger.LogDebug($">Clear Hair Acc Finish: Slot {targetSlot}");
            }
            KK_StudioCoordinateLoadOption.Logger.LogDebug("*Copy End");
        }

        public static void CleanHairAccBackup() {
            SourceHairAccCusController = null;
            TargetHairAccCusController = null;
            sourceDict.Clear();
            targetDict.Clear();
            KK_StudioCoordinateLoadOption.Logger.LogDebug("*Clean HairAccBackup");
        }
    }
}
