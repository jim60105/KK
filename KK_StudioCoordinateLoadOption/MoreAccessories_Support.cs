using System;
using System.Reflection;
using BepInEx.Logging;
using Harmony;
using Logger = BepInEx.Logger;

namespace KK_StudioCoordinateLoadOption
{
    class MoreAccessories_Support
    {
        internal static void InitPatch(HarmonyInstance harmony)
        {
            harmony.Patch(typeof(ChaFile).GetMethod("CopyAll", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(MoreAccessories_Support), nameof(CopyAllPrefix), null), null, null);
        }

        private static Type MoreAccessories = null;

        public static bool LoadAssembly()
        {
            try
            {
                Assembly ass = Assembly.LoadFrom("BepInEx/MoreAccessories.dll");
                MoreAccessories = ass.GetType("MoreAccessoriesKOI.MoreAccessories");
                if (null == MoreAccessories)
                {
                    throw new Exception("Load assembly FAILED: MoreAccessories");
                }
                Logger.Log(LogLevel.Debug, "[KK_SCLO] MoreAccessories found");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "[KK_SCLO] "+ex.Message);
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
                CleanMoreAccBackup();
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
            MoreAccessories.InvokeMember("Update", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, moreAcc, null);
            Logger.Log(LogLevel.Debug, "[KK_SCLO] Update MoreAccessories Finish");
        }

        public static void CleanMoreAccBackup()
        {
            chaFileTemp = null;
            return;
        }
    }
}
