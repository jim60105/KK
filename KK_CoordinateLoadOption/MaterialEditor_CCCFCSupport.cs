using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Extension;
using UnityEngine;

namespace KK_CoordinateLoadOption {
    class MaterialEditor_CCCFCSupport : CCFCSupport {
        public override string GUID => "com.deathweasel.bepinex.materialeditor";
        public override string ControllerName => "MaterialEditorCharaController";
        public override string CCFCName => "MaterialEditor";

        internal new Dictionary<string, object> SourceBackup { get => base.SourceBackup?.ToDictionary<string, object>(); set => base.SourceBackup = value; }
        internal new Dictionary<string, object> TargetBackup { get => base.TargetBackup?.ToDictionary<string, object>(); set => base.TargetBackup = value; }

        public MaterialEditor_CCCFCSupport(ChaControl chaCtrl) : base(chaCtrl)
            => isExist = KK_CoordinateLoadOption._isMaterialEditorExist;

        internal Dictionary<int, object> SourceTextureDictionaryBackup = null;
        internal Dictionary<int, object> TargetTextureDictionaryBackup = null;
        internal static DirectoryInfo CacheDirectory;

        private static Type MaterialAPI = null;
        private static Type ObjectTypeME = null;
        public override bool LoadAssembly() {
            bool loadSuccess = LoadAssembly(out string path, new Version(2, 0, 7));
            if (loadSuccess && !path.IsNullOrEmpty()) {
                Assembly ass = Assembly.LoadFrom(path);
                MaterialAPI = ass.GetType("MaterialEditorAPI.MaterialAPI");
                if (null==MaterialAPI) {
                    Logger.LogError("Get MaterialAPI type failed");
                    loadSuccess = false;
                }
                ObjectTypeME = ass.GetType("KK_Plugins.MaterialEditor.MaterialEditorCharaController").GetNestedType("ObjectType");
                if (null == ObjectTypeME || !ObjectTypeME.IsEnum) {
                    Logger.LogError("Get ObjectType Enum failed");
                    loadSuccess = false;
                }
                MakeCacheDirectory();
            }
            return loadSuccess;
        }

