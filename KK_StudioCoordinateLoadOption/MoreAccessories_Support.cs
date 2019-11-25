using ExtensibleSaveFormat;
using Extension;
using HarmonyLib;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEngine;
using ResolveInfo = Sideloader.AutoResolver.ResolveInfo;

namespace KK_StudioCoordinateLoadOption {

    internal class MoreAccessories_Support {
        internal static Type _MoreAccessories = null;
        internal static object _MoreAccObj = null;
        internal static Dictionary<ChaFile, object> _accessoriesByChar;
        internal static Queue<int> _accQueue = new Queue<int>();
        internal static ChaControl _sourceChaCtrl = null;
        internal static List<ChaFileAccessory.PartsInfo> _sourceAccParts = null;
        internal static ChaControl _targetChaCtrl = null;
        internal static List<ChaFileAccessory.PartsInfo> _targetAccParts = null;

        public static bool LoadAssembly() {
            try {
                string path = KK_StudioCoordinateLoadOption.TryGetPluginInstance("com.joan6694.illusionplugins.moreaccessories")?.Info.Location;
                Assembly ass = Assembly.LoadFrom(path);
                _MoreAccessories = ass.GetType("MoreAccessoriesKOI.MoreAccessories");
                _MoreAccObj = _MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null);
                if (null == _MoreAccessories || null == _MoreAccObj) {
                    throw new Exception("Load assembly FAILED: MoreAccessories");
                }
                KK_StudioCoordinateLoadOption.Logger.LogDebug("MoreAccessories found");
                return true;
            } catch (Exception ex) {
                KK_StudioCoordinateLoadOption.Logger.LogDebug(ex.Message);
                return false;
            }
        }

        private static bool fakeCallFlag_CopyAll = false;

