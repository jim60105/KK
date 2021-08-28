using ExtensibleSaveFormat;
using Extension;
//using HarmonyLib;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEngine;
using ResolveInfo = Sideloader.AutoResolver.ResolveInfo;

namespace CoordinateLoadOption {
    class MoreAccessories_Support {
        private static readonly BepInEx.Logging.ManualLogSource Logger = CoordinateLoadOption.Logger;
        private static Type MoreAccessories = null;
        private static object MoreAccObj;

        public static bool LoadAssembly() {
            try {
                string path = KoikatuHelper.TryGetPluginInstance("com.joan6694.illusionplugins.moreaccessories", new Version(1, 1))?.Info.Location;
                Assembly ass = Assembly.LoadFrom(path);
                MoreAccessories = ass.GetType("MoreAccessoriesKOI.MoreAccessories");
                MoreAccObj = MoreAccessories?.GetFieldStatic("_self");
                if (null == MoreAccessories || null == MoreAccObj) {
                    throw new Exception("Load assembly FAILED: MoreAccessories");
                }
                Logger.LogDebug("MoreAccessories found");
                return true;
            } catch (Exception ex) {
                Logger.LogDebug(ex.Message);
                return false;
            }
        }

        //private static bool fakeCopyFlag_CopyAll = false;
        //[HarmonyPrefix, HarmonyPatch(typeof(ChaFile), "CopyAll")]
        //public static bool CopyAllPrefix() {
        //    return !fakeCopyFlag_CopyAll;
        //}

        ///// <summary>
        ///// 將所有的MoreAccessories飾品由來源對象複製到目標對象
        ///// </summary>
        ///// <param name="oriChaCtrl">來源對象</param>
        ///// <param name="targetChaCtrl">目標對象</param>
        //public static void CopyAllMoreAccessoriesData(ChaControl oriChaCtrl, ChaControl targetChaCtrl) {
        //    //Do a forced clearing to avoid the broken clearing function added in MoreAcc v1.0.9.
        //    ClearMoreAccessoriesData(targetChaCtrl, true);

        //    fakeCopyFlag_CopyAll = true;
        //    targetChaCtrl.chaFile.CopyAll(oriChaCtrl.chaFile);
        //    fakeCopyFlag_CopyAll = false;

        //    //MoreAccessories.InvokeMember("Update", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, MoreAccObj, null);

        //    Logger.LogDebug($"Copy All MoreAccessories: {oriChaCtrl.fileParam.fullname} -> {targetChaCtrl.fileParam.fullname}");
        //}

        public static void GetExtDataFromPlugin(ChaFile chafile) {
            MoreAccObj.Invoke("OnActualCharaLoad", new object[] { chafile });
        }

        public static void SetExtDataFromPlugin(ChaFile chafile) {
            MoreAccObj.Invoke("OnActualCharaSave", new object[] { chafile });
        }

