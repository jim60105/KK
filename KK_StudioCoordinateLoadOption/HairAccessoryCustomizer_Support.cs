using ExtensibleSaveFormat;
using Extension;
using MessagePack;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_StudioCoordinateLoadOption {
    class HairAccessoryCustomizer_Support {
            ///* 失敗方案，嘗試一項一項對應 *///
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

        public static void BackupHairAccDict(ChaControl sourceChaCtrl, ChaControl targetChaCtrl) {
            SourceHairAccCusController = sourceChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            TargetHairAccCusController = targetChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "HairAccessoryController"));
            if (null == SourceHairAccCusController || null == TargetHairAccCusController) {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("No Hair Accessory Customizer Controller found");
                return;
            }

            sourceDict.Clear();
            targetDict.Clear();
            //SourceHairAccCusController.Invoke("LoadData");
            SourceHairAccCusController.Invoke("LoadCoordinateData", new object[] { sourceChaCtrl.nowCoordinate });
            //SourceHairAccCusController.Invoke("OnCoordinateBeingSaved", new object[] { sourceChaCtrl.nowCoordinate });

            targetChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType);
            targetChaCtrl.Reload(false, true, false, true);
            TargetHairAccCusController.Invoke("OnCoordinateBeingSaved", new object[] { targetChaCtrl.nowCoordinate });

            PluginData data = ExtendedSave.GetExtendedDataById(sourceChaCtrl.nowCoordinate, GUID);
            //if (data != null) {
            //    KK_StudioCoordinateLoadOption.Logger.LogDebug($"Flag 1");
            //    if (data.data.TryGetValue("CoordinateHairAccessories", out var loadedHairAccessories1)) {
            //        KK_StudioCoordinateLoadOption.Logger.LogDebug($"Flag 2");
            //        if (loadedHairAccessories1 != null) {
            //            KK_StudioCoordinateLoadOption.Logger.LogDebug($"Flag 3");
            if (data != null && data.data.TryGetValue("CoordinateHairAccessories", out var loadedHairAccessories1) && loadedHairAccessories1 != null) {
                sourceDict = MessagePackSerializer.Deserialize<Dictionary<int, object>>((byte[])loadedHairAccessories1);
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"Hair Accessory Source Count: {sourceDict.Count}");
                //    }
                //}
            }
            PluginData data2 = ExtendedSave.GetExtendedDataById(targetChaCtrl.nowCoordinate, GUID);
            //if (data2 != null) {
            //    KK_StudioCoordinateLoadOption.Logger.LogDebug($"Flag 1");
            //    if (data2.data.TryGetValue("CoordinateHairAccessories", out var loadedHairAccessories2)) {
            //        KK_StudioCoordinateLoadOption.Logger.LogDebug($"Flag 2");
            //        if (loadedHairAccessories2 != null) {
            //            KK_StudioCoordinateLoadOption.Logger.LogDebug($"Flag 3");
            if (data2 != null && data2.data.TryGetValue("CoordinateHairAccessories", out var loadedHairAccessories2) && loadedHairAccessories2 != null) {
                targetDict = MessagePackSerializer.Deserialize<Dictionary<int, object>>((byte[])loadedHairAccessories2);
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"Hair Accessory Target Count: {targetDict.Count}");
                //    }
                //}
            }
        }

        public static void CopyHairAcc(ChaControl sourceChaCtrl, int sourceSlot, ChaControl targetChaCtrl, int targetSlot) {
            if ((bool)SourceHairAccCusController.Invoke("IsHairAccessory", new object[] { sourceSlot })) {
                //TargetHairAccCusController.Invoke("InitHairAccessoryInfo", new object[] { targetSlot });
                //try {
                if (targetDict.ContainsKey(targetSlot)) {
                    targetDict.Remove(targetSlot);
                }
                targetDict.Add(targetSlot, sourceDict[sourceSlot]);
                KK_StudioCoordinateLoadOption.Logger.LogDebug($">Flag 1: {targetDict.Count}");
                //} catch (Exception) { }

                PluginData data2 = new PluginData();
                if (targetDict.Count > 0) {
                    data2.data.Add("CoordinateHairAccessories", MessagePackSerializer.Serialize(targetDict));
                } else {
                    data2.data.Add("CoordinateHairAccessories", null);
                }
                ExtendedSave.SetExtendedDataById(targetChaCtrl.nowCoordinate, GUID, data2);

                TargetHairAccCusController.Invoke("LoadCoordinateData", new object[] { targetChaCtrl.nowCoordinate });
                TargetHairAccCusController.Invoke("OnCardBeingSaved", new object[] { 0 });
                //TargetHairAccCusController.Invoke("OnCoordinateBeingSaved", new object[] { targetChaCtrl.nowCoordinate });
                ////targetChaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType);
                targetChaCtrl.Reload(false, true, false, true);
                Patches.ReplaceHairAccessory(targetChaCtrl, targetSlot, Patches.GetChaAccessoryComponent(sourceChaCtrl, sourceSlot).gameObject.GetComponent<ChaCustomHairComponent>());
                TargetHairAccCusController.Invoke("UpdateAccessories", new object[] { true });

                //印出內容數量
                data2 = ExtendedSave.GetExtendedDataById(targetChaCtrl.nowCoordinate, GUID);
                if (data2 != null && data2.data.TryGetValue("CoordinateHairAccessories", out var loadedHairAccessories2) && loadedHairAccessories2 != null) {
                    targetDict = MessagePackSerializer.Deserialize<Dictionary<int, object>>((byte[])loadedHairAccessories2);
                    KK_StudioCoordinateLoadOption.Logger.LogDebug($">Hair Accessory Target Count: {targetDict.Count}");
                }

                KK_StudioCoordinateLoadOption.Logger.LogDebug($">Copy Hair Acc Finish: CoordinateCard Slot {sourceSlot} -> Chara Slot {targetSlot}");
            } else {
                TargetHairAccCusController.Invoke("InitHairAccessoryInfo", new object[] { targetSlot });
                KK_StudioCoordinateLoadOption.Logger.LogDebug($">Clear Hair Acc Finish: Slot {targetSlot}");
            }
        }

        public static void CleanHairAccBackup() {
            SourceHairAccCusController = null;
            TargetHairAccCusController = null;
            sourceDict.Clear();
            targetDict.Clear();
        }

        //public static void BackupKCOXData(ChaControl chaCtrl, ChaFileClothes clothes) {
        //    KCOXController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KoiClothesOverlayX"));
        //    if (null == KCOXController) {
        //        KK_StudioCoordinateLoadOption.Logger.LogDebug("No KCOX Controller found");
        //    } else {
        //        KCOXTexDataBackup = new Dictionary<string, object>();
        //        int cnt = 0;
        //        for (int i = 0; i < clothes.parts.Length; i++) {
        //            cnt += GetOverlay(Patches.MainClothesNames[i]) ? 1 : 0;
        //        }
        //        for (int j = 0; j < clothes.subPartsId.Length; j++) {
        //            cnt += GetOverlay(Patches.SubClothesNames[j]) ? 1 : 0;
        //        }
        //        foreach (var maskKind in MaskKind) {
        //            cnt += GetOverlay(maskKind) ? 1 : 0;
        //        }
        //        KK_StudioCoordinateLoadOption.Logger.LogDebug("Get Original Overlay/Mask Total: " + cnt);
        //    }
        //    return;
        //}

        //private static bool GetOverlay(string name) {
        //    KCOXTexDataBackup[name] = KCOXController.Invoke("GetOverlayTex", new object[] { name, false });

        //    if (null == KCOXTexDataBackup[name]) {
        //        //KK_StudioCoordinateLoadOption.Logger.LogDebug(name + " not found");
        //        return false;
        //    } else {
        //        KK_StudioCoordinateLoadOption.Logger.LogDebug("->Get Original Overlay/Mask: " + name);
        //        return true;
        //    }
        //}

        //public static void RollbackOverlay(bool main, int kind, ChaControl chaCtrl, string name = "texName") {
        //    if (string.Equals("texName", name)) {
        //        name = main ? Patches.MainClothesNames[kind] : Patches.SubClothesNames[kind];
        //    }

        //    if (null != KCOXController && null != KCOXTexDataBackup) {
        //        bool exist = KCOXTexDataBackup.TryGetValue(name, out var tex);

        //        //KCOXController.Invoke("SetOverlayTex", new object[] { tex, name });
        //        var coordinateType = (ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType;
        //        Dictionary<ChaFileDefine.CoordinateType, object> _allOverlayTextures = KCOXController.GetField("_allOverlayTextures").ToDictionary<ChaFileDefine.CoordinateType, object>();
        //        if (KCOXController.GetProperty("CurrentOverlayTextures").ToDictionary<string, object>().TryGetValue(name, out var clothesTexData)) {
        //            clothesTexData.Invoke("Clear");
        //            clothesTexData.SetField("Override", false);
        //            if (!exist || tex == null || (bool)tex.Invoke("IsEmpty")) {
        //                _allOverlayTextures[coordinateType].Invoke("Remove", new object[] { name });
        //                KK_StudioCoordinateLoadOption.Logger.LogDebug($"->Clear Overlay/Mask: {name}");
        //            } else {
        //                clothesTexData.SetProperty("Texture", tex.GetProperty("Texture"));
        //                if (null != tex.GetField("Override")) {
        //                    clothesTexData.SetField("Override", tex.GetField("Override"));
        //                }
        //                KK_StudioCoordinateLoadOption.Logger.LogDebug($"->Overlay/Mask Rollback: {name} (Replace)");
        //            }
        //        } else {
        //            if (exist && tex != null && !(bool)tex.Invoke("IsEmpty")) {
        //                _allOverlayTextures[coordinateType].Invoke("Add", new object[] { name, tex });
        //                KK_StudioCoordinateLoadOption.Logger.LogDebug($"->Overlay/Mask Rollback: {name} (Add)");
        //            }
        //        }
        //    }
        //}

        //public static void CleanKCOXBackup() {
        //    if (null != KCOXController) {
        //        KCOXController.Invoke("OnCardBeingSaved", new object[] { 1 });
        //    }
        //    KCOXTexDataBackup = null;
        //    return;
        //}
    }
}
