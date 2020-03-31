using Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KK_StudioCoordinateLoadOption {
    class ABMX_Support {
        private static readonly BepInEx.Logging.ManualLogSource Logger = KK_StudioCoordinateLoadOption.Logger;
        private static Dictionary<string, object> ABMXDataBackup = null;
        private static object BoneController = null;
        private static Type BoneModifierType = null;

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

        public static void BackupABMXData(ChaControl chaCtrl) {
            BoneController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KKABMX.Core"));
            if (null == BoneController) {
                Logger.LogDebug("No ABMX BoneController found");
                return;
            }

            //Logger.LogDebug("BoneController Get");
            List<object> Modifiers = new List<object>();
            foreach (string boneName in (IEnumerable<string>)BoneController.Invoke("GetAllPossibleBoneNames")) {
                var tmpModifier = BoneController.Invoke("GetModifier", new object[] { boneName });
                if (null != tmpModifier) {
                    Modifiers.Add(tmpModifier);
                }
            }
            var toSave = Modifiers
                .Where(x => (bool)x.Invoke("IsCoordinateSpecific"))
                .Where(x => !(bool)x.Invoke("IsEmpty"))
                .ToDictionary(
                    x => (string)x.GetProperty("BoneName"), //x.BoneName,
                    x => {
                        Logger.LogDebug("Get original ABMX BoneData: " + (string)x.GetProperty("BoneName"));
                        var y = x.Invoke("GetModifier", new object[] { (ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType });
                        return y.Invoke("Clone");
                    }
                );

            //Logger.LogDebug("toSave Get");
            if (toSave == null || toSave.Count == 0) {
                ABMXDataBackup = null;
                Logger.LogDebug("No original ABMX BoneData.");
            } else {
                ABMXDataBackup = toSave;
                Logger.LogDebug("Get original ABMX BoneData: " + toSave.Count);
            }
            return;
        }

        public static void RollbackABMXBone(ChaControl chaCtrl) {
            // Clear previous data for this coordinate from coord specific modifiers
            List<object> Modifiers = new List<object>();
            foreach (string boneName in (IEnumerable<string>)BoneController.Invoke("GetAllPossibleBoneNames")) {
                var tmpModifier = BoneController.Invoke("GetModifier", new object[] { boneName });
                if (null != tmpModifier) {
                    Modifiers.Add(tmpModifier);
                }
            }
            //Logger.LogDebug("Get Modifiers by AllBoneName");
            foreach (var modifier in Modifiers.Where(x => (bool)x.Invoke("IsCoordinateSpecific"))) {
                var y = modifier.Invoke("GetModifier", new object[] { (ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType });
                y.Invoke("Clear");
            }
            //Logger.LogDebug("Clear all Modifiers");

            if (null != BoneController && null != ABMXDataBackup) {
                foreach (var modifierDict in ABMXDataBackup) {
                    var target = BoneController.Invoke("GetModifier", new object[] { modifierDict.Key });
                    if (target == null) {
                        // Add any missing modifiers
                        target = Activator.CreateInstance(BoneModifierType, new object[] { modifierDict.Key });
                        Logger.LogDebug("->Create target");
                        BoneController.Invoke("AddModifier", new object[] { target });
                    }
                    target.Invoke("MakeCoordinateSpecific");
                    var tmp = (object[])target.GetProperty("CoordinateModifiers");
                    tmp[chaCtrl.fileStatus.coordinateType] = modifierDict.Value;
                    target.SetProperty("CoordinateModifiers", tmp);
                    Logger.LogDebug("->Insert Modifier: " + modifierDict.Key);
                }
                BoneController.Invoke("StartCoroutine", new object[] {
                    BoneController.Invoke("OnDataChangedCo")
                }); //StartCoroutine(OnDataChangedCo());
                Logger.LogDebug("->ABMX Bone Rollback complete");
                ABMXDataBackup = null;
                return;
            }
            Logger.LogDebug("->ABMX Bone not found");
            ABMXDataBackup = null;
            return;
        }
    }
}
