using Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_StudioCoordinateLoadOption {
    class MaterialEditor_Support {
        private static object MaterialEditorController;
        private static readonly BepInEx.Logging.ManualLogSource Logger = KK_StudioCoordinateLoadOption.Logger;
        private static Dictionary<string, object> MaterialBackup = null;
        private static Dictionary<int, byte[]> TextureDictionaryBackup = null;

        public static bool LoadAssembly() {
            if (null != KK_StudioCoordinateLoadOption.TryGetPluginInstance("com.deathweasel.bepinex.materialeditor", new Version(1, 7))) {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("MaterialEditor found");
                return true;
            } else {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("Load assembly FAILED: MaterialEditor");
                return false;
            }
        }

        public class StoredValueInfo {
            public string className;
            public string removeFunctionName;
            public string addFunctionName;
            public string listName;
            public StoredValueInfo(string className, string listName, string removeFunctionName, string addFunctionName) {
                this.className = className;
                this.listName = listName;
                this.removeFunctionName = removeFunctionName;
                this.addFunctionName = addFunctionName;
            }
        }

        //若MaterialEditor改版不運作時，優先確認這部分
        //KK_MaterialEditor.KK_MaterialEditor.MaterialEditorCharaController
        public static StoredValueInfo[] storedValueInfos = {
            new StoredValueInfo("MaterialShader","MaterialShaderList","RemoveMaterialShaderName","AddMaterialShader"),
            new StoredValueInfo("RendererProperty","RendererPropertyList","RemoveRendererProperty","AddRendererProperty"),
            new StoredValueInfo("MaterialFloatProperty","MaterialFloatPropertyList","RemoveMaterialFloatProperty","AddMaterialFloatProperty"),
            new StoredValueInfo("MaterialColorProperty","MaterialColorPropertyList","RemoveMaterialColorProperty","AddMaterialColorProperty"),
            new StoredValueInfo("MaterialTextureProperty","MaterialTexturePropertyList","RemoveMaterialTextureProperty","AddMaterialTextureProperty")
        };
        //KK_MaterialEditor.KK_MaterialEditor.ObjectType
        public enum ObjectType { StudioItem, Clothing, Accessory, Hair, Character, Other };

        public static void BackupMaterialData(ChaControl chaCtrl) {
            MaterialEditorController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "MaterialEditorCharaController"));
            if (null == MaterialEditorController) {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("No MaterialEditor Controller found");
            } else {
                TextureDictionaryBackup = MaterialEditorController.GetField("TextureDictionary").ToDictionary<int, byte[]>();
                KK_StudioCoordinateLoadOption.Logger.LogDebug("TextureDictionaryBackup Count: " + TextureDictionaryBackup.Count);

                MaterialBackup = new Dictionary<string, object>();

                foreach (var storedValue in storedValueInfos) {
                    MaterialBackup.Add(storedValue.listName, MaterialEditorController.GetField(storedValue.listName).ToListWithoutType());
                }
                KK_StudioCoordinateLoadOption.Logger.LogDebug("Get Original Material Finish");
            }
        }

        public static void RollbackMaterialData(int objectType, int CoordinateIndex, int Slot) {
            Predicate<object> predicate = new Predicate<object>(x =>
                (int)x.GetField("ObjectType") == objectType &&
                (int)x.GetField("CoordinateIndex") == CoordinateIndex &&
                (int)x.GetField("Slot") == Slot
            );
            //是否有執行到Rollback
            bool doFlag = false;

            if (null != MaterialEditorController && null != MaterialBackup) {
                for (int i = 0; i < storedValueInfos.Length; i++) {
                    StoredValueInfo storedValue = storedValueInfos[i];
                    object target = MaterialEditorController.GetField(storedValue.listName);
                    //移除fakeLoad加載的部分
                    doFlag = target.RemoveAll(predicate) > 0;
                    //加回原始該位置的Material資料
                    var obj2Add = MaterialBackup[storedValue.listName].Where(predicate);
                    if (obj2Add.Count() > 0) {
                        doFlag = true;
                        target.AddRange(obj2Add);
                        KK_StudioCoordinateLoadOption.Logger.LogDebug($"Rollback {obj2Add.Count()} {storedValue.className} Object");
                    }
                    if (doFlag)
                        MaterialEditorController.SetField(storedValue.listName, target);
                }
                if (!doFlag) {
                } else if (objectType == (int)ObjectType.Clothing) {
                    KK_StudioCoordinateLoadOption.Logger.LogDebug($"->Material Rollback: {Patches.ClothesKindName[Slot]}");
                } else if (objectType == (int)ObjectType.Accessory) {
                    KK_StudioCoordinateLoadOption.Logger.LogDebug($"->Material Rollback: Accessory, Slot {Slot}");
                }
            }
        }

        public static void CleanMaterialBackup() {
            if (null != MaterialEditorController) {
                MaterialEditorController.Invoke("OnCardBeingSaved", new object[] { 1 });
                KK_StudioCoordinateLoadOption.Logger.LogDebug("TextureDictionary Count: " + MaterialEditorController.GetField("TextureDictionary").ToDictionary<int, byte[]>().Count);
            }
            MaterialBackup = null;
            TextureDictionaryBackup = null;
            return;
        }
    }
}
