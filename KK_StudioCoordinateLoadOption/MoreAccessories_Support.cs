using BepInEx.Logging;
using Harmony;
using System;
using System.Reflection;
using Logger = BepInEx.Logger;

namespace KK_StudioCoordinateLoadOption
{
    class MoreAccessories_Support
    {
        public static Type CharAdditionalData = null;
        private static Type MoreAccessories = null;
        private static Type ChaFile_CopyAll_Patches = null;

        internal static void InitPatch(HarmonyInstance harmony)
        {
            harmony.Patch(typeof(ChaFile).GetMethod("CopyAll", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(MoreAccessories_Support), nameof(CopyAllPrefix), null),null, null);
        }

        public static bool LoadAssembly()
        {
            try
            {
                Assembly ass = Assembly.LoadFrom("BepInEx/MoreAccessories.dll");
                MoreAccessories = ass.GetType("MoreAccessoriesKOI.MoreAccessories");
                ChaFile_CopyAll_Patches = ass.GetType("MoreAccessoriesKOI.ChaFile_CopyAll_Patches");
                CharAdditionalData = MoreAccessories.GetNestedType("CharAdditionalData");
                if (null == CharAdditionalData || null == MoreAccessories || null == ChaFile_CopyAll_Patches)
                {
                    throw new Exception("[KK_SCLO] Load assembly FAILED: MoreAccessories");
                }
                Logger.Log(LogLevel.Debug, "[KK_SCLO] MoreAccessories found");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex.Message);
                return false;
            }
        }

        private static ChaFile chaFileTemp = null;
        private static bool fakeCopyCall = false;
        private static bool CopyAllPrefix()
        {
            //Logger.Log(LogLevel.Debug, "[KK_SCLO] Block Origin Copy?:"+fakeCopyCall);
            return !fakeCopyCall;
        }

        public static void CopyMoreAccessoriesData(ChaFile chaFile)
        {
            chaFileTemp = new ChaFile();
            fakeCopyCall = true;
            chaFileTemp.CopyAll(chaFile);
            fakeCopyCall = false;

            Logger.Log(LogLevel.Debug, "[KK_SCLO] Copy MoreAccessories Finish");
            return;
        }

        public static void RollbackMoreAccessoriesData(ChaFile chaFile)
        {
            if (null != chaFileTemp)
            {
                fakeCopyCall = true;
                chaFile.CopyAll(chaFileTemp);
                fakeCopyCall = false;
                chaFileTemp = null;
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Rollback MoreAccessories Finish");
            }
            else
            {
                Logger.Log(LogLevel.Error, "[KK_SCLO] chaFileTemp is Null");
            }
            Update();
            return;
        }

