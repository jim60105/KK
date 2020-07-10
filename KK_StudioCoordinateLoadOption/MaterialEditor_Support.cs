using Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        internal static DirectoryInfo CacheDirectory;

        private static Type MaterialAPI = null;
        public static bool LoadAssembly() {
            try {
                string path = Extension.Extension.TryGetPluginInstance("com.deathweasel.bepinex.materialeditor", new Version(2, 0, 7))?.Info.Location;
                Assembly ass = Assembly.LoadFrom(path);
                MaterialAPI = ass.GetType("KK_Plugins.MaterialEditor.MaterialAPI");
                if (null == MaterialAPI) {
                    throw new Exception("Load assembly FAILED: MaterialEditor");
                }
                Logger.LogDebug("MaterialEditor found");

                CacheDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), KK_StudioCoordinateLoadOption.GUID));
                foreach (FileInfo file in CacheDirectory.GetFiles()) file.Delete();
                foreach (DirectoryInfo subDirectory in CacheDirectory.GetDirectories()) subDirectory.Delete(true);
                Logger.LogDebug("Clean cache folder");
                return true;
            } catch (Exception ex) {
                Logger.LogDebug(ex.Message);
                return false;
            }
        }

        public class StoredValueInfo {
            public string className;
            public string removeFunctionName;
            public string setFunctionName;
            public string listName;
            public StoredValueInfo(string className, string listName, string removeFunctionName, string setFunctionName) {
                this.className = className;
                this.listName = listName;
                this.removeFunctionName = removeFunctionName;
                this.setFunctionName = setFunctionName;
            }
        }

        //若MaterialEditor改版不運作時，優先確認這部分
        //KK_MaterialEditor.KK_MaterialEditor.MaterialEditorCharaController
        private static StoredValueInfo[] storedValueInfos = {
            new StoredValueInfo("MaterialShader","MaterialShaderList","RemoveMaterialShader","SetMaterialShader"),
            new StoredValueInfo("RendererProperty","RendererPropertyList","RemoveRendererProperty","SetRendererProperty"),
            new StoredValueInfo("MaterialFloatProperty","MaterialFloatPropertyList","RemoveMaterialFloatProperty","SetMaterialFloatProperty"),
            new StoredValueInfo("MaterialColorProperty","MaterialColorPropertyList","RemoveMaterialColorProperty","SetMaterialColorProperty"),
            new StoredValueInfo("MaterialTextureProperty","MaterialTexturePropertyList","RemoveMaterialTexture","")
        };

        /// <summary>
        /// KK_MaterialEditor.KK_MaterialEditor.ObjectType的複製
        /// </summary>
        public enum ObjectType { Unknown, Clothing, Accessory, Hair, Character };

        /// <summary>
        /// Copy前準備Source和Target資料
        /// </summary>
        /// <param name="sourceChaCtrl">來源ChaControl</param>
        /// <param name="targetChaCtrl">目標ChaControl</param>
        public static void GetControllerAndBackupData(ChaControl sourceChaCtrl = null, ChaControl targetChaCtrl = null) {
            if (null != sourceChaCtrl) {
                //Logger.LogDebug("Source-----");
                MaterialEditor_Support.sourceChaCtrl = sourceChaCtrl;
                SourceMaterialEditorController = GetExtDataFromController(sourceChaCtrl, out SourceMaterialBackup, out SourceTextureDictionaryBackup);
                if (null == SourceMaterialEditorController) {
                    Logger.LogDebug($"No Source Material Editor Controller found on {sourceChaCtrl.fileParam.fullname}");
                    return;
                }
            }

            if (null != targetChaCtrl) {
                //Logger.LogDebug("Target-----");
                MaterialEditor_Support.targetChaCtrl = targetChaCtrl;
                TargetMaterialEditorController = GetExtDataFromController(targetChaCtrl, out TargetMaterialBackup, out TargetTextureDictionaryBackup);
                if (null == TargetMaterialEditorController) {
                    Logger.LogDebug($"No Target Material Editor Controller found on {targetChaCtrl.fileParam.fullname}");
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
        /// <param name="sourceChaCtrl"></param>
        /// <param name="sourceSlot"></param>
        /// <param name="targetChaCtrl"></param>
        /// <param name="targetSlot"></param>
        /// <param name="gameObject">對象GameObject</param>
        /// <param name="objectType">對象分類</param>
        public static void CopyMaterialEditorData(ChaControl sourceChaCtrl, int sourceSlot, ChaControl targetChaCtrl, int targetSlot, GameObject gameObject, ObjectType objectType) {
            if (sourceChaCtrl != MaterialEditor_Support.sourceChaCtrl || targetChaCtrl != MaterialEditor_Support.targetChaCtrl) {
                GetControllerAndBackupData(sourceChaCtrl, targetChaCtrl);
            }

            //Make fake gameObject for MaterialEditorCharaController.FindGameObjectType
            if (objectType == ObjectType.Clothing && null != gameObject.GetComponentInChildren<ChaClothesComponent>() ||
                objectType == ObjectType.Accessory && (null != gameObject && null != gameObject?.GetComponent<ChaAccessoryComponent>())
                ) {
                RemoveMaterialEditorData(targetChaCtrl, targetSlot, gameObject, objectType);
                SetMaterialEditorData(sourceChaCtrl, sourceSlot, targetChaCtrl, targetSlot, gameObject, objectType);
            }
        }

        /// <summary>
        /// 移除Material Editor資料
        /// </summary>
        /// <param name="targetChaCtrl"></param>
        /// <param name="targetSlot"></param>
        /// <param name="gameObject">對象GameObject</param>
        /// <param name="objectType">對象分類</param>
        public static void RemoveMaterialEditorData(ChaControl targetChaCtrl, int targetSlot, GameObject gameObject, ObjectType objectType) {
            //是否有執行到
            bool doFlag = false;

            for (int i = 0; i < storedValueInfos.Length; i++) {
                bool doFlag2 = false;
                StoredValueInfo storedValue = storedValueInfos[i];
                object target = TargetMaterialBackup[storedValue.listName].ToListWithoutType();
                BindingFlags bf = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod;

                //移除
                object objRemoved = target.Where((x) =>
                    (int)x.GetField("ObjectType") == (int)objectType &&
                    (int)x.GetField("CoordinateIndex") == targetChaCtrl.fileStatus.coordinateType &&
                    (int)x.GetField("Slot") == targetSlot
                ).ForEach((x) => {
                    doFlag2 = true;
                    Renderer r = null;
                    Material m = null;

                    if (i == 1) {
                        r = MaterialAPI.InvokeMember("GetRendererList", bf, null, null, new object[] { gameObject })?.ToList<Renderer>().Where(y => y.name == (string)x.GetField("RendererName"))?.First();
                    } else {
                        m = MaterialAPI.InvokeMember("GetMaterials", bf, null, null, new object[] { gameObject, (string)x.GetField("MaterialName") })?.ToList<Material>()?.FirstOrDefault();
                    }

                    switch (i) {
                        case 0: //MaterialShader
                            TargetMaterialEditorController.Invoke(storedValue.removeFunctionName, new object[] {
                                targetSlot,
                                m,
                                gameObject,
                                true
                            });
                            if (null != x.GetField("RenderQueueOriginal")) {
                                TargetMaterialEditorController.Invoke("RemoveMaterialShaderRenderQueue", new object[] {
                                    targetSlot,
                                    m,
                                    gameObject,
                                    true
                                });
                            }
                            break;
                        case 1: //RendererProperty
                            TargetMaterialEditorController.Invoke(storedValue.removeFunctionName, new object[] {
                                targetSlot,
                                r,
                                x.GetField("Property") ,
                                gameObject,
                                true
                            });
                            break;
                        case 2: //MaterialFloatProperty
                        case 3: //MaterialColorProperty
                            TargetMaterialEditorController.Invoke(storedValue.removeFunctionName, new object[] {
                                targetSlot,
                                m,
                                x.GetField("Property") ,
                                gameObject,
                                true
                            });
                            break;
                        case 4: //MaterialTexture
                            TargetMaterialEditorController.Invoke(storedValue.removeFunctionName, new object[] {
                                    targetSlot,
                                    m,
                                    x.GetField("Property"),
                                    gameObject,
                                    false
                                });
                            break;
                    }
                });

                if (doFlag2) {
                    if (objRemoved.Count() > 0) {
                        GetExtDataFromController(targetChaCtrl, out TargetMaterialBackup, out TargetTextureDictionaryBackup);
                        Logger.LogDebug($"-->Remove {objRemoved.Count()} {storedValue.className}");
                    }
                }
                doFlag |= doFlag2;
            }

            if (objectType == ObjectType.Accessory) {
                if (doFlag) {
                    Logger.LogDebug($"->Remove Material Editor Data: {targetChaCtrl.fileParam.fullname} Slot{targetSlot}");
                } else {
                    Logger.LogDebug($"->No Material Editor Backup to remove: {targetChaCtrl.fileParam.fullname} Slot{targetSlot}");
                }
            } else if (objectType == ObjectType.Clothing) {
                if (doFlag) {
                    Logger.LogDebug($"->Remove Material Editor Data: {targetChaCtrl.fileParam.fullname} {Enum.GetName(typeof(ChaFileDefine.ClothesKind), targetSlot)}");
                } else {
                    Logger.LogDebug($"->No Material Editor Backup to remove: {targetChaCtrl.fileParam.fullname} {Enum.GetName(typeof(ChaFileDefine.ClothesKind), targetSlot)}");
                }
            }
        }

        /// <summary>
        /// 寫入Material Editor資料
        /// </summary>
        /// <param name="sourceChaCtrl"></param>
        /// <param name="sourceSlot"></param>
        /// <param name="targetChaCtrl"></param>
        /// <param name="targetSlot"></param>
        /// <param name="gameObject">對象GameObject</param>
        /// <param name="objectType">對象分類</param>
        private static void SetMaterialEditorData(ChaControl sourceChaCtrl, int sourceSlot, ChaControl targetChaCtrl, int targetSlot, GameObject gameObject, ObjectType objectType) {
            //是否有執行到
            bool doFlag = false;

            for (int i = 0; i < storedValueInfos.Length; i++) {
                bool doFlag2 = false;
                StoredValueInfo storedValue = storedValueInfos[i];
                object target = TargetMaterialBackup[storedValue.listName].ToListWithoutType();
                BindingFlags bf = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod;

                //加入
                object objAdded = SourceMaterialBackup[storedValue.listName].ToListWithoutType().Where(x =>
                    (int)x.GetField("ObjectType") == (int)objectType &&
                    (int)x.GetField("CoordinateIndex") == sourceChaCtrl.fileStatus.coordinateType &&
                    (int)x.GetField("Slot") == sourceSlot
                ).ForEach((x) => {
                    doFlag2 = true;
                    Renderer r = null;
                    Material m = null;

                    if (i == 1) {
                        r = MaterialAPI.InvokeMember("GetRendererList", bf, null, null, new object[] { gameObject })?.ToList<Renderer>().Where(y => y.name == (string)x.GetField("RendererName"))?.First();
                    } else {
                        m = MaterialAPI.InvokeMember("GetMaterials", bf, null, null, new object[] { gameObject, (string)x.GetField("MaterialName") })?.ToList<Material>()?.FirstOrDefault();
                    }

                    switch (i) {
                        case 0: //MaterialShader
                            TargetMaterialEditorController.Invoke(storedValue.setFunctionName, new object[] {
                                targetSlot,
                                m,
                                x.GetField("ShaderName"),
                                gameObject,
                                true
                            });
                            if (null != x.GetField("RenderQueueOriginal")) {
                                TargetMaterialEditorController.Invoke("SetMaterialShaderRenderQueue", new object[] {
                                    targetSlot,
                                    m,
                                    x.GetField("RenderQueue"),
                                    gameObject,
                                    true
                                });
                            }
                            break;
                        case 1: //RendererProperty
                            TargetMaterialEditorController.Invoke(storedValue.setFunctionName, new object[] {
                                targetSlot,
                                r,
                                x.GetField("Property"),
                                x.GetField("Value"),
                                gameObject,
                                true
                            });
                            break;
                        case 2: //MaterialFloatProperty
                            TargetMaterialEditorController.Invoke(storedValue.setFunctionName, new object[] {
                                targetSlot,
                                m,
                                x.GetField("Property"),
                                (float)Convert.ToDouble( x.GetField("Value")),
                                gameObject,
                                true
                            });
                            break;
                        case 3: //MaterialColorProperty
                            TargetMaterialEditorController.Invoke(storedValue.setFunctionName, new object[] {
                                targetSlot,
                                m,
                                x.GetField("Property"),
                                x.GetField("Value"),
                                gameObject,
                                true
                            });
                            break;
                        case 4: //MaterialTextureProperty
                            //Texture
                            int? texID = (int?)x.GetField("TexID");
                            if (texID.HasValue && SourceTextureDictionaryBackup.TryGetValue(texID.Value, out object textureHolder) && textureHolder.GetProperty("Data") is byte[] BA) {
                                string tempPath = Path.Combine(CacheDirectory.FullName, DateTime.UtcNow.Ticks + "_" + texID);
                                File.WriteAllBytes(tempPath, BA);
                                TargetMaterialEditorController.Invoke("SetMaterialTextureFromFile", new object[] {
                                    targetSlot,
                                    m,
                                    x.GetField("Property"),
                                    tempPath,
                                    gameObject,
                                    false
                                });

                                File.Delete(tempPath);

                                //Offset
                                if (null != x.GetField("OffsetOriginal")) {
                                    TargetMaterialEditorController.Invoke("SetMaterialTextureOffset", new object[] {
                                        targetSlot,
                                        m,
                                        x.GetField("Property"),
                                        x.GetField("Offset"),
                                        gameObject,
                                        true
                                    });
                                }
                                //Scale
                                if (null != x.GetField("ScaleOriginal")) {
                                    TargetMaterialEditorController.Invoke("SetMaterialTextureScale", new object[] {
                                        targetSlot,
                                        m,
                                        x.GetField("Property"),
                                        x.GetField("Scale"),
                                        gameObject,
                                        true
                                    });
                                }
                            }
                            break;
                    }
                });

                if (doFlag2) {
                    if (objAdded.Count() > 0) {
                        GetExtDataFromController(targetChaCtrl, out TargetMaterialBackup, out TargetTextureDictionaryBackup);
                        Logger.LogDebug($"-->Change {objAdded.Count()} {storedValue.className}");
                    }
                }
                doFlag |= doFlag2;
            }

            if (objectType == ObjectType.Accessory) {
                if (doFlag) {
                    Logger.LogDebug($"->Set Material Editor Data: {sourceChaCtrl.fileParam.fullname} Slot{sourceSlot} -> {targetChaCtrl.fileParam.fullname} Slot{targetSlot}");
                } else {
                    Logger.LogDebug($"->No Material Editor Backup to set: {sourceChaCtrl.fileParam.fullname} {sourceSlot}");
                }
            } else if (objectType == ObjectType.Clothing) {
                if (doFlag) {
                    Logger.LogDebug($"->Set Material Editor Data: {sourceChaCtrl.fileParam.fullname} {Enum.GetName(typeof(ChaFileDefine.ClothesKind), sourceSlot)} -> {targetChaCtrl.fileParam.fullname} {Enum.GetName(typeof(ChaFileDefine.ClothesKind), targetSlot)}");
                } else {
                    Logger.LogDebug($"->No Material Editor Backup to set: {sourceChaCtrl.fileParam.fullname} {Enum.GetName(typeof(ChaFileDefine.ClothesKind), sourceSlot)}");
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