        /// <summary>
        /// 將MoreAccessories飾品清空
        /// </summary>
        /// <param name="chaCtrl">清空對象</param>
        public static void ClearMoreAccessoriesData(ChaControl chaCtrl, bool force = false) {
            MoreAccObj.GetField("_accessoriesByChar").TryGetValue(chaCtrl.chaFile, out object charAdditionalData);
            charAdditionalData.GetField("rawAccessoriesInfos").ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>()
                .TryGetValue((ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType, out List<ChaFileAccessory.PartsInfo> parts);
            for (int i = 0; i < parts.Count; i++) {
                if (force || !(CoordinateLoad.IsHairAccessory(chaCtrl, i + 20) && Patches.lockHairAcc)) {
                    parts[i] = new ChaFileAccessory.PartsInfo();
                } else {
                    Logger.LogDebug($"Keep HairAcc{i}: {parts[i].id}");
                }
            }
            RemoveEmptyFromBackToFront(parts, 0);
            //charAdditionalData.SetField("rawAccessoriesInfos", rawAccessoriesInfos);
            //charAdditionalData.SetField("nowAccessories", rawAccessoriesInfos[(ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType]);

            //MoreAccObj.SetField("_accessoriesByChar", _accessoriesByChar);

            try {
                //MoreAccessories.InvokeMember("UpdateStudioUI", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, MoreAccObj, null);
                Update();
                chaCtrl.ChangeAccessory(true);
            } catch { }
            Logger.LogDebug("Clear MoreAccessories Finish");
        }

        /// <summary>
        /// 由sourceChaCtrl拷貝MoreAccessories至targetChaCtrl
        /// </summary>
        /// <param name="sourceChaCtrl">來源</param>
        /// <param name="targetChaCtrl">目標</param>
        public static void CopyMoreAccessories(ChaControl sourceChaCtrl, ChaControl targetChaCtrl) {
            Queue<int> accQueue = new Queue<int>();
            MoreAccObj.GetField("_accessoriesByChar").TryGetValue(sourceChaCtrl.chaFile, out object sourceCharAdditionalData);
            MoreAccObj.GetField("_accessoriesByChar").TryGetValue(targetChaCtrl.chaFile, out object targetCharAdditionalData);

            sourceCharAdditionalData.GetField("rawAccessoriesInfos").ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>()
                .TryGetValue((ChaFileDefine.CoordinateType)sourceChaCtrl.fileStatus.coordinateType, out List<ChaFileAccessory.PartsInfo> sourceParts);
            Dictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>> rawAccInfos = targetCharAdditionalData.GetField("rawAccessoriesInfos").ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>();
            rawAccInfos.TryGetValue((ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType, out List<ChaFileAccessory.PartsInfo> targetParts);

            while (targetParts.Count < sourceParts.Count) {
                targetParts.Add(new ChaFileAccessory.PartsInfo());
            }
            ChaFileAccessory.PartsInfo[] sourcePartsArray = sourceChaCtrl.nowCoordinate.accessory.parts.Concat(sourceParts).ToArray();
            ChaFileAccessory.PartsInfo[] targetPartsArray = targetChaCtrl.nowCoordinate.accessory.parts.Concat(targetParts).ToArray();

            Logger.LogDebug($"MoreAcc Source Count : {sourcePartsArray.Length}");
            Logger.LogDebug($"MoreAcc Target Count : {targetPartsArray.Length}");

            CoordinateLoad.ChangeAccessories(sourceChaCtrl, sourcePartsArray, targetChaCtrl, targetPartsArray, accQueue);

            targetParts.Clear();
            targetParts.AddRange(targetPartsArray);

            //遍歷空欄dequeue accQueue
            while (accQueue.Count > 0) {
                targetParts.Add(MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(
                    MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(sourcePartsArray[accQueue.Dequeue()])
                ));
                Logger.LogDebug($"->DeQueue: MoreAcc{targetParts.Count - 1} / Part: {(ChaListDefine.CategoryNo)targetParts.Last().type} / ID: {targetParts.Last().id}");
            }

            //由後往前刪除空欄
            RemoveEmptyFromBackToFront(targetParts);
            Logger.LogDebug($"MoreAcc Finish Count : {targetParts.Count}");

            targetChaCtrl.nowCoordinate.accessory.parts = targetParts.Take(20).ToArray();
            targetParts.RemoveRange(0, 20);
            Logger.LogDebug($"targetParts Count : {targetParts.Count}");

            //資料寫入MoreAcc
            List<ListInfoBase> infoAccessory = targetCharAdditionalData.GetField("infoAccessory").ToList<ListInfoBase>();
            List<GameObject> objAccessory = targetCharAdditionalData.GetField("objAccessory").ToList<GameObject>();
            List<GameObject[]> objAcsMove = targetCharAdditionalData.GetField("objAcsMove").ToList<GameObject[]>();
            List<ChaAccessoryComponent> cusAcsCmp = targetCharAdditionalData.GetField("cusAcsCmp").ToList<ChaAccessoryComponent>();
            List<bool> showAccessories = targetCharAdditionalData.GetField("showAccessories").ToList<bool>();

            while (infoAccessory.Count < targetParts.Count)
                infoAccessory.Add(null);
            while (objAccessory.Count < targetParts.Count)
                objAccessory.Add(null);
            while (objAcsMove.Count < targetParts.Count)
                objAcsMove.Add(new GameObject[2]);
            while (cusAcsCmp.Count < targetParts.Count)
                cusAcsCmp.Add(null);
            while (showAccessories.Count < targetParts.Count)
                showAccessories.Add(true);

            //targetCharAdditionalData.SetField("nowAccessories", targetParts);
            targetCharAdditionalData.SetField("infoAccessory", infoAccessory);
            targetCharAdditionalData.SetField("objAccessory", objAccessory);
            targetCharAdditionalData.SetField("objAcsMove", objAcsMove);
            targetCharAdditionalData.SetField("cusAcsCmp", cusAcsCmp);
            targetCharAdditionalData.SetField("showAccessories", showAccessories);

            //_accessoriesByChar[targetChaCtrl.chaFile] = targetCharAdditionalData;
            //MoreAccObj.SetField("_accessoriesByChar", _accessoriesByChar);

            Logger.LogDebug($"->MoreAcc Parts Count: {GetAccessoriesAmount(targetChaCtrl.chaFile)}");

            Logger.LogDebug("Load MoreAccessories Finish");
        }

        private static PluginData sideLoaderExtData;

        /// <summary>
        /// 讀取MoreAccessories
        /// </summary>
        /// <param name="chaFileCoordinate">讀取的衣裝對象</param>
        /// <returns></returns>
        public static string[] LoadMoreAcc(ChaFileCoordinate chaFileCoordinate) {
            List<ChaFileAccessory.PartsInfo> tempLoadedAccessories = new List<ChaFileAccessory.PartsInfo>();

            typeof(Sideloader.AutoResolver.UniversalAutoResolver).GetNestedType("Hooks", BindingFlags.NonPublic).InvokeStatic("ExtendedCoordinateLoad", new object[] { chaFileCoordinate });

            //本地Info
            List<ResolveInfo> LoadedResolutionInfoList = Sideloader.AutoResolver.UniversalAutoResolver.LoadedResolutionInfo?.ToList();
            //讀取Sideloader extData
            List<ResolveInfo> extInfoList;
            sideLoaderExtData = ExtendedSave.GetExtendedDataById(chaFileCoordinate, "com.bepis.sideloader.universalautoresolver");
            if (sideLoaderExtData == null || !sideLoaderExtData.data.ContainsKey("info")) {
                Logger.LogDebug("No sideloader extInfo found");
                extInfoList = null;
            } else {
                object[] tmpExtInfo = (object[])sideLoaderExtData.data["info"];
                extInfoList = tmpExtInfo.Select(x => MessagePackSerializer.Deserialize<ResolveInfo>((byte[])x)).ToList();
            }

            XmlNode node = null;
            PluginData pluginData = ExtendedSave.GetExtendedDataById(chaFileCoordinate, "moreAccessories");
            if (pluginData != null && pluginData.data.TryGetValue("additionalAccessories", out object xmlData)) {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml((string)xmlData);
                node = doc.FirstChild;
            }

            if (node != null) {
                foreach (XmlNode accessoryNode in node.ChildNodes) {
                    ChaFileAccessory.PartsInfo part = new ChaFileAccessory.PartsInfo {
                        type = XmlConvert.ToInt32(accessoryNode.Attributes["type"].Value)
                    };
                    if (part.type != 120) {
                        part.id = XmlConvert.ToInt32(accessoryNode.Attributes["id"].Value);
                        part.parentKey = accessoryNode.Attributes["parentKey"].Value;

                        for (int i = 0; i < 2; i++) {
                            for (int j = 0; j < 3; j++) {
                                part.addMove[i, j] = new Vector3 {
                                    x = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}x"].Value),
                                    y = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}y"].Value),
                                    z = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}z"].Value)
                                };
                            }
                        }
                        for (int i = 0; i < 4; i++) {
                            part.color[i] = new Color {
                                r = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}r"].Value),
                                g = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}g"].Value),
                                b = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}b"].Value),
                                a = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}a"].Value)
                            };
                        }
                        part.hideCategory = XmlConvert.ToInt32(accessoryNode.Attributes["hideCategory"].Value);

