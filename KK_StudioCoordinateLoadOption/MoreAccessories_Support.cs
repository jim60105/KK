using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using Harmony;
using Logger = BepInEx.Logger;
using Extension;
using MessagePack;
using System.Collections;
using UnityEngine;
using Sideloader.AutoResolver;

namespace KK_StudioCoordinateLoadOption {
    class MoreAccessories_Support {
        private static Type MoreAccessories = null;
        private static Type CharAdditionalData = null;

        public static bool LoadAssembly() {
            try {
                Assembly ass = Assembly.LoadFrom("BepInEx/MoreAccessories.dll");
                MoreAccessories = ass.GetType("MoreAccessoriesKOI.MoreAccessories");
                CharAdditionalData = MoreAccessories.GetNestedType("CharAdditionalData");
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

        public static void CopyMoreAccessoriesData(ChaFile oriChaFile, ChaFile targetChaFile) {
            fakeCopyCall = true;
            targetChaFile.CopyAll(oriChaFile);
            fakeCopyCall = false;

            MoreAccessories.InvokeMember("Update",
                BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null),
                null);

            Logger.Log(LogLevel.Debug, "[KK_SCLO] Copy MoreAccessories Finish(1)");
        }

        public static void CopyMoreAccessoriesData(ChaFile oriChaFile, ChaFile targetChaFile, ChaFileDefine.CoordinateType coordinateType, bool[] bools) {
            //如果全選，就調用fakeCopy，用意在清除比選取之服裝更多的飾品
            bool isAllTrueFlag = true;
            bool isAllFalseFlag = true;
            foreach (var b in bools) {
                isAllTrueFlag &= b;
                isAllFalseFlag &= !b;
            }
            if (isAllTrueFlag) {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Copy MoreAccessories All True");
                CopyMoreAccessoriesData(oriChaFile, targetChaFile);
                return;
            }
            if (isAllFalseFlag) {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Copy MoreAccessories All False");
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Copy MoreAccessories Finish(2)");
                return;
            }

            

            object MoreAccObj = MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null);
            Dictionary<ChaFile,object> _accessoriesByChar = MoreAccObj.GetField("_accessoriesByChar").ToDictionary<ChaFile,object>();
            _accessoriesByChar.TryGetValue(oriChaFile, out var oriCharAdditionalData);
            oriCharAdditionalData.GetField("rawAccessoriesInfos").ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>().TryGetValue(coordinateType, out List<ChaFileAccessory.PartsInfo> oriParts);
            _accessoriesByChar.TryGetValue(targetChaFile, out var targetCharAdditionalData);
            targetCharAdditionalData.GetField("rawAccessoriesInfos").ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>().TryGetValue(coordinateType, out List<ChaFileAccessory.PartsInfo> targetParts);
            Logger.Log(LogLevel.Debug, $"[KK_SCLO] Flag Bools Count : {bools.Length}");
            Logger.Log(LogLevel.Debug, $"[KK_SCLO] Flag TargetParts Count : {targetParts.Count}");
            Logger.Log(LogLevel.Debug, $"[KK_SCLO] Flag OriParts Count : {oriParts.Count}");

            for (int i = 0; i < bools.Length; i++) {
                if (bools[i]) {
                    var tmp = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(oriParts.ElementAtOrDefault(i) ?? new ChaFileAccessory.PartsInfo());
                    if (i > targetParts.Count - 1) {
                        targetParts.Add(MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tmp));
                    } else {
                        targetParts[i] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tmp);
                    }
                } else {
                    if (i > targetParts.Count - 1) {
                        targetParts.Add(new ChaFileAccessory.PartsInfo());
                    }
                }
                Logger.Log(LogLevel.Debug, $"[KK_SCLO] -> MoreAcc {i} id : {targetParts[i].id}");
            }

            Dictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>> rawAccessoriesInfos = targetCharAdditionalData.GetField("rawAccessoriesInfos").ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>();
            rawAccessoriesInfos[coordinateType] = targetParts;
            targetCharAdditionalData.SetField("rawAccessoriesInfos", rawAccessoriesInfos);

            List<ListInfoBase> infoAccessory = targetCharAdditionalData.GetField("infoAccessory").ToList<ListInfoBase>();
            List<GameObject> objAccessory= targetCharAdditionalData.GetField("objAccessory").ToList<GameObject>();
            List<GameObject[]> objAcsMove= targetCharAdditionalData.GetField("objAcsMove").ToList<GameObject[]>();
            List<ChaAccessoryComponent> cusAcsCmp= targetCharAdditionalData.GetField("cusAcsCmp").ToList<ChaAccessoryComponent>();
            List<bool> showAccessories= targetCharAdditionalData.GetField("showAccessories").ToList<bool>();
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
            targetCharAdditionalData.SetField("objAccessory",objAccessory );
            targetCharAdditionalData.SetField("objAcsMove",objAcsMove );
            targetCharAdditionalData.SetField("cusAcsCmp",cusAcsCmp );
            targetCharAdditionalData.SetField("showAccessories", showAccessories);
            targetCharAdditionalData.SetField("nowAccessories", targetParts);

            //_accessoriesByChar[targetChaFile] = targetCharAdditionalData;
            MoreAccObj.SetField("_accessoriesByChar", _accessoriesByChar);
            MoreAccessories.InvokeMember("Update", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, MoreAccObj, null);

            Logger.Log(LogLevel.Debug, "[KK_SCLO] Copy MoreAccessories Finish(3)");
        }

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

            ChaListControl chaListControl = Singleton<Manager.Character>.Instance.chaListCtrl;
            List<string> result = nowAccessories.Select(x => {
                //Logger.Log(LogLevel.Debug, "[KK_SCLO] Find id: " + x.id);
                //if (x.id == 0) { return "空"; }
                ////var l = Patches.accessoriesList.Find(info => info.Id == x.id);
                //var l = chaListControl.GetListInfo((ChaListDefine.CategoryNo)x.type, x.id);
                
                //return (null == l) ? "未識別" : l.Name;
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Find id: " + x.id);

                string name = "";
                if (x.id == 0) {
                    name = "空";
                }
                if (null == name || "" == name) {
                    name = chaListControl.GetListInfo((ChaListDefine.CategoryNo)x.type, x.id)?.Name;
                }
                if (null == name || "" == name) {
                    name = Patches.TryGetResolutionInfo(x.id, (ChaListDefine.CategoryNo)x.type);
                }
                if (null == name || "" == name) {
                    name = "未識別";
                }
                return name;

            }).ToList();

            for (int i = result.Count - 1; i >= 0; i--) {
                if (result[i] == "空") {
                    result.RemoveAt(i);
                } else {
                    break;
                }
            }

            return result.ToArray();
        }
    }
}
