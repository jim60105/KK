using System;
using System.Reflection;
using BepInEx.Logging;
using Harmony;
using Logger = BepInEx.Logger;

namespace KK_StudioCharaOnlyLoadBody
{
    class MoreAccessories_Support
    {
        internal static void InitPatch(HarmonyInstance harmony)
        {
            harmony.Patch(typeof(ChaFile).GetMethod("CopyAll", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(MoreAccessories_Support), nameof(CopyAllPrefix), null), null, null);
            harmony.Patch(typeof(ChaControl).GetMethod("Reload", AccessTools.all), new HarmonyMethod(typeof(MoreAccessories_Support), nameof(ReloadPrefix), null), null, null);
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
                    throw new Exception("[KK_SCOLB] Load assembly FAILED: MoreAccessories");
                }
                Logger.Log(LogLevel.Debug, "[KK_SCOLB] MoreAccessories found");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex.Message);
                return false;
            }
        }

        //Set this to true, then write back overlay data on the next reload trigger
        public static bool RollbackFlag = false;
        private static void ReloadPrefix(ChaControl __instance)
        {
            if (RollbackFlag)
            {
                //var moreAcc = MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null);
                //MoreAccessories.InvokeMember("OnActualCharaSave", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, moreAcc, new object[] { __instance.chaFile });
                RollbackMoreAccessoriesData(__instance.chaFile);
                Logger.Log(LogLevel.Debug, "[KK_SCOLB] MoreAccessories Rollback Finish");
                RollbackFlag = false;
            }
            return;
        }

        private static ChaFile chaFileTemp = null;
        private static bool fakeCopyCall = false;
        private static bool CopyAllPrefix()
        {
            Logger.Log(LogLevel.Debug, "[KK_SCOLB] Block Origin Copy?:" + fakeCopyCall);
            return !fakeCopyCall;
        }

        public static void CopyMoreAccessoriesData(ChaFile chaFile)
        {
            chaFileTemp = new ChaFile();
            fakeCopyCall = true;
            chaFileTemp.CopyAll(chaFile);
            fakeCopyCall = false;

            Logger.Log(LogLevel.Debug, "[KK_SCOLB] Copy MoreAccessories Finish");
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
                Logger.Log(LogLevel.Debug, "[KK_SCOLB] Rollback MoreAccessories Finish");
            }
            else
            {
                Logger.Log(LogLevel.Error, "[KK_SCOLB] chaFileTemp is Null");
            }
            Update();
            return;
        }

        public static void Update()
        {
            var moreAcc = MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null);
            MoreAccessories.InvokeMember("Update", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, moreAcc, null);
            //Logger.Log(LogLevel.Debug, "[KK_SCOLB] Update MoreAccessories Finish");
        }

        public static void CleanMoreAccBackup()
        {
            chaFileTemp = null;
            return;
        }
    }
}