        private static void MakeCacheDirectory() {
            CacheDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), KK_CoordinateLoadOption.GUID));
            foreach (FileInfo file in CacheDirectory.GetFiles()) file.Delete();
            foreach (DirectoryInfo subDirectory in CacheDirectory.GetDirectories()) subDirectory.Delete(true);
            Logger.LogDebug("Clean cache folder");
        }

        /* HACK 若MaterialEditor改版不運作時，優先確認這部分
         * 這些是變數名稱和方法名稱
         */
        #region StoredValueInfos
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

        /// <summary>
        /// MaterialEditor.MaterialEditorCharaController
        /// </summary>
        private static readonly StoredValueInfo[] storedValueInfos = {
            new StoredValueInfo("MaterialShader","MaterialShaderList","RemoveMaterialShader","SetMaterialShader"),
            new StoredValueInfo("RendererProperty","RendererPropertyList","RemoveRendererProperty","SetRendererProperty"),
            new StoredValueInfo("MaterialFloatProperty","MaterialFloatPropertyList","RemoveMaterialFloatProperty","SetMaterialFloatProperty"),
            new StoredValueInfo("MaterialColorProperty","MaterialColorPropertyList","RemoveMaterialColorProperty","SetMaterialColorProperty"),
            new StoredValueInfo("MaterialTextureProperty","MaterialTexturePropertyList","RemoveMaterialTexture","")
        };

        /// <summary>
        /// MaterialEditor.MaterialEditorCharaController.ObjectType
        /// </summary>
        public enum ObjectType { Unknown, Clothing, Accessory, Hair, Character };
        #endregion

        public override bool GetControllerAndBackupData(ChaControl sourceChaCtrl = null, ChaControl targetChaCtrl = null) {
            bool loadSuccess = base.GetControllerAndBackupData(sourceChaCtrl, targetChaCtrl);
            if (null != sourceChaCtrl) SourceTextureDictionaryBackup = GetTextureDictionaryFromController(sourceChaCtrl);
            if (null != targetChaCtrl) TargetTextureDictionaryBackup = GetTextureDictionaryFromController(targetChaCtrl);
            return loadSuccess;
        }

        /// <summary>
        /// 由ChaControl Controller取得TextureDictionary，注意這個無法寫回
        /// </summary>
        /// <param name="chaCtrl">對象ChaControl</param>
        /// <returns></returns>
        public Dictionary<int, object> GetTextureDictionaryFromController(ChaControl chaCtrl)
            => GetController(chaCtrl).GetField("TextureDictionary").ToDictionary<int, object>();

        public override object GetDataFromController(ChaControl chaCtrl) {
            Dictionary<string, object> MaterialBackup = new Dictionary<string, object>();
            MonoBehaviour controller = GetController(chaCtrl);

            foreach (StoredValueInfo storedValue in storedValueInfos) {
                MaterialBackup.Add(storedValue.listName, controller.GetField(storedValue.listName).ToListWithoutType());
            }
            return MaterialBackup;
        }

        /// <summary>
        /// 將給入的Material Data Backup儲存至ChaControl之Controller內
        /// </summary>
        /// <param name="chaCtrl">目標ChaControl</param>
        /// <param name="objectType">類型</param>
        /// <param name="MaterialBackup">要存入的Material Data Backup</param>
        /// <param name="Slot">Coordinate ClothesKind 或 Accessory Slot</param>
        public void SetToController(ChaControl chaCtrl, ObjectType objectType, Dictionary<string, object> MaterialBackup = null, int Slot = -1) {
            Predicate<object> predicate = new Predicate<object>(x =>
                (int)x.GetField("ObjectType") == (int)objectType &&
                (int)x.GetField("CoordinateIndex") == chaCtrl.fileStatus.coordinateType &&
                //若Slot有給入，則加上檢查Slot的判斷
                ((Slot < 0) || (int)x.GetField("Slot") == Slot)
            );

            //是否有執行到
            bool doFlag = false;

            MonoBehaviour controller = GetController(chaCtrl);
            if (null == MaterialBackup) {
                MaterialBackup = GetDataFromController(chaCtrl).ToDictionary<string, object>();
            }

            if (null != controller && null != MaterialBackup) {
                for (int i = 0; i < storedValueInfos.Length; i++) {
                    StoredValueInfo storedValue = storedValueInfos[i];
                    bool doFlag2 = false;
                    object target = controller.GetField(storedValue.listName).ToListWithoutType();
                    //移除
                    doFlag2 = target.RemoveAll(predicate) > 0;
                    //加回
                    object obj2Add = MaterialBackup[storedValue.listName].Where(predicate);
                    if (obj2Add.Count() > 0) {
                        doFlag2 = true;

                        target.AddRange(obj2Add);
                    }

                    if (doFlag2) {
                        controller.SetField(storedValue.listName, target);
                        GetDataFromController(chaCtrl).TryGetValue(storedValue.listName, out object val);
                        Logger.LogDebug($"--->{storedValue.className}: { val.Count() }");
                    }

                    doFlag |= doFlag2;
                }

                if (doFlag) {
                    if (objectType == ObjectType.Clothing) {
                        Logger.LogDebug($"-->Material Set: Clothes " + ((Slot >= 0) ? $", {Patches.ClothesKindName[Slot]}" : ", All Clothes"));
                    } else if (objectType == ObjectType.Accessory) {
                        Logger.LogDebug($"-->Material Set: Accessory" + ((Slot >= 0) ? ", Slot " + Slot : ", All Slots"));
                    }
                }
            }
        }

        /// <summary>
        /// 拷貝Material Editor資料
        /// </summary>
        /// <param name="sourceChaCtrl"></param>
        /// <param name="sourceSlot"></param>
        /// <param name="targetSlot"></param>
        /// <param name="gameObject">對象GameObject</param>
        /// <param name="objectType">對象分類</param>
        public void CopyMaterialEditorData(ChaControl sourceChaCtrl, int sourceSlot, int targetSlot, GameObject gameObject, ObjectType objectType) {
            RemoveMaterialEditorData(targetSlot, gameObject, objectType);
            SetMaterialEditorData(sourceChaCtrl, sourceSlot, targetSlot, gameObject, objectType);
        }

        /// <summary>
        /// 移除Material Editor資料
        /// </summary>
        /// <param name="targetSlot"></param>
        /// <param name="gameObject">對象GameObject</param>
        /// <param name="objectType">對象分類</param>
        public void RemoveMaterialEditorData(int targetSlot, GameObject gameObject, ObjectType objectType)
            => RemoveMaterialEditorData(DefaultChaCtrl, targetSlot, gameObject, objectType);
        public void RemoveMaterialEditorData(ChaControl targetChaCtrl, int targetSlot, GameObject gameObject, ObjectType objectType) {
            if (targetChaCtrl != TargetChaCtrl) GetControllerAndBackupData(targetChaCtrl: targetChaCtrl);

            // 轉換ObjectType
            object _objectType = Enum.Parse(ObjectTypeME, Enum.GetName(typeof(ObjectType), objectType));

            //是否有執行到
            bool doFlag = false;

            for (int i = 0; i < storedValueInfos.Length; i++) {
                bool doFlag2 = false;
                StoredValueInfo storedValue = storedValueInfos[i];
                object target = TargetBackup[storedValue.listName].ToListWithoutType();

                //移除
                object objRemoved = target.Where((x) =>
                    (int)x.GetField("ObjectType") == (int)objectType &&
                    (int)x.GetField("CoordinateIndex") == targetChaCtrl.fileStatus.coordinateType &&
                    (int)x.GetField("Slot") == targetSlot
                ).ForEach((x) => {
                    if (null == x) return;
                    doFlag2 = true;

                    Renderer r = null;
                    Material m = null;

                    if (i == 1) {
                        r = MaterialAPI.InvokeStatic("GetRendererList", new object[] { gameObject })?.ToList<Renderer>().Where(y => y.name == (string)x.GetField("RendererName"))?.FirstOrDefault();
                        if (r == null) {
                            doFlag2 = false;
                            return;
                        }
                    } else { 
                        m = MaterialAPI.InvokeStatic("GetObjectMaterials", new object[] { gameObject, (string)x.GetField("MaterialName") })?.ToList<Material>()?.FirstOrDefault();
                        if (m == null) {
                            doFlag2 = false;
                            return;
                        }
                    }

                    switch (i) {
                        case 0: //MaterialShader
                            TargetController.Invoke(storedValue.removeFunctionName, new object[] {
                                targetSlot,
                                _objectType,
                                m,
                                gameObject,
                                true
                            });
                            if (null != x.GetField("RenderQueueOriginal")) {
                                TargetController.Invoke("RemoveMaterialShaderRenderQueue", new object[] {
                                    targetSlot,
                                _objectType,
                                    m,
                                    gameObject,
                                    true
                                });
                            }
                            break;
                        case 1: //RendererProperty
                            TargetController.Invoke(storedValue.removeFunctionName, new object[] {
                                targetSlot,
                                _objectType,
                                r,
                                x.GetField("Property") ,
                                gameObject,
                                true
                            });
                            break;
                        case 2: //MaterialFloatProperty
                        case 3: //MaterialColorProperty
                            TargetController.Invoke(storedValue.removeFunctionName, new object[] {
                                targetSlot,
                                _objectType,
                                m,
                                x.GetField("Property") ,
                                gameObject,
                                true
                            });
                            break;
                        case 4: //MaterialTexture
                            //Offset
                            if (null != x.GetField("OffsetOriginal")) {
                                TargetController.Invoke("RemoveMaterialTextureOffset", new object[] {
                                    targetSlot,
                                _objectType,
                                    m,
                                    x.GetField("Property"),
                                    gameObject,
                                    true
                                });
                            }
                            //Scale
                            if (null != x.GetField("ScaleOriginal")) {
                                TargetController.Invoke("RemoveMaterialTextureScale", new object[] {
                                    targetSlot,
                                _objectType,
                                    m,
                                    x.GetField("Property"),
                                    gameObject,
                                    true
                                });
                            }
                            //Texture
                            TargetController.Invoke(storedValue.removeFunctionName, new object[] {
                                targetSlot,
                                _objectType,
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
                        TargetBackup = GetDataFromController(targetChaCtrl) as Dictionary<string, object>;
                        TargetTextureDictionaryBackup = GetTextureDictionaryFromController(targetChaCtrl);
                        Logger.LogDebug($"--->Remove {objRemoved.Count()} {storedValue.className}");
                    }
                }
                doFlag |= doFlag2;
            }

            if (objectType == ObjectType.Accessory) {
                if (doFlag) {
                    Logger.LogDebug($"-->Remove Material Editor Data: {targetChaCtrl.fileParam.fullname} Slot{targetSlot}");
                } else {
                    //Logger.LogDebug($"-->No Material Editor Backup to remove: {targetChaCtrl.fileParam.fullname} Slot{targetSlot}");
                }
            } else if (objectType == ObjectType.Clothing) {
                if (doFlag) {
                    Logger.LogDebug($"-->Remove Material Editor Data: {targetChaCtrl.fileParam.fullname} {Enum.GetName(typeof(ChaFileDefine.ClothesKind), targetSlot)}");
                } else {
                    //Logger.LogDebug($"-->No Material Editor Backup to remove: {targetChaCtrl.fileParam.fullname} {Enum.GetName(typeof(ChaFileDefine.ClothesKind), targetSlot)}");
                }
            }
        }

        /// <summary>
        /// 寫入Material Editor資料
        /// </summary>
        /// <param name="sourceChaCtrl"></param>
        /// <param name="sourceSlot"></param>
        /// <param name="targetSlot"></param>
        /// <param name="gameObject">對象GameObject</param>
        /// <param name="objectType">對象分類</param>
        private void SetMaterialEditorData(ChaControl sourceChaCtrl, int sourceSlot, int targetSlot, GameObject gameObject, ObjectType objectType)
            => SetMaterialEditorData(sourceChaCtrl, sourceSlot, DefaultChaCtrl, targetSlot, gameObject, objectType);
        private void SetMaterialEditorData(ChaControl sourceChaCtrl, int sourceSlot, ChaControl targetChaCtrl, int targetSlot, GameObject gameObject, ObjectType objectType) {
            if (sourceChaCtrl != SourceChaCtrl) GetControllerAndBackupData(sourceChaCtrl: sourceChaCtrl);
            if (targetChaCtrl != TargetChaCtrl) GetControllerAndBackupData(targetChaCtrl: targetChaCtrl);

            // 轉換ObjectType
            object _objectType = Enum.Parse(ObjectTypeME, Enum.GetName(typeof(ObjectType), objectType));

            if (gameObject?.gameObject != null) {

            }
            //是否有執行到
            bool doFlag = false;

            for (int i = 0; i < storedValueInfos.Length; i++) {
                bool doFlag2 = false;
                StoredValueInfo storedValue = storedValueInfos[i];
                object target = TargetBackup[storedValue.listName].ToListWithoutType();

                //加入
                object objAdded = SourceBackup[storedValue.listName].ToListWithoutType().Where(x =>
                    (int)x.GetField("ObjectType") == (int)objectType &&
                    (int)x.GetField("CoordinateIndex") == sourceChaCtrl.fileStatus.coordinateType &&
                    (int)x.GetField("Slot") == sourceSlot
                ).ForEach((x) => {
                    doFlag2 = true;

                    Renderer r = null;
                    Material m = null;

                    if (i == 1) {
                        r = MaterialAPI.InvokeStatic("GetRendererList", new object[] { gameObject })?.ToList<Renderer>().Where(y => y.name == (string)x.GetField("RendererName"))?.FirstOrDefault();
                        if (r == null) {
                            Logger.LogWarning($"Missing Renderer: {(string)x.GetField("RendererName")}!");
                            doFlag2 = false;
                            return;
                        }
                    } else { 
                        m = MaterialAPI.InvokeStatic("GetObjectMaterials", new object[] { gameObject, (string)x.GetField("MaterialName") })?.ToList<Material>()?.FirstOrDefault();
                        if (m == null) {
                            Logger.LogWarning($"Missing Material: {(string)x.GetField("MaterialName")}!");
                            doFlag2 = false;
                            return;
                        }
                    }

                    switch (i) {
                        case 0: //MaterialShader
                            TargetController.Invoke(storedValue.setFunctionName, new object[] {
                                targetSlot,
                                _objectType,
                                m,
                                x.GetField("ShaderName"),
                                gameObject,
                                true
                            });
                            if (null != x.GetField("RenderQueueOriginal")) {
                                TargetController.Invoke("SetMaterialShaderRenderQueue", new object[] {
                                    targetSlot,
                                _objectType,
                                    m,
                                    x.GetField("RenderQueue"),
                                    gameObject,
                                    true
                                });
                            }
                            break;
                        case 1: //RendererProperty
                            TargetController.Invoke(storedValue.setFunctionName, new object[] {
                                targetSlot,
                                _objectType,
                                r,
                                x.GetField("Property"),
                                x.GetField("Value"),
                                gameObject,
                                true
                            });
                            break;
                        case 2: //MaterialFloatProperty
                            TargetController.Invoke(storedValue.setFunctionName, new object[] {
                                targetSlot,
                                _objectType,
                                m,
                                x.GetField("Property"),
                                (float)Convert.ToDouble(x.GetField("Value")),
                                gameObject,
                                true
                            });
                            break;
                        case 3: //MaterialColorProperty
                            TargetController.Invoke(storedValue.setFunctionName, new object[] {
                                targetSlot,
                                _objectType,
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

                                if (!Directory.Exists(CacheDirectory.FullName)) { MakeCacheDirectory(); }
                                File.WriteAllBytes(tempPath, BA);
                                TargetController.Invoke("SetMaterialTextureFromFile", new object[] {
                                    targetSlot,
                                _objectType,
                                    m,
                                    x.GetField("Property"),
                                    tempPath,
                                    gameObject,
                                    false
                                });

                                File.Delete(tempPath);
                            }
                            if (null != x.GetField("OffsetOriginal")) {
                                //Offset
                                TargetController.Invoke("SetMaterialTextureOffset", new object[] {
                                    targetSlot,
                                _objectType,
                                    m,
                                    x.GetField("Property"),
                                    x.GetField("Offset"),
                                    gameObject,
                                    true
                                });
                            }
                            //Scale
                            if (null != x.GetField("ScaleOriginal")) {
                                TargetController.Invoke("SetMaterialTextureScale", new object[] {
                                    targetSlot,
                                _objectType,
                                    m,
                                    x.GetField("Property"),
                                    x.GetField("Scale"),
                                    gameObject,
                                    true
                                });
                            }
                            break;
                    }
                });

                if (doFlag2) {
                    if (objAdded.Count() > 0) {
                        TargetBackup = GetDataFromController(targetChaCtrl) as Dictionary<string, object>;
                        TargetTextureDictionaryBackup = GetTextureDictionaryFromController(targetChaCtrl);
                        Logger.LogDebug($"--->Set {objAdded.Count()} {storedValue.className}");
                    }
                }
                doFlag |= doFlag2;
            }

            if (objectType == ObjectType.Accessory) {
                if (doFlag) {
                    Logger.LogDebug($"-->Set Material Editor Data: {sourceChaCtrl.fileParam.fullname} Slot{sourceSlot} -> {targetChaCtrl.fileParam.fullname} Slot{targetSlot}");
                } else {
                    //Logger.LogDebug($"-->No Material Editor Backup to set: {sourceChaCtrl.fileParam.fullname} {sourceSlot}");
                }
            } else if (objectType == ObjectType.Clothing) {
                if (doFlag) {
                    Logger.LogDebug($"-->Set Material Editor Data: {sourceChaCtrl.fileParam.fullname} {Enum.GetName(typeof(ChaFileDefine.ClothesKind), sourceSlot)} -> {targetChaCtrl.fileParam.fullname} {Enum.GetName(typeof(ChaFileDefine.ClothesKind), targetSlot)}");
                } else {
                    //Logger.LogDebug($"-->No Material Editor Backup to set: {sourceChaCtrl.fileParam.fullname} {Enum.GetName(typeof(ChaFileDefine.ClothesKind), sourceSlot)}");
                }
            }
        }

        public override bool CheckControllerPrepared(ChaControl chaCtrl)
         => base.CheckControllerPrepared(
             chaCtrl,
             (controller) => !(bool)controller?.GetProperty("CharacterLoading"));

        public new void ClearBackup() {
            base.ClearBackup();
            SourceTextureDictionaryBackup = null;
            TargetTextureDictionaryBackup = null;
        }
    }
}