        [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), "CopyAll")]
        public static bool CopyAllPrefix() {
            //KK_StudioCoordinateLoadOption.Logger.LogDebug("Block Origin Copy?:"+fakeCopyCall);
            return !fakeCallFlag_CopyAll;
        }

        /// <summary>
        /// 將所有的MoreAccessories飾品由來源對象複製到目標對象
        /// </summary>
        /// <param name="sourceChaCtrl">來源對象</param>
        /// <param name="targetChaCtrl">目標對象</param>
        public static void CopyAllAccessories() {
            fakeCallFlag_CopyAll = true;
            _targetChaCtrl.chaFile.CopyAll(_sourceChaCtrl.chaFile);
            fakeCallFlag_CopyAll = false;

            _MoreAccessories.InvokeMember("Update",
                BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                _MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null),
                null);

            if (KK_StudioCoordinateLoadOption._isHairAccessoryCustomizerExist) {
                HairAccessoryCustomizer_Support.CopyAllHairAcc(_sourceChaCtrl, _targetChaCtrl);
            }
            KK_StudioCoordinateLoadOption.Logger.LogDebug("Copy all MoreAccessories Finish");
        }

        /// <summary>
        /// 將MoreAccessories飾品清空
        /// </summary>
        /// <param name="chaCtrl">清空對象</param>
        public static void ClearMoreAccessoriesData(ChaControl chaCtrl) {
            var parts = GetPartsInfoList(chaCtrl);
            for (int i = 0; i < parts.Count; i++) {
                if (!Patches.IsHairAccessory(chaCtrl, i)) {
                    parts[i] = new ChaFileAccessory.PartsInfo();
                }
            }
            WriteBackMoreAccData(chaCtrl, parts);
            _MoreAccessories.InvokeMember("UpdateStudioUI", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, _MoreAccObj, null);

            KK_StudioCoordinateLoadOption.Logger.LogDebug("Clear MoreAccessories Finish");
        }

        public static void GetMoreAccInfoLists(ChaControl sourceChaCtrl, ChaControl targetChaCtrl) {
            _sourceChaCtrl = sourceChaCtrl;
            _targetChaCtrl = targetChaCtrl;
            _sourceAccParts = GetPartsInfoList(_sourceChaCtrl);
            _targetAccParts = GetPartsInfoList(_targetChaCtrl);

            KK_StudioCoordinateLoadOption.Logger.LogDebug($"MoreAcc SourceAccParts Count : {_sourceAccParts.Count}");
            KK_StudioCoordinateLoadOption.Logger.LogDebug($"MoreAcc TargetAccParts Count : {_targetAccParts.Count}");
        }

        private static List<ChaFileAccessory.PartsInfo> GetPartsInfoList(ChaControl chaCtrl) {
            _accessoriesByChar = _MoreAccObj.GetField("_accessoriesByChar").ToDictionary<ChaFile, object>();
            _accessoriesByChar.TryGetValue(chaCtrl.chaFile, out var charAdditionalData);
            charAdditionalData.GetField("rawAccessoriesInfos")
                .ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>()
                .TryGetValue((ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType, out List<ChaFileAccessory.PartsInfo> parts);
            return parts;
        }

        public static void WriteBackMoreAccData(ChaControl chaCtrl, List<ChaFileAccessory.PartsInfo> parts) {
            _accessoriesByChar.TryGetValue(chaCtrl.chaFile, out var charAdditionalData);

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

            //因為是參考型別，不需要做這些
            //charAdditionalData.SetField("nowAccessories", parts);
            //charAdditionalData.SetField("infoAccessory", infoAccessory);
            //charAdditionalData.SetField("objAccessory", objAccessory);
            //charAdditionalData.SetField("objAcsMove", objAcsMove);
            //charAdditionalData.SetField("cusAcsCmp", cusAcsCmp);
            //charAdditionalData.SetField("showAccessories", showAccessories);

            //_accessoriesByChar[chaCtrl.chaFile] = charAdditionalData;
            //_MoreAccObj.SetField("_accessoriesByChar", _accessoriesByChar);
            _MoreAccessories.InvokeMember("Update", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, _MoreAccObj, null);
        }

        public static void RollbackMoreAccessories(bool[] toggleList) {
            bool isAllTrueFlag = true;
            bool isAllFalseFlag = true;
            foreach (var b in toggleList) {
                isAllTrueFlag &= b;
                isAllFalseFlag &= !b;
            }
            if (isAllTrueFlag && _accQueue.Count == 0 && !Patches.lockHairAcc) {
                CopyAllAccessories();
                KK_StudioCoordinateLoadOption.Logger.LogDebug("Load MoreAccessories All True");
                KK_StudioCoordinateLoadOption.Logger.LogDebug("Load MoreAccessories Finish (1)");
                return;
            }
            if (isAllFalseFlag && _accQueue.Count == 0 && !Patches.lockHairAcc) {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("Load MoreAccessories All False");
                KK_StudioCoordinateLoadOption.Logger.LogDebug("Load MoreAccessories Finish (2)");
                return;
            }

            toggleList = toggleList.Skip(20).ToArray();
            KK_StudioCoordinateLoadOption.Logger.LogDebug($"MoreAcc Toggles Count : {toggleList.Length}");

            for (int i = 0; i < _sourceAccParts.Count; i++) {
                //倒回時，如果該位置為髮飾品、增加模式，且在舊飾品數量內，則把佔位的Enqueue
                if (i < toggleList.Length && (Patches.IsHairAccessory(_sourceChaCtrl, i + 20) || toggleList[i] || Patches.addAccModeFlag)) {
                    //if (i < toggleList.Length && !toggleList[i] &&(( Patches.IsHairAccessory(_sourceChaCtrl, i + 20) && _targetAccParts[i].type != 120) || Patches.addAccModeFlag)) {
                    //if (_sourceAccParts[i].type != 120) {
                    _accQueue.Enqueue(i);
                    //KK_StudioCoordinateLoadOption.Logger.LogDebug($"->Lock: MoreAcc{i + 20} / ID: {_targetAccParts.ElementAtOrDefault(i)?.id ?? 0}");
                    KK_StudioCoordinateLoadOption.Logger.LogDebug($"->EnQueue: MoreAcc{i + 20} / ID: {_sourceAccParts.ElementAtOrDefault(i).id}");
                    //} //else continue;
                    //} else if (i >= targetAccParts.Count) {
                    //    //超過原本數量，就改用Add
                    //    targetAccParts.Add(MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tempSerlz));
                    //    KK_StudioCoordinateLoadOption.Logger.LogDebug($"->Added: MoreAcc{i} / ID: {targetAccParts.Last().id}");
                } else if (!toggleList[i]) {
                    CopyAccessory(_sourceChaCtrl, i, _targetChaCtrl, i);
                    KK_StudioCoordinateLoadOption.Logger.LogDebug($"->Rollback: MoreAcc{i + 20} / ID: {_sourceAccParts[i].id} -> {_targetAccParts[i].id}");
                }
            }

            //遍歷空欄，寫入暫存在accQueue的飾品
            for (int j = 0; _accQueue.Count > 0; j++) {
                if (j < _targetAccParts.Count && _targetAccParts[j].type != 120) {
                    continue;
                }
                var dequeueSlot = _accQueue.Dequeue();
                if (toggleList[dequeueSlot]) {
                    CopyAccessory(_targetChaCtrl, dequeueSlot, _targetChaCtrl, j);
                    KK_StudioCoordinateLoadOption.Logger.LogDebug($"->Dequeue: MoreAcc{j} / ID: {_targetAccParts[j].id}");
                }
                CopyAccessory(_sourceChaCtrl, dequeueSlot, _targetChaCtrl, dequeueSlot);
                KK_StudioCoordinateLoadOption.Logger.LogDebug($"->Rollback: MoreAcc{dequeueSlot} / ID: {_targetAccParts[dequeueSlot].id}");
            }

            ////由後往前刪除空欄
            //for (int i = _targetAccParts.Count - 1; i >= 0; i--) {
            //    if (_targetAccParts[i].type == 120) {
            //        _targetAccParts.RemoveAt(i);
            //    } else {
            //        break;
            //    }
            //}

            WriteBackMoreAccData(_targetChaCtrl, _targetAccParts);
            KK_StudioCoordinateLoadOption.Logger.LogDebug($"MoreAcc Parts Count : {_targetAccParts.Count}");
            KK_StudioCoordinateLoadOption.Logger.LogDebug("Load MoreAccessories Finish (3)");
        }

        public static void CopyAccessory(ChaControl sourceChaCtrl, int sourceSlot, ChaControl targetChaCtrl, int targetSlot) {
            List<ChaFileAccessory.PartsInfo> sourceAccParts = GetPartsInfoList(sourceChaCtrl);
            List<ChaFileAccessory.PartsInfo> targetAccParts = GetPartsInfoList(targetChaCtrl);

            byte[] tmp = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(sourceAccParts.ElementAtOrDefault(sourceSlot) ?? new ChaFileAccessory.PartsInfo());
            if (targetSlot < targetAccParts.Count) {
                targetAccParts[targetSlot] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tmp);
            } else {
                targetAccParts.Add(MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(tmp));
            }
            if (KK_StudioCoordinateLoadOption._isHairAccessoryCustomizerExist) {
                HairAccessoryCustomizer_Support.CopyHairAcc(sourceChaCtrl, sourceSlot + 20, targetChaCtrl, targetSlot + 20);
            }
            KK_StudioCoordinateLoadOption.Logger.LogDebug($">MoreAcc Copy: {sourceChaCtrl.fileParam.fullname} {sourceSlot + 20} -> {targetChaCtrl.fileParam.fullname} {targetSlot + 20}");
        }

        /// <summary>
        /// 讀取MoreAccessories
        /// </summary>
        /// <param name="chaFileCoordinate">讀取的衣裝對象</param>
        /// <returns></returns>
        public static string[] LoadMoreAccFromCoordinate(ChaFileCoordinate chaFileCoordinate) {
            List<ChaFileAccessory.PartsInfo> LoadedAccParts_FromSaveData = new List<ChaFileAccessory.PartsInfo>();
            PluginData sideLoaderExtData;
            //LoadedAccParts_FromSaveData.Clear();

            //typeof(Sideloader.AutoResolver.UniversalAutoResolver).GetNestedType("Hooks", BindingFlags.NonPublic).Invoke("ExtendedCoordinateLoad", new object[] { chaFileCoordinate });
            typeof(Sideloader.AutoResolver.UniversalAutoResolver).GetNestedType("Hooks", BindingFlags.NonPublic)
                .InvokeMember("ExtendedCoordinateLoad", BindingFlags.Default | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod, null, null,
                    new object[] { chaFileCoordinate });

            //本地Info
            List<ResolveInfo> LoadedResolutionInfoList = Sideloader.AutoResolver.UniversalAutoResolver.LoadedResolutionInfo?.ToList();
            //讀取Sideloader extData
            List<ResolveInfo> extInfoList;
            sideLoaderExtData = ExtendedSave.GetExtendedDataById(chaFileCoordinate, "com.bepis.sideloader.universalautoresolver");
            if (sideLoaderExtData == null || !sideLoaderExtData.data.ContainsKey("info")) {
                KK_StudioCoordinateLoadOption.Logger.LogDebug("No sideloader extInfo found");
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
                                    KK_StudioCoordinateLoadOption.Logger.LogDebug($"Resolve {localExtInfo.GUID}: {localExtInfo.Slot} -> {localExtInfo.LocalSlot}");
                                    part.id = localExtInfo.LocalSlot;
                                }
                            }
                        }
                    }
                    LoadedAccParts_FromSaveData.Add(part);
                }
            }

            ////由後往前刪除空欄
            //for (int i = LoadedAccParts_FromSaveData.Count - 1; i >= 0; i--) {
            //    if (LoadedAccParts_FromSaveData[i].type == 120) {
            //        LoadedAccParts_FromSaveData.RemoveAt(i);
            //    } else {
            //        break;
            //    }
            //}

            return LoadedAccParts_FromSaveData.Select(x => Patches.GetNameFromIDAndType(x.id, (ChaListDefine.CategoryNo)x.type)).ToArray();
        }

        /// <summary>
        /// 從MoreAccessories取得ChaAccessoryComponent
        /// </summary>
        /// <param name="chaCtrl">對象角色</param>
        /// <param name="index">飾品欄位index</param>
        /// <returns></returns>
        public static ChaAccessoryComponent GetChaAccessoryComponent(ChaControl chaCtrl, int index) {
            return (ChaAccessoryComponent)_MoreAccessories.InvokeMember("GetChaAccessoryComponent",
                BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
                null,
                _MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null),
                new object[] { chaCtrl, index });
        }

        public static int GetAccessoriesAmount(ChaControl chaCtrl) {
            return GetPartsInfoList(chaCtrl)?.Count + 20 ?? 20;
        }

        public static void CleanMoreAccBackup() {
            _accessoriesByChar = null;
            _sourceChaCtrl = null;
            _sourceAccParts = null;
            _targetChaCtrl = null;
            _targetAccParts = null;
            _accQueue.Clear();
        }
    }
}