using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Extension;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_StudioCoordinateLoadOption {
    class KCOX_Support {
        private static object KCOXController;
        private static Dictionary<string, object> KCOXTexDataBackup = null;

        public static bool LoadAssembly() {
            if (!File.Exists("BepInEx/KoiClothesOverlay.dll")) {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Load assembly FAILED: KCOX");
                return false;
            } else {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] KCOX found");
            }
            return true;
        }

        public static void BackupKCOXData(ChaControl chaCtrl, ChaFileClothes clothes) {
            KCOXController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KoiClothesOverlayX"));
            if (null == KCOXController) {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] No KCOX Controller found");
            } else {
                KCOXTexDataBackup = new Dictionary<string, object>();
                int cnt = 0;
                for (int i = 0; i < clothes.parts.Length; i++) {
                    if (GetOverlay(Patches.MainClothesNames[i])) {
                        cnt++;
                    }
                }
                for (int j = 0; j < clothes.subPartsId.Length; j++) {
                    if (GetOverlay(Patches.SubClothesNames[j])) {
                        cnt++;
                    }
                }
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Get Original Overlay Total: " + cnt);
            }
            return;
        }

        private static bool GetOverlay(string name) {
            KCOXTexDataBackup[name] = KCOXController.Invoke("GetOverlayTex", new object[] { name });
            if (null == KCOXTexDataBackup[name]) {
                //Logger.Log(LogLevel.Debug, "[KK_SCLO] " + name + " not found");
                return false;
            } else {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] ->Get Original Overlay: " + name);
                return true;
            }
        }

        public static void RollbackOverlay(bool main, int kind) {
            string name = main ? Patches.MainClothesNames[kind] : Patches.SubClothesNames[kind];

            if (null != KCOXController && null != KCOXTexDataBackup) {
                KCOXTexDataBackup.TryGetValue(name, out var tex);
                KCOXController.Invoke("SetOverlayTex", new object[] { tex, name });

                if (null != tex) {
                    Logger.Log(LogLevel.Debug, "[KK_SCLO] ->Overlay Rollback: " + name);
                    return;
                }
            }
            //Logger.Log(LogLevel.Debug, "[KK_SCLO] ->Overlay not found: " + name);
            return;
        }

        public static void CleanKCOXBackup() {
            if (null != KCOXController) {
                KCOXController.Invoke("OnCardBeingSaved", new object[] { 1 });
            }
            KCOXTexDataBackup = null;
            return;
        }
    }
}
