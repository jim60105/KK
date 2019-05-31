using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_StudioCoordinateLoadOption
{
    class KCOX_Support
    {
        private static readonly BindingFlags publicFlag = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance;
        private static readonly BindingFlags privateFlag = BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance;
        private static object KCOXController;
        private static Dictionary<string, object> KCOXTexDataBackup = null;

        public static bool LoadAssembly()
        {
            try
            {
                var ass = Assembly.LoadFrom("BepInEx/KoiClothesOverlay.dll");
                if (null == ass)
                {
                    throw new Exception("Load assembly FAILED: KCOX");
                }
                Logger.Log(LogLevel.Debug, "[KK_SCLO] KCOX found");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] " + ex.Message);
                return false;
            }
        }

        public static void BackupKCOXData(ChaControl chaCtrl, ChaFileClothes clothes)
        {
            KCOXController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KoiClothesOverlayX"));
            if (null == KCOXController)
            {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] No KCOX Controller found");
            }
            else
            {
                KCOXTexDataBackup = new Dictionary<string, object>();
                int cnt = 0;
                for (int i = 0; i < clothes.parts.Length; i++)
                {
                    if (GetOverlay(Patches.MainClothesNames[i]))
                    {
                        cnt++;
                    }
                }
                for (int j = 0; j < clothes.subPartsId.Length; j++)
                {
                    if (GetOverlay(Patches.SubClothesNames[j]))
                    {
                        cnt++;
                    }
                }
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Get original Overlay: " + cnt);
            }
            return;
        }

        private static bool GetOverlay(string name)
        {
            KCOXTexDataBackup[name] = KCOXController.GetType().InvokeMember("GetOverlayTex", publicFlag, null, KCOXController, new object[] { name });
            if (null == KCOXTexDataBackup[name])
            {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] " + name + " not found");
                return false;
            }
            else
            {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Get original overlay: " + name);
                return true;
            }
        }

        public static void RollbackOverlay(bool main, int kind)
        {
            string name = main ? Patches.MainClothesNames[kind] : Patches.SubClothesNames[kind];

            if (null != KCOXController && null != KCOXTexDataBackup)
            {
                KCOXTexDataBackup.TryGetValue(name, out var tex);
                KCOXController.GetType().InvokeMember("SetOverlayTex", publicFlag, null, KCOXController, new object[] { tex, name });

                if (null != tex)
                {
                    Logger.Log(LogLevel.Debug, "[KK_SCLO] ->Overlay Rollback: " + name);
                    return;
                }
            }
            Logger.Log(LogLevel.Debug, "[KK_SCLO] ->Overlay not found: " + name);
            return;
        }

        public static void CleanKCOXBackup()
        {
            if (null != KCOXController)
            {
                KCOXController.GetType().InvokeMember("OnCardBeingSaved", privateFlag, null, KCOXController, new object[] { 1 });
            }
            KCOXTexDataBackup = null;
            return;
        }
    }
}
