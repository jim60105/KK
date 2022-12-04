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

namespace CoordinateLoadOption.OtherPlugin
{
    class MoreAccessories
    {
        public const string GUID = "com.joan6694.illusionplugins.moreaccessories";
        private static object MoreAccObj;
        private static Type MoreAcc;

        public static bool LoadAssembly()
        {
            try
            {
                string path = KoikatuHelper.TryGetPluginInstance(GUID, new Version(2, 0, 10))?.Info.Location;
                Assembly ass = Assembly.LoadFrom(path);
                MoreAcc = ass.GetType("MoreAccessoriesKOI.MoreAccessories");
                MoreAccObj = MoreAcc?.GetFieldStatic("_self");
                if (null == MoreAccObj)
                {
                    throw new Exception("Load assembly FAILED: MoreAccessories");
                }
                Extension.Logger.LogDebug("MoreAccessories found");
                return true;
            }
            catch (Exception ex)
            {
                Extension.Logger.LogDebug(ex.Message);
                return false;
            }
        }

        internal static void PatchMoreAcc(Harmony harmony)
        {
            // MoreAccessoriesKOI.MoreAccessories.OnActualCoordSave(ChaFileCoordinate file)
            // is not working outside maker
            harmony.Patch(MoreAcc.GetMethod("OnActualCoordSave", AccessTools.all),
                prefix: new HarmonyMethod(typeof(MoreAccessories), nameof(NotInsideStudio)));
        }

        private static bool NotInsideStudio() => !CoordinateLoadOption.insideStudio;

        /// <summary>
        /// 讀取MoreAccessories
        /// </summary>
        /// <param name="chaFileCoordinate">讀取的衣裝對象</param>
        /// <returns></returns>
        public static string[] LoadOldMoreAccData(ChaFileCoordinate chaFileCoordinate)
        {
            List<ChaFileAccessory.PartsInfo> tempLoadedAccessories = new List<ChaFileAccessory.PartsInfo>();

            typeof(Sideloader.AutoResolver.UniversalAutoResolver).GetNestedType("Hooks", BindingFlags.NonPublic).InvokeStatic("ExtendedCoordinateLoad", new object[] { chaFileCoordinate });

            //本地Info
            List<ResolveInfo> LoadedResolutionInfoList = Sideloader.AutoResolver.UniversalAutoResolver.LoadedResolutionInfo?.ToList();
            //讀取Sideloader extData
            List<ResolveInfo> extInfoList;
            PluginData sideLoaderExtData = ExtendedSave.GetExtendedDataById(chaFileCoordinate, "com.bepis.sideloader.universalautoresolver");
            if (sideLoaderExtData == null || !sideLoaderExtData.data.ContainsKey("info"))
            {
                Extension.Logger.LogDebug("No sideloader extInfo found");
                extInfoList = null;
            }
            else
            {
                object[] tmpExtInfo = (object[])sideLoaderExtData.data["info"];
                extInfoList = tmpExtInfo.Select(x => MessagePackSerializer.Deserialize<ResolveInfo>((byte[])x)).ToList();
            }

            XmlNode node = null;
            PluginData pluginData = ExtendedSave.GetExtendedDataById(chaFileCoordinate, "moreAccessories");
            if (pluginData != null && pluginData.data.TryGetValue("additionalAccessories", out object xmlData))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml((string)xmlData);
                node = doc.FirstChild;
            }

            if (node != null)
            {
                foreach (XmlNode accessoryNode in node.ChildNodes)
                {
                    ChaFileAccessory.PartsInfo part = new ChaFileAccessory.PartsInfo
                    {
                        type = XmlConvert.ToInt32(accessoryNode.Attributes["type"].Value)
                    };
                    if (part.type != 120)
                    {
                        part.id = XmlConvert.ToInt32(accessoryNode.Attributes["id"].Value);
                        part.parentKey = accessoryNode.Attributes["parentKey"].Value;

                        for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                part.addMove[i, j] = new Vector3
                                {
                                    x = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}x"].Value),
                                    y = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}y"].Value),
                                    z = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}z"].Value)
                                };
                            }
                        }
                        for (int i = 0; i < 4; i++)
                        {
                            part.color[i] = new Color
                            {
                                r = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}r"].Value),
                                g = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}g"].Value),
                                b = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}b"].Value),
                                a = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}a"].Value)
                            };
                        }
                        part.hideCategory = XmlConvert.ToInt32(accessoryNode.Attributes["hideCategory"].Value);

                        //Only Darkness and KKS has this
                        if (null != part.GetType().GetProperty("noShake"))
                        {
                            part.SetProperty("noShake", accessoryNode.Attributes["noShake"] != null && XmlConvert.ToBoolean(accessoryNode.Attributes["noShake"].Value));
                        }

                        //處理Sideloader mod
                        if (null != extInfoList && null != LoadedResolutionInfoList)
                        {
                            ResolveInfo tmpExtInfo = extInfoList.FirstOrDefault(x => x.CategoryNo == (ChaListDefine.CategoryNo)part.type && x.Slot == part.id);
                            if (default(ResolveInfo) != tmpExtInfo)
                            {
                                ResolveInfo localExtInfo = LoadedResolutionInfoList.FirstOrDefault(x => x.GUID == tmpExtInfo.GUID && x.CategoryNo == tmpExtInfo.CategoryNo && x.Slot == tmpExtInfo.Slot);
                                if (default(ResolveInfo) != localExtInfo)
                                {
                                    Extension.Logger.LogDebug($"Resolve {localExtInfo.GUID}: {localExtInfo.Slot} -> {localExtInfo.LocalSlot}");
                                    part.id = localExtInfo.LocalSlot;
                                }
                            }
                        }
                    }
                    tempLoadedAccessories.Add(part);
                }
            }

            return tempLoadedAccessories.Select(x => Helper.GetNameFromIDAndType(x.id, (ChaListDefine.CategoryNo)x.type)).ToArray();
        }

        public static void ArraySync(ChaControl chaCtrl) => MoreAccObj.Invoke("ArraySync", new object[] { chaCtrl });
        public static void Update() => MoreAccObj.Invoke("UpdateUI");
    }
}
