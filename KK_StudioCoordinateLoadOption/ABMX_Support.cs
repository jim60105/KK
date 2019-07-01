﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using Extension;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_StudioCoordinateLoadOption {
    class ABMX_Support {
        private static Dictionary<string, object> ABMXDataBackup = null;
        private static object BoneController = null;
        private static Type BoneModifierType = null;

        public static bool LoadAssembly() {
            try {
                if (File.Exists("BepInEx/KKABMPlugin.dll")) {
                    BoneModifierType = Assembly.LoadFrom("BepInEx/KKABMPlugin.dll").GetType("KKABMX.Core.BoneModifier");
                } else if (File.Exists("BepInEx/KKABMX.dll")) {
                    BoneModifierType = Assembly.LoadFrom("BepInEx/KKABMX.dll").GetType("KKABMX.Core.BoneModifier");
                } else {
                    throw new Exception("Load assembly FAILED: KKABMX");
                }
                Logger.Log(LogLevel.Debug, "[KK_SCLO] KKABMX found");
                return true;
            } catch (Exception ex) {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] " + ex.Message);
                return false;
            }
        }

        public static void BackupABMXData(ChaControl chaCtrl) {
            BoneController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KKABMX.Core"));
            if (null == BoneController) {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] No ABMX BoneController found");
                return;
            }

            //Logger.Log(LogLevel.Debug, "[KK_SCLO] BoneController Get");
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
                        Logger.Log(LogLevel.Debug, "[KK_SCLO] Get original ABMX BoneData: " + (string)x.GetProperty("BoneName"));
                        var y = x.Invoke("GetModifier", new object[] { (ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType });
                        return y.Invoke("Clone");
                    }
                );

            //Logger.Log(LogLevel.Debug, "[KK_SCLO] toSave Get");
            if (toSave == null || toSave.Count == 0) {
                ABMXDataBackup = null;
                Logger.Log(LogLevel.Debug, "[KK_SCLO] No original ABMX BoneData.");
            } else {
                ABMXDataBackup = toSave;
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Get original ABMX BoneData: " + toSave.Count);
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
            //Logger.Log(LogLevel.Debug, "[KK_SCLO] Get Modifiers by AllBoneName");
            foreach (var modifier in Modifiers.Where(x => (bool)x.Invoke("IsCoordinateSpecific"))) {
                var y = modifier.Invoke("GetModifier", new object[] { (ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType });
                y.Invoke("Clear");
            }
            //Logger.Log(LogLevel.Debug, "[KK_SCLO] Clear all Modifiers");

            if (null != BoneController && null != ABMXDataBackup) {
                foreach (var modifierDict in ABMXDataBackup) {
                    var target = BoneController.Invoke("GetModifier", new object[] { modifierDict.Key });
                    if (target == null) {
                        // Add any missing modifiers
                        target = Activator.CreateInstance(BoneModifierType, new object[] { modifierDict.Key });
                        Logger.Log(LogLevel.Debug, "[KK_SCLO] ->Create target");
                        BoneController.Invoke("AddModifier", new object[] { target });
                    }
                    target.Invoke("MakeCoordinateSpecific");
                    var tmp = (object[])target.GetProperty("CoordinateModifiers");
                    tmp[chaCtrl.fileStatus.coordinateType] = modifierDict.Value;
                    target.SetProperty("CoordinateModifiers",tmp);
                    Logger.Log(LogLevel.Debug, "[KK_SCLO] ->Insert Modifier: " + modifierDict.Key);
                }
                BoneController.Invoke("StartCoroutine", new object[] {
                    BoneController.Invoke("OnDataChangedCo")
                }); //StartCoroutine(OnDataChangedCo());
                Logger.Log(LogLevel.Debug, "[KK_SCLO] ->ABMX Bone Rollback complete");
                ABMXDataBackup = null;
                return;
            }
            Logger.Log(LogLevel.Debug, "[KK_SCLO] ->ABMX Bone not found");
            ABMXDataBackup = null;
            return;
        }
    }
}