        public static void Update()
        {
            var moreAcc = MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null);
            MoreAccessories.InvokeMember("Update", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, moreAcc,null);
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Update MoreAccessories Finish");
        }

        public static void CleanMoreAccBackup()
        {
            chaFileTemp = null;
            return;
        }

        //public static ChaFileAccessory.PartsInfo[] CopyMoreAccessoriesData(ChaFile chaFile)
        //{
        //    object accessoriesByChar = null;
        //    backupData = null;
        //    try
        //    {
        //        FieldInfo[] fieldInfo = MoreAccessories.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        //        foreach (var fi in fieldInfo)
        //        {
        //            //Logger.Log(LogLevel.Debug, "[KK_SCLO] Name: " + fi.Name);
        //            //Logger.Log(LogLevel.Debug, "[KK_SCLO] FieldType: " + fi.FieldType);
        //            if (fi.Name == "_accessoriesByChar")
        //            {
        //                var moreAcc = MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null);
        //                if (null == moreAcc)
        //                {
        //                    Logger.Log(LogLevel.Error, "[KK_SCLO] Get MoreAccessories._self FAIELD");
        //                    return null;
        //                }
        //                accessoriesByChar = fi.GetValue(moreAcc);
        //                if (null == accessoriesByChar)
        //                {
        //                    Logger.Log(LogLevel.Error, "[KK_SCLO] AccessoriesByChar not get");
        //                    return null;
        //                }
        //                Type t = accessoriesByChar.GetType();
        //                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        //                {
        //                    IDictionary newDict = (IDictionary)Convert.ChangeType(accessoriesByChar, t);
        //                    foreach (DictionaryEntry entry in newDict)
        //                    {
        //                        if (entry.Key == chaFile)
        //                        {
        //                            if (null == entry.Value)
        //                            {
        //                                Logger.Log(LogLevel.Error, "[KK_SCLO] Get CharAdditionalData FAILD");
        //                            }
        //                            var nowAcc = entry.Value.GetPrivate("nowAccessories") as List<ChaFileAccessory.PartsInfo>;
        //                            //            backupData = Extensions.CloneObject(entry.Value);
        //                            Stack<ChaFileAccessory.PartsInfo> result = new Stack<ChaFileAccessory.PartsInfo>();
        //                            foreach(var n in nowAcc)
        //                            //for (int i = 0; i < nowAcc.Count; i++)
        //                            {
        //                                result.Push(n);

        //                                Logger.Log(LogLevel.Debug, "[KK_SCLO] Get Accessory: " + n.id);
        //                            }
        //                            while(result.Count>0)
        //                            {
        //                                if (result.Peek().id == 0)
        //                                {
        //                                    result.Pop();
        //                                }
        //                                else{
        //                                    break;
        //                                }
        //                            }

        //                            //if (null == backupData)
        //                            //{
        //                            //    Logger.Log(LogLevel.Error, "[KK_SCLO] Get MoreAccessories FAIELD");
        //                            //    return false;
        //                            //}
        //                            Logger.Log(LogLevel.Debug, "[KK_SCLO] Get MoreAccessories Success");
        //                            return result.ToArray();
        //                        }
        //                    }
        //                    Logger.Log(LogLevel.Error, "[KK_SCLO] Char not found in AccessoriesByChar");
        //                }
        //                break;
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Logger.Log(LogLevel.Error, "[KK_SCLO] Exception: " + e);
        //        Logger.Log(LogLevel.Error, "[KK_SCLO] Exception: " + e.Message);
        //        return null;
        //    }
        //    return null;
        //}

        //public static bool RollbackMoreAccessoriesData(ChaFile chaFile)
        //{
        //    if (null == backupData)
        //    {
        //        Logger.Log(LogLevel.Error, "[KK_SCLO] MoreAccessories backupData is Null");
        //        return false;
        //    }
        //    object accessoriesByChar = null;
        //    try
        //    {
        //        FieldInfo[] fieldInfo = MoreAccessories.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        //        foreach (var fi in fieldInfo)
        //        {
        //            //Logger.Log(LogLevel.Debug, "[KK_SCLO] Name: " + fi.Name);
        //            //Logger.Log(LogLevel.Debug, "[KK_SCLO] FieldType: " + fi.FieldType);
        //            if (fi.Name == "_accessoriesByChar")
        //            {
        //                var moreAcc = MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null);
        //                if (null == moreAcc)
        //                {
        //                    Logger.Log(LogLevel.Error, "[KK_SCLO] Get MoreAccessories._self FAIELD");
        //                    return false;
        //                }
        //                //accessoriesByChar = fi.GetValue(moreAcc);
        //                //if (null == accessoriesByChar)
        //                //{
        //                //    Logger.Log(LogLevel.Error, "[KK_SCLO] AccessoriesByChar not get");
        //                //    return false;
        //                //}
        //                //Type t = accessoriesByChar.GetType();
        //                //if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        //                //{
        //                //IDictionary newDict = (IDictionary)Convert.ChangeType(accessoriesByChar, t);
        //                //    foreach (DictionaryEntry entry in newDict)
        //                //    {
        //                //        if (entry.Key == chaFile)
        //                //        {
        //                //            if (null == entry.Value)
        //                //            {
        //                //                Logger.Log(LogLevel.Error, "[KK_SCLO] Get CharAdditionalData FAILD");
        //                //            }
        //                //            //newDict[entry.Key].SetPrivate("nowAccessories", backupData);
        //                //            newDict[entry.Key] = Extensions.CloneObject(backupData);
        //                //            break;
        //                //        }
        //                //    }
        //                //    accessoriesByChar = newDict;
        //                //}
        //                //if (null == accessoriesByChar)
        //                //{
        //                //    Logger.Log(LogLevel.Error, "[KK_SCLO] Char not found in AccessoriesByChar");
        //                //    return false;
        //                //}
        //                //fi.SetValue(moreAcc, accessoriesByChar);
        //                Update();
        //                Logger.Log(LogLevel.Debug, "[KK_SCLO] Set MoreAccessories Success");
        //                return true;
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Logger.Log(LogLevel.Error, "[KK_SCLO] Exception: " + e);
        //        Logger.Log(LogLevel.Error, "[KK_SCLO] Exception: " + e.Message);
        //        return false;
        //    }
        //    return false;
        //}
    }

}
