using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Extension;
using UnityEngine;

namespace CoordinateLoadOption {
    class ABMX : CharaCustomFunctionController_Support {
        public override string GUID => "KKABMX.Core";
        public override string ControllerName => "BoneController";
        public override string CCFCName => "ABMX";

        internal new Dictionary<string, object> SourceBackup { get => base.SourceBackup.ToDictionary<string, object>(); set => base.SourceBackup = value; }
        //internal new Dictionary<string, object> TargetBackup { get => base.TargetBackup.ToDictionary<string, object>(); set => base.TargetBackup = value; }

        public ABMX(ChaControl chaCtrl) : base(chaCtrl) 
            => isExist = CoordinateLoadOption._isABMXExist;

        private static Type BoneModifierType = null;

        public override bool LoadAssembly() {
            bool loadSuccess = LoadAssembly(out string path, new Version(3, 3));
            if (loadSuccess && !path.IsNullOrEmpty()) {
                BoneModifierType = Assembly.LoadFrom(path).GetType("KKABMX.Core.BoneModifier");
            }
            return loadSuccess;
        }

        /// <summary>
        /// 由ChaControl Controller取得ExtData
        /// </summary>
        /// <param name="chaCtrl">對象ChaControl</param>
        /// <returns>ExtDataData</returns>
        public override object GetDataFromController(ChaControl chaCtrl) {
            MonoBehaviour controller = GetController(chaCtrl);

            List<object> Modifiers = new List<object>();
            foreach (string boneName in (IEnumerable<string>)controller.Invoke("GetAllPossibleBoneNames")) {
                object tmpModifier = controller.Invoke("GetModifier", new object[] { boneName });
                if (null != tmpModifier) {
                    Modifiers.Add(tmpModifier);
                }
            }
            Dictionary<string, object> dictOut = Modifiers
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

            if (dictOut == null || dictOut.Count == 0) {
                Logger.LogDebug("No original ABMX BoneData.");
            } else {
                Logger.LogDebug("Get original ABMX BoneData: " + dictOut.Count);
            }

            return dictOut;
        }

        /// <summary>
        /// 拷貝ABMX資料
        /// </summary>
        /// <param name="sourceChaCtrl"></param>
        public void CopyABMXData(ChaControl sourceChaCtrl) {
            if (sourceChaCtrl != SourceChaCtrl || DefaultChaCtrl != TargetChaCtrl) {
                GetControllerAndBackupData(sourceChaCtrl, DefaultChaCtrl);
            }

            // Clear previous data for this coordinate from coord specific modifiers
            List<object> Modifiers = new List<object>();
            foreach (string boneName in (IEnumerable<string>)TargetController.Invoke("GetAllPossibleBoneNames")) {
                object tmpModifier = TargetController.Invoke("GetModifier", new object[] { boneName });
                if (null != tmpModifier) {
                    Modifiers.Add(tmpModifier);
                }
            }
            //Logger.LogDebug("Get Modifiers by AllBoneName");
            foreach (object modifier in Modifiers.Where(x => (bool)x.Invoke("IsCoordinateSpecific"))) {
                object y = modifier.Invoke("GetModifier", new object[] { (ChaFileDefine.CoordinateType)DefaultChaCtrl.fileStatus.coordinateType });
                y.Invoke("Clear");
            }
            //Logger.LogDebug("Clear all Modifiers");

            if (null != TargetController && null != base.SourceBackup) {
                foreach (KeyValuePair<string, object> modifierDict in SourceBackup) {
                    object target = TargetController.Invoke("GetModifier", new object[] { modifierDict.Key });
                    if (target == null) {
                        // Add any missing modifiers
                        target = Activator.CreateInstance(BoneModifierType, new object[] { modifierDict.Key });
                        Logger.LogDebug("->Create target");
                        TargetController.Invoke("AddModifier", new object[] { target });
                    }
                    target.Invoke("MakeCoordinateSpecific");
                    object[] tmp = (object[])target.GetProperty("CoordinateModifiers");
                    tmp[DefaultChaCtrl.fileStatus.coordinateType] = modifierDict.Value;
                    target.SetProperty("CoordinateModifiers", tmp);
                    Logger.LogDebug("->Insert Modifier: " + modifierDict.Key);
                }
                TargetController.SetProperty("NeedsFullRefresh", true); 
                Logger.LogDebug("->ABMX Bone Rollback complete");
            } else {
                Logger.LogDebug("->ABMX Bone not found");
            }
        }

        public override void SetExtDataFromController(ChaControl chaCtrl) {
            base.SetExtDataFromController(chaCtrl);
            GetController(chaCtrl).SetProperty("NeedsFullRefresh", true); 
        }
    }
}
