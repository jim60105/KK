using System;
using System.Linq;
using System.Reflection;

using BepInEx.Logging;

using Harmony;

using UnityEngine;

using Logger = BepInEx.Logger;

namespace KK_StudioCharaOnlyLoadBody
{
    class KCOX_Support
    {
        internal static void InitPatch(HarmonyInstance harmony)
        {
            harmony.Patch(typeof(ChaControl).GetMethod("Reload", AccessTools.all), new HarmonyMethod(typeof(KCOX_Support), nameof(ReloadPrefix), null), null, null);
        }

        public static bool LoadAssembly()
        {
            try
            {
                var ass = Assembly.LoadFrom("BepInEx/KoiClothesOverlay.dll");
                if (null == ass)
                {
                    throw new Exception("[KK_SCOLB] Load assembly FAILED: KCOX");
                }
                Logger.Log(LogLevel.Debug, "[KK_SCOLB] KCOX found");
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
                object KCOXController = __instance.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KoiClothesOverlayX"));
                KCOXController.GetType().InvokeMember("SetExtendedData", BindingFlags.Public | BindingFlags.InvokeMethod, null, KCOXController, new object[] { null });
                KCOXController.GetType().InvokeMember("OnCardBeingSaved", BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, KCOXController, new object[] { 1 });
                Logger.Log(LogLevel.Debug, "[KK_SCOLB] Overlay Rollback Finish");
                RollbackFlag = false;
            }
            return;
        }
    }
}
