using BepInEx.Logging;
using Extension;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_StudioCoordinateLoadOption {
    class KCOX_Support {
        private static object KCOXController;
        private static Dictionary<string, object> KCOXTexDataBackup = null;

        internal static string[] MaskKind = { "BodyMask","InnerMask","InnerMask" };
        public static bool LoadAssembly() {
            if (File.Exists("BepInEx/KoiClothesOverlay.dll") || File.Exists("BepInEx/KK.OverlayMods.dll")) {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] KCOX found");
            } else {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Load assembly FAILED: KCOX");
                return false;
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
                foreach(var maskKind in MaskKind) {
                    if (GetOverlay(maskKind)) {
                        cnt++;
                    }
                }
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Get Original Overlay/Mask Total: " + cnt);
            }
            return;
        }

        private static bool GetOverlay(string name) {
            KCOXTexDataBackup[name] = KCOXController.Invoke("GetOverlayTex", new object[] { name, false });

            if (null == KCOXTexDataBackup[name]) {
                //Logger.Log(LogLevel.Debug, "[KK_SCLO] " + name + " not found");
                return false;
            } else {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] ->Get Original Overlay/Mask: " + name);
                return true;
            }
        }

        public static void RollbackOverlay(bool main, int kind, ChaControl chaCtrl, string name = "texName") {
            if (string.Equals("texName", name)) {
                name = main ? Patches.MainClothesNames[kind] : Patches.SubClothesNames[kind];
            }

            if (null != KCOXController && null != KCOXTexDataBackup) {
                KCOXTexDataBackup.TryGetValue(name, out var tex);

                //KCOXController.Invoke("SetOverlayTex", new object[] { tex, name });
                Dictionary<string, object> CurrentOverlayTextures = KCOXController.GetProperty("CurrentOverlayTextures").ToDictionary<string, object>();
                var coordinateType = (ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType;
                Dictionary<ChaFileDefine.CoordinateType, object> _allOverlayTextures = KCOXController.GetField("_allOverlayTextures").ToDictionary<ChaFileDefine.CoordinateType, object>();
                if (CurrentOverlayTextures.TryGetValue(name, out var existing)) {
                    existing?.Invoke("Dispose");
                    if (tex == null || (bool)tex.Invoke("IsEmpty")) {
                        CurrentOverlayTextures.Remove(name);
                    } else {
                        CurrentOverlayTextures[name].SetProperty("Texture", tex.GetProperty("Texture"));
                        Logger.Log(LogLevel.Debug, $"[KK_SCLO] ->Overlay/Mask Rollback: {name} (Replace)");
                    }
                } else {
                    if (tex == null || (bool)tex.Invoke("IsEmpty")) {
                        CurrentOverlayTextures.Remove(name);
                    } else {
                        _allOverlayTextures[coordinateType].Invoke("Add", new object[] { name, tex });
                        Logger.Log(LogLevel.Debug, $"[KK_SCLO] ->Overlay/Mask Rollback: {name} (Add)");
                    }
                }
                KCOXController.Invoke("RefreshTexture", new object[] { name });
            } else {
                //Logger.Log(LogLevel.Debug, "[KK_SCLO] ->Overlay/Mask not found: " + name);
            }
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
