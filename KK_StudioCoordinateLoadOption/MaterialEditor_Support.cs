using Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_StudioCoordinateLoadOption {
    class MaterialEditor_Support {
        private static object SourceMaterialEditorController;
        private static object TargetMaterialEditorController;
        private static readonly BepInEx.Logging.ManualLogSource Logger = KK_StudioCoordinateLoadOption.Logger;
        internal static ChaControl sourceChaCtrl = null;
        internal static Dictionary<string, object> SourceMaterialBackup = null;
        internal static Dictionary<int, object> SourceTextureDictionaryBackup = null;
        internal static ChaControl targetChaCtrl = null;
        internal static Dictionary<string, object> TargetMaterialBackup = null;
        internal static Dictionary<int, object> TargetTextureDictionaryBackup = null;

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
        private static StoredValueInfo[] storedValueInfos = {
            new StoredValueInfo("MaterialShader","MaterialShaderList","RemoveMaterialShaderName","AddMaterialShader"),
            new StoredValueInfo("RendererProperty","RendererPropertyList","RemoveRendererProperty","AddRendererProperty"),
            new StoredValueInfo("MaterialFloatProperty","MaterialFloatPropertyList","RemoveMaterialFloatProperty","AddMaterialFloatProperty"),
            new StoredValueInfo("MaterialColorProperty","MaterialColorPropertyList","RemoveMaterialColorProperty","AddMaterialColorProperty"),
            new StoredValueInfo("MaterialTextureProperty","MaterialTexturePropertyList","RemoveMaterialTextureProperty","AddMaterialTextureProperty")
        };

        /// <summary>
        /// KK_MaterialEditor.KK_MaterialEditor.ObjectType的複製
        /// </summary>
        public enum ObjectType { StudioItem, Clothing, Accessory, Hair, Character, Other };

        /// <summary>
        /// Copy前準備Source和Target資料
        /// </summary>
        /// <param name="sourceChaCtrl">來源ChaControl</param>
        /// <param name="targetChaCtrl">目標ChaControl</param>
        public static void GetControllerAndBackupData(ChaControl sourceChaCtrl, ChaControl targetChaCtrl = null) {
            if (null != sourceChaCtrl) {
                Logger.LogDebug("Source-----");
                MaterialEditor_Support.sourceChaCtrl = sourceChaCtrl;
                SourceMaterialEditorController = GetExtDataFromController(sourceChaCtrl, out SourceMaterialBackup, out SourceTextureDictionaryBackup);
                if (null == SourceMaterialEditorController) {
                    Logger.LogDebug("No Source Material Editor Controller found");
                    return;
                }
            }

            if (null != targetChaCtrl) {
                Logger.LogDebug("Target-----");
                MaterialEditor_Support.targetChaCtrl = targetChaCtrl;
                TargetMaterialEditorController = GetExtDataFromController(targetChaCtrl, out TargetMaterialBackup, out TargetTextureDictionaryBackup);
                if (null == TargetMaterialEditorController) {
                    Logger.LogDebug("No Target Material Editor Controller found");
                    return;
                }
            }
        }

        /// <summary>
        /// 由ChaControl Controller取得TextureDictionary，注意這個無法寫回
        /// </summary>
        /// <param name="chaCtrl">對象ChaControl</param>
        /// <returns></returns>
        public static Dictionary<int, object> GetTextureDictionaryFromController(ChaControl chaCtrl) {
            MonoBehaviour MaterialEditorController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "MaterialEditorCharaController"));
            if (null == MaterialEditorController) {
                Logger.LogDebug("No MaterialEditor Controller found");
                return null;
            }

            Dictionary<int, object> TextureDictionary = MaterialEditorController.GetField("TextureDictionary").ToDictionary<int, object>();
            //if (TextureDictionary is Dictionary<int, object> tdb) {
            //    Logger.LogDebug("TextureDictionaryBackup Count: " + tdb.Count);
            //}
            return TextureDictionary;
        }

        /// <summary>
        /// 由ChaControl Controller取得ExtData
        /// </summary>
        /// <param name="chaCtrl">對象ChaControl</param>
        /// <param name="MaterialBackup">Output Material Data Backup</param>
        /// <param name="TextureDictionaryBackup">Output TextureDiuctionary，注意這個無法寫回Controller</param>
        /// <returns></returns>
        public static MonoBehaviour GetExtDataFromController(ChaControl chaCtrl, out Dictionary<string, object> MaterialBackup, out Dictionary<int, object> TextureDictionaryBackup) {
            MaterialBackup = new Dictionary<string, object>();
            TextureDictionaryBackup = null;
            MonoBehaviour MaterialEditorController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "MaterialEditorCharaController"));
            if (null == MaterialEditorController) {
                Logger.LogDebug("No MaterialEditor Controller found");
                return null;
            }

            TextureDictionaryBackup = GetTextureDictionaryFromController(chaCtrl);

            foreach (StoredValueInfo storedValue in storedValueInfos) {
                MaterialBackup.Add(storedValue.listName, MaterialEditorController.GetField(storedValue.listName).ToListWithoutType());
            }
            //Logger.LogDebug($"Get {chaCtrl.fileParam.fullname} Material From Controller");
            return MaterialEditorController;
        }

        /// <summary>
        /// 將給入的Material Data Backup儲存至ChaControl之Controller內
        /// </summary>
        /// <param name="chaCtrl">目標ChaControl</param>
        /// <param name="objectType">類型</param>
        /// <param name="MaterialBackup">要存入的Material Data Backup</param>
        /// <param name="Slot">Coordinate ClothesKind 或 Accessory Slot</param>
        public static void SetToController(ChaControl chaCtrl, ObjectType objectType, Dictionary<string, object> MaterialBackup = null, int Slot = -1) {
            Predicate<object> predicate = new Predicate<object>(x =>
                (int)x.GetField("ObjectType") == (int)objectType &&
                (int)x.GetField("CoordinateIndex") == chaCtrl.fileStatus.coordinateType &&
                //若Slot有給入，則加上檢查Slot的判斷
                (Slot < 0) ? true : (int)x.GetField("Slot") == Slot
            );

            //是否有執行到
            bool doFlag = false;

            MonoBehaviour MaterialEditorController;
            if (null == MaterialBackup) {
                MaterialEditorController = GetExtDataFromController(chaCtrl, out Dictionary<string, object> m, out Dictionary<int, object> _);
                if (null == MaterialBackup) MaterialBackup = m;
            } else {
                MaterialEditorController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "MaterialEditorCharaController"));
            }

            if (null != MaterialEditorController && null != MaterialBackup) {
                for (int i = 0; i < storedValueInfos.Length; i++) {
                    StoredValueInfo storedValue = storedValueInfos[i];
                    bool doFlag2 = false;
                    object target = MaterialEditorController.GetField(storedValue.listName).ToListWithoutType();
                    //移除
                    doFlag2 = target.RemoveAll(predicate) > 0;
                    //加回
                    object obj2Add = MaterialBackup[storedValue.listName].Where(predicate);
                    if (obj2Add.Count() > 0) {
                        doFlag2 = true;

                        target.AddRange(obj2Add);
                    }

                    if (doFlag2) {
                        MaterialEditorController.SetField(storedValue.listName, target);
                        GetExtDataFromController(chaCtrl, out Dictionary<string, object> m, out Dictionary<int, object> t);
                        Logger.LogDebug($"-->{storedValue.className}: {m[storedValue.listName].Count()}");
                    }

                    doFlag |= doFlag2;
                }

                if (doFlag) {
                    if (objectType == ObjectType.Clothing) {
                        Logger.LogDebug($"->Material Set: Clothes " + ((Slot >= 0) ? $", {Patches.ClothesKindName[Slot]}" : ", All Clothes"));
                    } else if (objectType == ObjectType.Accessory) {
                        Logger.LogDebug($"->Material Set: Accessory" + ((Slot >= 0) ? ", Slot " + Slot : ", All Slots"));
                    }
                }
            }
        }

        /// <summary>
        /// 將Controller內之Material Editor Data儲存至ChaControl ExtendedData內
        /// </summary>
        /// <param name="chaCtrl">對象ChaControl</param>
        public static void SetExtDataFromController(ChaControl chaCtrl) {
            MonoBehaviour MaterialEditorController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "MaterialEditorCharaController"));
            MaterialEditorController.Invoke("OnCardBeingSaved", new object[] { 1 });
        }

        /// <summary>
        /// 將Controller內之Material Editor Data儲存至Coordinate ExtendedData內
        /// </summary>
        /// <param name="chaCtrl">來源ChaControl</param>
        /// <param name="coordinate">目標Coordinate</param>
        public static void SetCoordinateExtDataFromController(ChaControl chaCtrl, ChaFileCoordinate coordinate = null) {
            if (null == coordinate) {
                coordinate = chaCtrl.nowCoordinate;
            }
            MonoBehaviour MaterialEditorController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "MaterialEditorCharaController"));
            MaterialEditorController.Invoke("OnCoordinateBeingSaved", new object[] { coordinate });
        }

        /// <summary>
        /// 由Coordinate載入Material Editor Data至Controller內
        /// </summary>
        /// <param name="chaCtrl">要被設定的ChaControl</param>
        /// <param name="coordinate">要載入的coordibate</param>
        /// <returns></returns>
        public static bool SetControllerFromCoordinate(ChaControl chaCtrl, ChaFileCoordinate coordinate = null) {
            if (null == coordinate) {
                coordinate = chaCtrl.nowCoordinate;
            }
            MonoBehaviour MaterialEditorController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "MaterialEditorCharaController"));
            MaterialEditorController.Invoke("OnCoordinateBeingLoaded", new object[] { coordinate, false });
            return true;
        }

        /// <summary>
        /// 拷貝Material Editor資料
        /// </summary>
        /// <param name="objectType">對象分類</param>
        /// <param name="sourceChaCtrl"></param>
        /// <param name="sourceSlot"></param>
        /// <param name="targetChaCtrl"></param>
        /// <param name="targetSlot"></param>
        public static void CopyMaterialEditorData(ObjectType objectType, ChaControl sourceChaCtrl, int sourceSlot, ChaControl targetChaCtrl, int targetSlot) {
            if (sourceChaCtrl != MaterialEditor_Support.sourceChaCtrl || targetChaCtrl != MaterialEditor_Support.targetChaCtrl) {
                GetControllerAndBackupData(sourceChaCtrl, targetChaCtrl);
            }

            //是否有執行到
            bool doFlag = false;

            for (int i = 0; i < storedValueInfos.Length; i++) {
                bool doFlag2 = false;
                StoredValueInfo storedValue = storedValueInfos[i];
                object target = TargetMaterialBackup[storedValue.listName].ToListWithoutType();

                //移除
                doFlag2 = target.RemoveAll(x =>
                    (int)x.GetField("ObjectType") == (int)objectType &&
                    (int)x.GetField("CoordinateIndex") == targetChaCtrl.fileStatus.coordinateType &&
                    (int)x.GetField("Slot") == targetSlot
                ) > 0;

                //加入
                object obj2Add = SourceMaterialBackup[storedValue.listName].ToListWithoutType().Where(x =>
                    (int)x.GetField("ObjectType") == (int)objectType &&
                    (int)x.GetField("CoordinateIndex") == sourceChaCtrl.fileStatus.coordinateType &&
                    (int)x.GetField("Slot") == sourceSlot
                );

                if (obj2Add.Count() > 0) {
                    doFlag2 = true;

                    //對原資料修改Slot和CoordinateIndex
                    obj2Add.ForEach((x) => {
                        x.SetField("CoordinateIndex", targetChaCtrl.fileStatus.coordinateType);
                        x.SetField("Slot", targetSlot);
                    });

                    //修改MaterialTexture TexID
                    if (storedValue.className == "MaterialTextureProperty") {
                        obj2Add = obj2Add.ForEach((texprop) => {
                            int? texID = (int?)texprop.GetField("TexID");
                            int? newTexID = null;
                            if (texID.HasValue && SourceTextureDictionaryBackup.TryGetValue(texID.Value, out object textureHolder) && textureHolder.GetProperty("Data") is byte[] BA) {
                                //對TargetController塞來自Source的byte[] texture，並取得他在target的TexID
                                newTexID = (int)TargetMaterialEditorController.Invoke("SetAndGetTextureID", new object[] { BA });
                                if (newTexID.HasValue) {
                                    TargetTextureDictionaryBackup = GetTextureDictionaryFromController(targetChaCtrl);
                                    texprop.SetField("TexID", newTexID.Value);
                                }
                                Logger.LogDebug($"-->Copy Material Editor Texture: {sourceChaCtrl.fileParam.fullname} Tex{texID} -> {targetChaCtrl.fileParam.fullname} Tex{newTexID}");
                            }
                        });
                    }

                    target.AddRange(obj2Add);
                }

                if (doFlag2) {
                    TargetMaterialBackup[storedValue.listName] = target;
                    Logger.LogDebug($"-->Change {obj2Add.Count()} {storedValue.className}");
                }
                doFlag |= doFlag2;
            }

            if (objectType == ObjectType.Accessory) {
                if (doFlag) {
                    Logger.LogDebug($"->Copy Material Editor Data: {sourceChaCtrl.fileParam.fullname} Slot{sourceSlot} -> {targetChaCtrl.fileParam.fullname} Slot{targetSlot}");
                } else {
                    Logger.LogDebug($"->No Material Editor Backup to copy: {sourceChaCtrl.fileParam.fullname} {sourceSlot}");
                }
            } else if (objectType == ObjectType.Clothing) {
                if (doFlag) {
                    Logger.LogDebug($"->Copy Material Editor Data: {sourceChaCtrl.fileParam.fullname} {Enum.GetName(typeof(ChaFileDefine.ClothesKind), sourceSlot)} -> {targetChaCtrl.fileParam.fullname} {Enum.GetName(typeof(ChaFileDefine.ClothesKind), targetSlot)}");
                } else {
                    Logger.LogDebug($"->No Material Editor Backup to copy: {sourceChaCtrl.fileParam.fullname} {Enum.GetName(typeof(ChaFileDefine.ClothesKind), sourceSlot)}");
                }
            }
        }

        public static void ClearMaterialBackup() {
            SourceMaterialBackup = null;
            SourceTextureDictionaryBackup = null;
            SourceMaterialEditorController = null;
            TargetMaterialBackup = null;
            TargetTextureDictionaryBackup = null;
            TargetMaterialEditorController = null;
        }
    }
}
