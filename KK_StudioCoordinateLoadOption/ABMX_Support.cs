using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_StudioCoordinateLoadOption
{
    class ABMX_Support
    {
        private static readonly BindingFlags publicFlag = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance;
        private static readonly BindingFlags nonPublicFlag = BindingFlags.InvokeMethod | BindingFlags.NonPublic| BindingFlags.Instance;
        private static Dictionary<string, object> ABMXDataBackup = null;
        private static object BoneController = null;
        private static Type BoneModifierType = null;

        public static bool LoadAssembly()
        {
            try
            {
                if (File.Exists("BepInEx/KKABMPlugin.dll"))
                {
                    BoneModifierType = Assembly.LoadFrom("BepInEx/KKABMPlugin.dll").GetType("KKABMX.Core.BoneModifier");
                }
                else if(File.Exists("BepInEx/KKABMX.dll"))
                {
                    BoneModifierType = Assembly.LoadFrom("BepInEx/KKABMX.dll").GetType("KKABMX.Core.BoneModifier");
                }
                else  
                {
                    throw new Exception("Load assembly FAILED: KKABMX");
                }
                Logger.Log(LogLevel.Debug, "[KK_SCLO] KKABMX found");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] "+ex.Message);
                return false;
            }
        }
        public static void BackupABMXData(ChaControl chaCtrl)
        {
            BoneController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KKABMX.Core"));
            if (null == BoneController)
            {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] No ABMX BoneController found");
                return;
            }

            //Logger.Log(LogLevel.Debug, "[KK_SCLO] BoneController Get");
            BoneController.GetType().InvokeMember("ModifiersPurgeEmpty", nonPublicFlag, null, BoneController, null);
            List<object> Modifiers = new List<object>();
            foreach (string boneName in (IEnumerable<string>)BoneController.GetType().InvokeMember("GetAllPossibleBoneNames", publicFlag, null, BoneController, null))
            {
                var tmpModifier = BoneController.GetType().InvokeMember("GetModifier", publicFlag, null, BoneController, new object[] { boneName });
                if (null != tmpModifier)
                {
                    Modifiers.Add(tmpModifier);
                }
            }
            var toSave = Modifiers
                .Where(x => (bool)x.GetType().InvokeMember("IsCoordinateSpecific", publicFlag, null, x, null))
                .ToDictionary(
                    x => (string)x.GetType().InvokeMember("BoneName", BindingFlags.GetProperty, null, x, null), //x.BoneName,
                    x =>
                    {
                        Logger.Log(LogLevel.Debug, "[KK_SCLO] Get original ABMX BoneData: " + (string)x.GetType().InvokeMember("BoneName", BindingFlags.GetProperty, null, x, null));
                        var y = x.GetType().InvokeMember("GetModifier", publicFlag, null, x, new object[] { (ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType });
                        return y.GetType().InvokeMember("Clone", publicFlag, null, y, null);
                    }
                );

            //Logger.Log(LogLevel.Debug, "[KK_SCLO] toSave Get");
            if (toSave == null || toSave.Count == 0)
            {
                ABMXDataBackup = null;
                Logger.Log(LogLevel.Debug, "[KK_SCLO] No original ABMX BoneData.");
            }
            else
            {
                ABMXDataBackup = toSave;
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Get original ABMX BoneData: " + toSave.Count);
            }
            return;
        }

        public static void RollbackABMXBone(ChaControl chaCtrl)
        {
            // Clear previous data for this coordinate from coord specific modifiers
            List<object> Modifiers = new List<object>();
            foreach (string boneName in (IEnumerable<string>)BoneController.GetType().InvokeMember("GetAllPossibleBoneNames", publicFlag, null, BoneController, null))
            {
                var tmpModifier = BoneController.GetType().InvokeMember("GetModifier", publicFlag, null, BoneController, new object[] { boneName });
                if (null != tmpModifier)
                {
                    Modifiers.Add(tmpModifier);
                }
            }
            //Logger.Log(LogLevel.Debug, "[KK_SCLO] Get Modifiers by AllBoneName");
            foreach (var modifier in Modifiers.Where(x => (bool)x.GetType().InvokeMember("IsCoordinateSpecific", publicFlag, null, x, null)))
            {
                var y = modifier.GetType().InvokeMember("GetModifier", publicFlag, null, modifier, new object[] { (ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType });
                y.GetType().InvokeMember("Clear", publicFlag, null, y, null);
            }
            //Logger.Log(LogLevel.Debug, "[KK_SCLO] Clear all Modifiers");

            if (null != BoneController && null != ABMXDataBackup)
            {
                foreach (var modifierDict in ABMXDataBackup)
                {
                    var target = BoneController.GetType().InvokeMember("GetModifier", publicFlag, null, BoneController, new object[] { modifierDict.Key });
                    if (target == null)
                    {
                        // Add any missing modifiers
                        target = Activator.CreateInstance(BoneModifierType, new object[] { modifierDict.Key });
                        Logger.Log(LogLevel.Debug, "[KK_SCLO] ->Create target");
                        BoneController.GetType().InvokeMember("AddModifier", publicFlag, null, BoneController, new object[] { target });
                    }
                    target.GetType().InvokeMember("MakeCoordinateSpecific", publicFlag, null, target, null);
                    var tmp = (object[])target.GetType().InvokeMember("CoordinateModifiers", BindingFlags.GetProperty, null, target, null);
                    tmp[chaCtrl.fileStatus.coordinateType] = modifierDict.Value;
                    target.GetType().InvokeMember("CoordinateModifiers", BindingFlags.SetProperty, null, target, new object[] { tmp });
                    Logger.Log(LogLevel.Debug, "[KK_SCLO] ->Insert Modifier: " + modifierDict.Key);
                }
                BoneController.GetType().InvokeMember("StartCoroutine", publicFlag, null, BoneController, new object[] {
                    BoneController.GetType().InvokeMember("OnDataChangedCo", nonPublicFlag, null, BoneController, null)
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
