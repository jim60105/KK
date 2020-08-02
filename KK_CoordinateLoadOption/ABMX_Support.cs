using Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KK_CoordinateLoadOption {
    class ABMX_Support {
        private static readonly BepInEx.Logging.ManualLogSource Logger = KK_CoordinateLoadOption.Logger;
        private static Type BoneModifierType = null;
        private static ChaControl sourceChaCtrl;
        private static ChaControl targetChaCtrl;
        private static MonoBehaviour TargetAMBXController;
        private static Dictionary<string, object> SourceABMXBackup = null;

        public static bool LoadAssembly() {
            try {
                string path = Extension.Extension.TryGetPluginInstance("KKABMX.Core", new Version(3, 3))?.Info.Location;
                if (File.Exists(path)) {
                    BoneModifierType = Assembly.LoadFrom(path).GetType("KKABMX.Core.BoneModifier");
                } else {
                    throw new Exception("Load assembly FAILED: KKABMX");
                }
                Logger.LogDebug("KKABMX found");
                return true;
            } catch (Exception ex) {
                Logger.LogDebug(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Copy前準備Source和Target資料
        /// </summary>
        /// <param name="sourceChaCtrl">來源ChaControl</param>
        /// <param name="targetChaCtrl">目標ChaControl</param>
        public static bool GetControllerAndBackupData(ChaControl sourceChaCtrl = null, ChaControl targetChaCtrl = null) {
            if (null != sourceChaCtrl) {
                Logger.LogDebug("Source ABMX-----");
                ABMX_Support.sourceChaCtrl = sourceChaCtrl;
                MonoBehaviour SourceABMXController = GetExtendedDataFromController(sourceChaCtrl, out SourceABMXBackup);
                if (null == SourceABMXController) {
                    Logger.LogDebug($"No Source ABMX Controller found on {sourceChaCtrl.fileParam.fullname}");
                    return false;
                }
            }

            if (null != targetChaCtrl) {
                Logger.LogDebug("Target ABMX-----");
                ABMX_Support.targetChaCtrl = targetChaCtrl;
                TargetAMBXController = GetExtendedDataFromController(targetChaCtrl, out _);
                if (null == TargetAMBXController) {
                    Logger.LogDebug($"No Target ABMX Controller found on {targetChaCtrl.fileParam.fullname}");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 由ChaControl Controller取得ExtData
        /// </summary>
        /// <param name="chaCtrl">對象ChaControl</param>
        /// <param name="dict">Output ABMX Data Backup</param>
        /// <returns>ABMX Controller</returns>
        public static MonoBehaviour GetExtendedDataFromController(ChaControl chaCtrl, out Dictionary<string, object> dict) {
            dict = new Dictionary<string, object>();
            MonoBehaviour controller = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KKABMX.Core"));
            if (null == controller) {
                Logger.LogDebug("No ABMX BoneController found");
                return null;
            }

            //Logger.LogDebug("BoneController Get");
            List<object> Modifiers = new List<object>();
            foreach (string boneName in (IEnumerable<string>)controller.Invoke("GetAllPossibleBoneNames")) {
                object tmpModifier = controller.Invoke("GetModifier", new object[] { boneName });
                if (null != tmpModifier) {
                    Modifiers.Add(tmpModifier);
                }
            }
            dict = Modifiers
                .Where(x => (bool)x.Invoke("IsCoordinateSpecific"))
                .Where(x => !(bool)x.Invoke("IsEmpty"))
                .ToDictionary(
                    x => (string)x.GetProperty("BoneName"), //x.BoneName,
                    x => {
                        Logger.LogDebug("Get original ABMX BoneData: " + (string)x.GetProperty("BoneName"));
                        object y = x.Invoke("GetModifier", new object[] { (ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType });
                        return y.Invoke("Clone");
                    }
                );

            if (dict == null || dict.Count == 0) {
                Logger.LogDebug("No original ABMX BoneData.");
            } else {
                Logger.LogDebug("Get original ABMX BoneData: " + dict.Count);
            }
            return controller;
        }

        /// <summary>
        /// 拷貝ABMX資料
        /// </summary>
        /// <param name="sourceChaCtrl"></param>
        /// <param name="targetChaCtrl"></param>
        public static void CopyABMXData(ChaControl sourceChaCtrl, ChaControl targetChaCtrl) {
            if (sourceChaCtrl != ABMX_Support.sourceChaCtrl || targetChaCtrl != ABMX_Support.targetChaCtrl) {
                GetControllerAndBackupData(sourceChaCtrl, targetChaCtrl);
            }

            // Clear previous data for this coordinate from coord specific modifiers
            List<object> Modifiers = new List<object>();
            foreach (string boneName in (IEnumerable<string>)TargetAMBXController.Invoke("GetAllPossibleBoneNames")) {
                object tmpModifier = TargetAMBXController.Invoke("GetModifier", new object[] { boneName });
                if (null != tmpModifier) {
                    Modifiers.Add(tmpModifier);
                }
            }
            //Logger.LogDebug("Get Modifiers by AllBoneName");
            foreach (object modifier in Modifiers.Where(x => (bool)x.Invoke("IsCoordinateSpecific"))) {
                object y = modifier.Invoke("GetModifier", new object[] { (ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType });
                y.Invoke("Clear");
            }
            //Logger.LogDebug("Clear all Modifiers");

            if (null != TargetAMBXController && null != SourceABMXBackup) {
                foreach (KeyValuePair<string, object> modifierDict in SourceABMXBackup) {
                    object target = TargetAMBXController.Invoke("GetModifier", new object[] { modifierDict.Key });
                    if (target == null) {
                        // Add any missing modifiers
                        target = Activator.CreateInstance(BoneModifierType, new object[] { modifierDict.Key });
                        Logger.LogDebug("->Create target");
                        TargetAMBXController.Invoke("AddModifier", new object[] { target });
                    }
                    target.Invoke("MakeCoordinateSpecific");
                    object[] tmp = (object[])target.GetProperty("CoordinateModifiers");
                    tmp[targetChaCtrl.fileStatus.coordinateType] = modifierDict.Value;
                    target.SetProperty("CoordinateModifiers", tmp);
                    Logger.LogDebug("->Insert Modifier: " + modifierDict.Key);
                }
                TargetAMBXController.Invoke("StartCoroutine", new object[] {
                    TargetAMBXController.Invoke("OnDataChangedCo")
                }); //StartCoroutine(OnDataChangedCo());
                Logger.LogDebug("->ABMX Bone Rollback complete");
            } else {
                Logger.LogDebug("->ABMX Bone not found");
            }
        }

        /// <summary>
        /// 將Controller內之ABMX Data儲存至ChaControl ExtendedData內
        /// </summary>
        /// <param name="chaCtrl">對象ChaControl</param>
        public static void SetExtDataFromController(ChaControl chaCtrl) {
            MonoBehaviour BoneController = GetExtendedDataFromController(chaCtrl, out _);
            BoneController.Invoke("OnCardBeingSaved", new object[] { 1 });
        }

        public static void ClearABMXBackup() {
            sourceChaCtrl = null;
            targetChaCtrl = null;
            SourceABMXBackup = null;
            TargetAMBXController = null;
        }
    }
}
