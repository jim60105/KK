using Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_CharaOverlaysBasedOnCoordinate {
    class MaterialEditor_Support {
        private static object MaterialEditorController;
        private static readonly BepInEx.Logging.ManualLogSource Logger = KK_CharaOverlaysBasedOnCoordinate.Logger;
        private static Dictionary<string, object> MaterialBackup = null;
        private static Dictionary<int, byte[]> TextureDictionaryBackup = null;

        public static bool LoadAssembly() {
            if (null != Extension.Extension.TryGetPluginInstance("com.deathweasel.bepinex.materialeditor", new Version(1, 7))) {
                Logger.LogDebug("MaterialEditor found");
                return true;
            } else {
                Logger.LogDebug("Load assembly FAILED: MaterialEditor");
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

        public static object[] GetMaterialData(ChaControl chaCtrl) {
            MaterialEditorController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "MaterialEditorCharaController"));
            if (null == MaterialEditorController) {
                Logger.LogDebug("No MaterialEditor Controller found");
            } else {
                Dictionary<int, byte[]> TextureDictionaryBackup = MaterialEditorController.GetField("TextureDictionary").ToDictionary<int, byte[]>();
                Logger.LogDebug("TextureDictionaryBackup Count: " + TextureDictionaryBackup.Count);

                Dictionary<string, object> MaterialBackup = new Dictionary<string, object>();
                foreach (var storedValue in storedValueInfos) {
                    MaterialBackup.Add(storedValue.listName, MaterialEditorController.GetField(storedValue.listName).ToListWithoutType().Where(x => (int)x.GetField("ObjectType") == (int)ObjectType.Character));
                }

                List<int> IDsToPurge = new List<int>();
                foreach (int texID in TextureDictionaryBackup.Keys) {
                    if (!MaterialBackup["MaterialTexturePropertyList"].ToList<object>().Any(x => (int)x.GetField("TexID") == texID)) {
                        IDsToPurge.Add(texID);
                    }
                }

                foreach (int texID in IDsToPurge) TextureDictionaryBackup.Remove(texID);

                //Logger.LogDebug("Get Original Material Finish");
                return new object[] { MaterialBackup, TextureDictionaryBackup };
            }
            return null;
        }

        public static void SetMaterialData(ChaControl chaCtrl, object[] MEBackupPack) {
            Predicate<object> predicate = new Predicate<object>(x => (int)x.GetField("ObjectType") == (int)ObjectType.Character );
            //是否有執行到
            bool doFlag = false;

            MaterialEditorController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "MaterialEditorCharaController"));
            Dictionary<string, object> MaterialBackup = null;
            Dictionary<int, byte[]> TextureDictionaryBackup = null;
            try {
                MaterialBackup = MEBackupPack[0].ToDictionary<string, object>();
                TextureDictionaryBackup = MEBackupPack[1].ToDictionary<int, byte[]>();
            } catch (Exception) {
                Logger.LogError("Failed to cast Material Backup Data!");
            }

            Dictionary<int, int> TexIDChangeList = new Dictionary<int, int>();

            if (null != MaterialEditorController && null != MaterialBackup & null != TextureDictionaryBackup) {
                foreach (var tex in TextureDictionaryBackup) {
                    int texID = (int)MaterialEditorController.Invoke("SetAndGetTextureID", new object[] { tex.Value });
                    if (tex.Key != texID) {
                        TexIDChangeList.Add(tex.Key, texID);
                    }
                }

                if (TexIDChangeList.Count != 0) {
                    foreach (var MTP in MaterialBackup["MaterialTexturePropertyList"].ToList<object>()) {
                        if (TexIDChangeList.TryGetValue((int)MTP.GetField("TexID"), out int newTexID)) {
                            MTP.SetField("TexID", newTexID);
                        }
                    }
                }

                //TODO
                //Not Finish, 但我選則死亡

                for (int i = 0; i < storedValueInfos.Length; i++) {
                    StoredValueInfo storedValue = storedValueInfos[i];
                    object target = MaterialEditorController.GetField(storedValue.listName);
                    //移除Character Overlay
                    doFlag = target.RemoveAll(predicate) > 0;
                    //塞入原始該位置的Material資料
                    var obj2Add = MaterialBackup[storedValue.listName].Where(predicate);
                    if (obj2Add.Count() > 0) {
                        doFlag = true;
                        target.AddRange(obj2Add);
                        Logger.LogDebug($"Rollback {obj2Add.Count()} {storedValue.className} Object");
                    }
                    if (doFlag)
                        MaterialEditorController.SetField(storedValue.listName, target);
                }
                if (!doFlag) {
                } else if (objectType == (int)ObjectType.Clothing) {
                    Logger.LogDebug($"->Material Rollback: {Patches.ClothesKindName[Slot]}");
                } else if (objectType == (int)ObjectType.Accessory) {
                    Logger.LogDebug($"->Material Rollback: Accessory, Slot {Slot}");
                }
            }
        }

        public static void CleanMaterialBackup() {
            if (null != MaterialEditorController) {
                MaterialEditorController.Invoke("OnCardBeingSaved", new object[] { 1 });
                Logger.LogDebug("TextureDictionary Count: " + MaterialEditorController.GetField("TextureDictionary").ToDictionary<int, byte[]>().Count);
            }
            MaterialBackup = null;
            TextureDictionaryBackup = null;
            return;
        }
    }
}