                        //Only Darkness has this
                        if (null != part.GetType().GetProperty("noShake")) {
                            part.SetProperty("noShake", accessoryNode.Attributes["noShake"] != null && XmlConvert.ToBoolean(accessoryNode.Attributes["noShake"].Value));
                        }

                        //處理Sideloader mod
                        if (null != extInfoList && null != LoadedResolutionInfoList) {
                            ResolveInfo tmpExtInfo = extInfoList.FirstOrDefault(x => x.CategoryNo == (ChaListDefine.CategoryNo)part.type && x.Slot == part.id);
                            if (default(ResolveInfo) != tmpExtInfo) {
                                ResolveInfo localExtInfo = LoadedResolutionInfoList.FirstOrDefault(x => x.GUID == tmpExtInfo.GUID && x.CategoryNo == tmpExtInfo.CategoryNo && x.Slot == tmpExtInfo.Slot);
                                if (default(ResolveInfo) != localExtInfo) {
                                    Logger.LogDebug($"Resolve {localExtInfo.GUID}: {localExtInfo.Slot} -> {localExtInfo.LocalSlot}");
                                    part.id = localExtInfo.LocalSlot;
                                }
                            }
                        }
                    }
                    tempLoadedAccessories.Add(part);
                }
            }

            return tempLoadedAccessories.Select(x => CoordinateLoad.GetNameFromIDAndType(x.id, (ChaListDefine.CategoryNo)x.type)).ToArray();
        }

        /// <summary>
        /// 由後往前刪除空欄
        /// </summary>
        /// <param name="partsInfos"></param>
        public static void RemoveEmptyFromBackToFront(List<ChaFileAccessory.PartsInfo> partsInfos, int lowerLimit = 20) {
            for (int i = partsInfos.Count - 1; i >= lowerLimit; i--) {
                if (partsInfos[i].type == 120) {
                    partsInfos.RemoveAt(i);
                } else {
                    break;
                }
            }
        }

        /// <summary>
        /// 從MoreAccessories取得ChaAccessoryComponent
        /// </summary>
        /// <param name="chaCtrl">對象角色</param>
        /// <param name="index">飾品欄位index</param>
        /// <returns></returns>
        public static ChaAccessoryComponent GetChaAccessoryComponent(ChaControl chaCtrl, int index) {
            return (ChaAccessoryComponent)MoreAccObj.Invoke("GetChaAccessoryComponent", new object[] { chaCtrl, index });
        }

        /// <summary>
        /// 取得飾品數量
        /// </summary>
        /// <param name="chaFile"></param>
        /// <returns></returns>
        public static int GetAccessoriesAmount(ChaFile chaFile) {
            return MoreAccObj.GetField("_accessoriesByChar").TryGetValue(chaFile, out object _acc) ?
                _acc.GetField("nowAccessories").ToList<ChaFileAccessory.PartsInfo>().Count + 20
                : 20;
        }

        public static void Update() => MoreAccObj.Invoke("UpdateUI");
    }
}
