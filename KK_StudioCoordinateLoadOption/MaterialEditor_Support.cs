using BepInEx.Logging;
using Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_StudioCoordinateLoadOption {
    class MaterialEditor_Support {
        private static object MaterialEditorController;
        private static Dictionary<string, object> MaterialBackup = null;

        public static bool LoadAssembly() {
            if (File.Exists("BepInEx/KK_MaterialEditor.dll")) {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] MaterialEditor found");
            } else {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Load assembly FAILED: MaterialEditor");
                return false;
            }
            return true;
        }

        public static string[] targetListNames = {
            "MaterialShaderList",
            "RendererPropertyList",
            "MaterialFloatPropertyList",
            "MaterialColorPropertyList",
            "MaterialTexturePropertyList"
        };
        public enum ObjectType { StudioItem, Clothing, Accessory, Hair, Character, Other };

        public static void BackupMaterialData(ChaControl chaCtrl) {
            MaterialEditorController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KK_MaterialEditor"));
            if (null == MaterialEditorController) {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] No MaterialEditor Controller found");
            } else {
                MaterialBackup = new Dictionary<string, object>();

                foreach (var targetName in targetListNames) {
                    MaterialBackup.Add(targetName, MaterialEditorController.GetField(targetName).ToListWithoutType());
                }
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Get Original Material Finish");
            }
        }

        public static void RollbackMaterialData(int objectType, int CoordinateIndex, int Slot) {
            Predicate<object> predicate = new Predicate<object>(x =>
                (int)x.GetField("ObjectType") == objectType &&
                (int)x.GetField("CoordinateIndex") == CoordinateIndex &&
                (int)x.GetField("Slot") == Slot
            );
            if (null != MaterialEditorController && null != MaterialBackup) {
                foreach (var targetName in targetListNames) {
                    object target = MaterialEditorController.GetField(targetName);
                    target.RemoveAll(predicate);
                    target.AddRange(MaterialBackup[targetName].Where(predicate));

                    MaterialEditorController.SetField(targetName, target);
                }
            }
            if (objectType == (int)ObjectType.Clothing) {
                Logger.Log(LogLevel.Debug, $"[KK_SCLO] ->Material Rollback: {Patches.ClothesKindName[Slot]}");
            } else if (objectType == (int)ObjectType.Accessory) {
                Logger.Log(LogLevel.Debug, $"[KK_SCLO] ->Material Rollback: Accessory, Slot {Slot}");
            }
        }

        public static void CleanMaterialBackup() {
            MaterialBackup = null;
            if (null != MaterialEditorController) {
                MaterialEditorController.Invoke("OnCardBeingSaved", new object[] { 1 });
                MaterialEditorController.SetProperty("CoordinateChanging", true);
                MaterialEditorController.Invoke("LoadData", new object[] { true, true, false });
            }
            return;
        }
    }
}
