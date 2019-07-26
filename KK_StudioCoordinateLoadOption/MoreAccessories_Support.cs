using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using Extension;
using Harmony;
using MessagePack;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_StudioCoordinateLoadOption {
    class MoreAccessories_Support {
        private static Type MoreAccessories = null;
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

            Logger.Log(LogLevel.Debug, "[KK_SCLO] Copy MoreAccessories Finish(1)");
        }

        /// <summary>
        /// 將MoreAccessories飾品，依照傳入的布林陣列，由來源對象複製到目標對象
        /// </summary>
        /// <param name="oriChaCtrl">來源對象</param>
        /// <param name="targetChaCtrl">目標對象</param>
        /// <param name="coordinateType">CoordinateType</param>
        /// <param name="bools">布林陣列，True為複製</param>
        public static void CopyMoreAccessoriesData(ChaControl oriChaCtrl, ChaControl targetChaCtrl, ChaFileDefine.CoordinateType coordinateType, bool[] bools) {
            //如果全選，就調用fakeCopy，用意在清除比選取之服裝更多的飾品
            bool isAllTrueFlag = true;
            bool isAllFalseFlag = true;
            foreach (var b in bools) {
                isAllTrueFlag &= b;
                isAllFalseFlag &= !b;
            }
            if (isAllTrueFlag && accQueue.Count == 0 && !Patches.excludeHairAcc) {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Copy MoreAccessories All True");
                CopyMoreAccessoriesData(oriChaCtrl, targetChaCtrl);
                return;
            }
            if (isAllFalseFlag && accQueue.Count == 0 && !Patches.excludeHairAcc) {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Copy MoreAccessories All False");
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Copy MoreAccessories Finish (2)");
                return;
            }

            bools = bools.Skip(20).ToArray();

            object MoreAccObj = MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null);
            Dictionary<ChaFile, object> _accessoriesByChar = MoreAccObj.GetField("_accessoriesByChar").ToDictionary<ChaFile, object>();
            _accessoriesByChar.TryGetValue(oriChaCtrl.chaFile, out var oriCharAdditionalData);
            oriCharAdditionalData.GetField("rawAccessoriesInfos").ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>().TryGetValue(coordinateType, out List<ChaFileAccessory.PartsInfo> oriParts);
            _accessoriesByChar.TryGetValue(targetChaCtrl.chaFile, out var targetCharAdditionalData);
            targetCharAdditionalData.GetField("rawAccessoriesInfos").ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>().TryGetValue(coordinateType, out List<ChaFileAccessory.PartsInfo> targetParts);
            Logger.Log(LogLevel.Debug, $"[KK_SCLO] MoreAcc Bools Count : {bools.Length}");
            Logger.Log(LogLevel.Debug, $"[KK_SCLO] MoreAcc TargetParts Count : {targetParts.Count}");
            Logger.Log(LogLevel.Debug, $"[KK_SCLO] MoreAcc OriParts Count : {oriParts.Count}");

            for (int i = 0; i < oriParts.Count || i < bools.Length; i++) {
                var tmp = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(oriParts.ElementAtOrDefault(i) ?? new ChaFileAccessory.PartsInfo());
                //倒回時，如果遇到原始是頭髮飾品，就強制替換入該位置，並把原來佔位的飾品EnQueue
                if (Patches.IsHairAccessory(oriChaCtrl, i + 20) && !bools[i]) {
                    accQueue.Enqueue(MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(targetParts[i] ?? new ChaFileAccessory.PartsInfo()));
                    targetParts[i] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tmp);
                    Logger.Log(LogLevel.Debug, $"[KK_SCLO] ->HairLock: MoreAcc{i} / ID: {oriParts.ElementAtOrDefault(i)?.id ?? 0}");
                    Logger.Log(LogLevel.Debug, $"[KK_SCLO] ->EnQueue: MoreAcc{i} / ID: {targetParts[i].id}");
                } else if (i > bools.Length - 1) {
                    //超過原本數量，就改用Add
                    targetParts.Add(MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tmp));
                } else if (bools[i]) {
                    targetParts[i] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tmp);
                }
                Logger.Log(LogLevel.Debug, $"[KK_SCLO] ->Change: MoreAcc{i} / ID: {targetParts[i].id}");
            }
            //遍歷空欄，寫入暫存在accQueue的飾品
            for (int j = 0; accQueue.Count > 0; j++) {
                if (j < targetParts.Count - 1) {
                    if (targetParts[j].type == 120) {
                        targetParts[j] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(accQueue.Dequeue());
                        Logger.Log(LogLevel.Debug, $"[KK_SCLO] ->DeQueue: MoreAcc{j} / ID: {targetParts[j].id} (1)");
                    }   //else continue;
                } else {
                    targetParts.Add(MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(accQueue.Dequeue()));
                    Logger.Log(LogLevel.Debug, $"[KK_SCLO] ->DeQueue: MoreAcc{j} / ID: {targetParts[j].id} (2)");
                }
            }

            Dictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>> rawAccessoriesInfos = targetCharAdditionalData.GetField("rawAccessoriesInfos").ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>();
            rawAccessoriesInfos[coordinateType] = targetParts;
            targetCharAdditionalData.SetField("rawAccessoriesInfos", rawAccessoriesInfos);

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

            targetCharAdditionalData.SetField("infoAccessory", infoAccessory);
            //Logger.Log(LogLevel.Debug, "[KK_SCLO] infoAccessory Count: "+infoAccessory.Count);
            targetCharAdditionalData.SetField("objAccessory", objAccessory);
            //Logger.Log(LogLevel.Debug, "[KK_SCLO] objAccessory Count: "+objAccessory.Count);
            targetCharAdditionalData.SetField("objAcsMove", objAcsMove);
            //Logger.Log(LogLevel.Debug, "[KK_SCLO] objAcsMove Count: "+objAcsMove.Count);
            targetCharAdditionalData.SetField("cusAcsCmp", cusAcsCmp);
            //Logger.Log(LogLevel.Debug, "[KK_SCLO] cusAcsCmp Count: "+cusAcsCmp.Count);
            targetCharAdditionalData.SetField("showAccessories", showAccessories);
            //Logger.Log(LogLevel.Debug, "[KK_SCLO] showAccessories Count: "+showAccessories.Count);
            targetCharAdditionalData.SetField("nowAccessories", targetParts);

            //_accessoriesByChar[targetChaCtrl.chaFile] = targetCharAdditionalData;
            MoreAccObj.SetField("_accessoriesByChar", _accessoriesByChar);
            MoreAccessories.InvokeMember("Update", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, MoreAccObj, null);

            Logger.Log(LogLevel.Debug, "[KK_SCLO] Copy MoreAccessories Finish (3)");
        }

        /// <summary>
        /// 讀取MoreAccessories的名稱
        /// </summary>
        /// <param name="tmpChaFileCoordinate">讀取的衣裝對象</param>
        /// <returns></returns>
        public static string[] LoadMoreAccNames(ChaFileCoordinate tmpChaFileCoordinate) {
            MoreAccessories.InvokeMember("Update",
                BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null),
                null);
            List<ChaFileAccessory.PartsInfo> nowAccessories = new List<ChaFileAccessory.PartsInfo>();

            XmlNode node = null;
            PluginData pluginData = ExtendedSave.GetExtendedDataById(tmpChaFileCoordinate, "moreAccessories");
            if (pluginData != null && pluginData.data.TryGetValue("additionalAccessories", out object xmlData)) {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml((string)xmlData);
                node = doc.FirstChild;
            }
            if (node != null) {
                foreach (XmlNode accessoryNode in node.ChildNodes) {
                    ChaFileAccessory.PartsInfo part = new ChaFileAccessory.PartsInfo();
                    part.type = XmlConvert.ToInt32(accessoryNode.Attributes["type"].Value);
                    if (part.type != 120) {
                        part.id = XmlConvert.ToInt32(accessoryNode.Attributes["id"].Value);
                        part.parentKey = accessoryNode.Attributes["parentKey"].Value;
                    }
                    nowAccessories.Add(part);
                }
            }

            List<string> result = nowAccessories.Select(x => Patches.GetNameFromID(x.id, (ChaListDefine.CategoryNo)x.type)).ToList();

            //由後往前刪除空欄
            for (int i = result.Count - 1; i >= 0; i--) {
                if (result[i] == Patches.emptyWord) {
                    result.RemoveAt(i);
                } else {
                    break;
                }
            }

            return result.ToArray();
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
    }
}
