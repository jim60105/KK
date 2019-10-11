using BepInEx.Logging;
using ExtensibleSaveFormat;
using Extension;
using Harmony;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEngine;
using Logger = BepInEx.Logger;
using ResolveInfo = Sideloader.AutoResolver.ResolveInfo;

namespace KK_StudioCoordinateLoadOption {
    class MoreAccessories_Support {
        internal static Type MoreAccessories = null;
        internal static Queue<byte[]> accQueue = new Queue<byte[]>();

        public static bool LoadAssembly() {
            try {
                Assembly ass = Assembly.LoadFrom("BepInEx/MoreAccessories.dll");
                MoreAccessories = ass.GetType("MoreAccessoriesKOI.MoreAccessories");
                //CharAdditionalData = MoreAccessories.GetNestedType("CharAdditionalData");
                if (null == MoreAccessories) {
                    throw new Exception("Load assembly FAILED: MoreAccessories");
                }
                Logger.Log(LogLevel.Debug, "[KK_SCLO] MoreAccessories found");
                return true;
            } catch (Exception ex) {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] " + ex.Message);
                return false;
            }
        }

        private static bool fakeCopyCall = false;
        [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), "CopyAll")]
        public static bool CopyAllPrefix() {
            //Logger.Log(LogLevel.Debug, "[KK_SCLO] Block Origin Copy?:"+fakeCopyCall);
            return !fakeCopyCall;
        }

        /// <summary>
        /// 將所有的MoreAccessories飾品由來源對象複製到目標對象
        /// </summary>
        /// <param name="oriChaCtrl">來源對象</param>
        /// <param name="targetChaCtrl">目標對象</param>
        public static void CopyMoreAccessoriesData(ChaControl oriChaCtrl, ChaControl targetChaCtrl) {
            fakeCopyCall = true;
            targetChaCtrl.chaFile.CopyAll(oriChaCtrl.chaFile);
            fakeCopyCall = false;

            MoreAccessories.InvokeMember("Update",
                BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null),
                null);

            Logger.Log(LogLevel.Debug, "[KK_SCLO] Copy MoreAccessories Finish");
        }

        /// <summary>
        /// 將MoreAccessories飾品清空
        /// </summary>
        /// <param name="chaCtrl">清空對象</param>
        public static void ClearMoreAccessoriesData(ChaControl chaCtrl) {
            object MoreAccObj = MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null);
            Dictionary<ChaFile, object> _accessoriesByChar = MoreAccObj.GetField("_accessoriesByChar").ToDictionary<ChaFile, object>();
            _accessoriesByChar.TryGetValue(chaCtrl.chaFile, out var charAdditionalData);
            charAdditionalData.GetField("rawAccessoriesInfos").ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>().TryGetValue((ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType, out List<ChaFileAccessory.PartsInfo> parts);
            for (int i = 0; i < parts.Count; i++) {
                if (!Patches.IsHairAccessory(chaCtrl, i)) {
                    parts[i] = new ChaFileAccessory.PartsInfo();
                }
            }
            //charAdditionalData.SetField("rawAccessoriesInfos", rawAccessoriesInfos);
            //charAdditionalData.SetField("nowAccessories", rawAccessoriesInfos[(ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType]);

            //MoreAccObj.SetField("_accessoriesByChar", _accessoriesByChar);
            MoreAccessories.InvokeMember("UpdateStudioUI", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, MoreAccObj, null);

            Logger.Log(LogLevel.Debug, "[KK_SCLO] Clear MoreAccessories Finish");
        }

        /// <summary>
        /// 由已選擇的服裝卡數據載入MoreAccessories
        /// </summary>
        /// <param name="chaCtrl">對象</param>
        /// <param name="coordinateType">CoordinateType</param>
        /// <param name="bools">布林陣列，True為複製</param>
        public static void LoadMoreAccFromCoodrinate(ChaControl chaCtrl, ChaFileDefine.CoordinateType coordinateType, bool[] bools) {
            bool isAllFalseFlag = true;
            foreach (var b in bools) {
                isAllFalseFlag &= !b;
            }
            if (isAllFalseFlag && accQueue.Count == 0) {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Load MoreAccessories All False");
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Load MoreAccessories Finish (1)");
                return;
            }

            bools = bools.Skip(20).ToArray();

            object MoreAccObj = MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null);
            Dictionary<ChaFile, object> _accessoriesByChar = MoreAccObj.GetField("_accessoriesByChar").ToDictionary<ChaFile, object>();
            _accessoriesByChar.TryGetValue(chaCtrl.chaFile, out var charAdditionalData);
            charAdditionalData.GetField("rawAccessoriesInfos").ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>().TryGetValue(coordinateType, out List<ChaFileAccessory.PartsInfo> parts);

            Logger.Log(LogLevel.Debug, $"[KK_SCLO] MoreAcc Bools Count : {bools.Length}");
            Logger.Log(LogLevel.Debug, $"[KK_SCLO] MoreAcc TempLoadedAcc Count : {tempLoadedAccessories.Count}");
            Logger.Log(LogLevel.Debug, $"[KK_SCLO] MoreAcc OriginalParts Count : {parts.Count}");
            for (int i = 0; i < tempLoadedAccessories.Count; i++) {
                ChaFileAccessory.PartsInfo tempLoadedAccessory = tempLoadedAccessories.ElementAtOrDefault(i);

                var tempSerlz = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(tempLoadedAccessory ?? new ChaFileAccessory.PartsInfo());
                //如果該位置為髮飾品、增加模式，且在舊飾品數量內，則Enqueue
                if (i < parts.Count && (Patches.IsHairAccessory(chaCtrl, i + 20) || Patches.addAccModeFlag)) {
                    accQueue.Enqueue(tempSerlz);
                    //parts[i] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(ori);
                    Logger.Log(LogLevel.Debug, $"[KK_SCLO] ->Lock: MoreAcc{i} / ID: {parts.ElementAtOrDefault(i)?.id ?? 0}");
                    Logger.Log(LogLevel.Debug, $"[KK_SCLO] ->EnQueue: MoreAcc{i} / ID: {tempLoadedAccessory.id}");
                } else if (i > parts.Count - 1) {
                    //超過原本數量，就改用Add
                    parts.Add(MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tempSerlz));
                } else if (bools[i]) {
                    parts[i] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tempSerlz);
                }
                Logger.Log(LogLevel.Debug, $"[KK_SCLO] ->Changed: MoreAcc{i} / ID: {parts[i].id}");
            }
            //遍歷空欄，寫入暫存在accQueue的飾品
            for (int j = 0; accQueue.Count > 0; j++) {
                if (j < parts.Count - 1) {
                    if (parts[j].type == 120) {
                        parts[j] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(accQueue.Dequeue());
                        Logger.Log(LogLevel.Debug, $"[KK_SCLO] ->DeQueue: MoreAcc{j} / ID: {parts[j].id} (1)");
                    }   //else continue;
                } else {
                    parts.Add(MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(accQueue.Dequeue()));
                    Logger.Log(LogLevel.Debug, $"[KK_SCLO] ->DeQueue: MoreAcc{j} / ID: {parts[j].id} (2)");
                }
            }

            //由後往前刪除空欄
            for (int i = parts.Count - 1; i >= 0; i--) {
                if (parts[i].type == 120) {
                    parts.RemoveAt(i);
                } else {
                    break;
                }
            }

            //資料寫入MoreAcc
            List<ListInfoBase> infoAccessory = charAdditionalData.GetField("infoAccessory").ToList<ListInfoBase>();
            List<GameObject> objAccessory = charAdditionalData.GetField("objAccessory").ToList<GameObject>();
            List<GameObject[]> objAcsMove = charAdditionalData.GetField("objAcsMove").ToList<GameObject[]>();
            List<ChaAccessoryComponent> cusAcsCmp = charAdditionalData.GetField("cusAcsCmp").ToList<ChaAccessoryComponent>();
            List<bool> showAccessories = charAdditionalData.GetField("showAccessories").ToList<bool>();

            while (infoAccessory.Count < parts.Count)
                infoAccessory.Add(null);
            while (objAccessory.Count < parts.Count)
                objAccessory.Add(null);
            while (objAcsMove.Count < parts.Count)
                objAcsMove.Add(new GameObject[2]);
            while (cusAcsCmp.Count < parts.Count)
                cusAcsCmp.Add(null);
            while (showAccessories.Count < parts.Count)
                showAccessories.Add(true);

            //charAdditionalData.SetField("nowAccessories", parts);
            //charAdditionalData.SetField("infoAccessory", infoAccessory);
            //charAdditionalData.SetField("objAccessory", objAccessory);
            //charAdditionalData.SetField("objAcsMove", objAcsMove);
            //charAdditionalData.SetField("cusAcsCmp", cusAcsCmp);
            //charAdditionalData.SetField("showAccessories", showAccessories);

            //_accessoriesByChar[chaCtrl.chaFile] = charAdditionalData;
            //MoreAccObj.SetField("_accessoriesByChar", _accessoriesByChar);
            MoreAccessories.InvokeMember("Update", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, MoreAccObj, null);

            Logger.Log(LogLevel.Debug, $"[KK_SCLO] MoreAcc Parts Count : {parts.Count}");
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Load MoreAccessories Finish (2)");
        }

        private static readonly List<ChaFileAccessory.PartsInfo> tempLoadedAccessories = new List<ChaFileAccessory.PartsInfo>();
        private static PluginData sideLoaderExtData;
        /// <summary>
        /// 讀取MoreAccessories
        /// </summary>
        /// <param name="chaFileCoordinate">讀取的衣裝對象</param>
        /// <returns></returns>
        public static string[] LoadMoreAcc(ChaFileCoordinate chaFileCoordinate) {
            tempLoadedAccessories.Clear();

            typeof(Sideloader.AutoResolver.UniversalAutoResolver).GetNestedType("Hooks", BindingFlags.NonPublic).Invoke("ExtendedCoordinateLoad", new object[] { chaFileCoordinate });

            //本地Info
            List<ResolveInfo> LoadedResolutionInfoList = Sideloader.AutoResolver.UniversalAutoResolver.LoadedResolutionInfo?.ToList();
            //讀取Sideloader extData
            List<ResolveInfo> extInfoList;
            sideLoaderExtData = ExtendedSave.GetExtendedDataById(chaFileCoordinate, "com.bepis.sideloader.universalautoresolver");
            if (sideLoaderExtData == null || !sideLoaderExtData.data.ContainsKey("info")) {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] No sideloader extInfo found");
                extInfoList = null;
            } else {
                var tmpExtInfo = (object[])sideLoaderExtData.data["info"];
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
                        part.noShake = (accessoryNode.Attributes["noShake"] != null && XmlConvert.ToBoolean(accessoryNode.Attributes["noShake"].Value));

                        //處理Sideloader mod
                        if (null != extInfoList && null != LoadedResolutionInfoList) {
                            ResolveInfo tmpExtInfo = extInfoList.FirstOrDefault(x => x.CategoryNo == (ChaListDefine.CategoryNo)part.type && x.Slot == part.id);
                            if (default(ResolveInfo) != tmpExtInfo) {
                                ResolveInfo localExtInfo = LoadedResolutionInfoList.FirstOrDefault(x => x.GUID == tmpExtInfo.GUID && x.CategoryNo == tmpExtInfo.CategoryNo && x.Slot == tmpExtInfo.Slot);
                                if (default(ResolveInfo) != localExtInfo) {
                                    Logger.Log(LogLevel.Debug, $"[KK_SCLO] Resolve {localExtInfo.GUID}: {localExtInfo.Slot} -> {localExtInfo.LocalSlot}");
                                    part.id = localExtInfo.LocalSlot;
                                }
                            }
                        }
                    }
                    tempLoadedAccessories.Add(part);
                }
            }

            //由後往前刪除空欄
            for (int i = tempLoadedAccessories.Count - 1; i >= 0; i--) {
                if (tempLoadedAccessories[i].type == 120) {
                    tempLoadedAccessories.RemoveAt(i);
                } else {
                    break;
                }
            }

            return tempLoadedAccessories.Select(x => Patches.GetNameFromIDAndType(x.id, (ChaListDefine.CategoryNo)x.type)).ToArray();
        }

        /// <summary>
        /// 從MoreAccessories取得ChaAccessoryComponent
        /// </summary>
        /// <param name="chaCtrl">對象角色</param>
        /// <param name="index">飾品欄位index</param>
        /// <returns></returns>
        public static ChaAccessoryComponent GetChaAccessoryComponent(ChaControl chaCtrl, int index) {
            return (ChaAccessoryComponent)MoreAccessories.InvokeMember("GetChaAccessoryComponent",
                BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
                null,
                MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null),
                new object[] { chaCtrl, index });
        }

        public static int GetAccessoriesAmount(ChaFile chaFile) {
            (MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null))
            .GetField("_accessoriesByChar")
            .ToDictionary<ChaFile, object>()
            .TryGetValue(chaFile, out var charAdditionalData);
            return charAdditionalData?.GetField("nowAccessories").ToList<ChaFileAccessory.PartsInfo>().Count + 20 ?? 20;
        }
    }
}
