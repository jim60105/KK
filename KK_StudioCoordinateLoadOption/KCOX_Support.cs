using Extension;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_StudioCoordinateLoadOption {
    class KCOX_Support {
        private static readonly BepInEx.Logging.ManualLogSource Logger = KK_StudioCoordinateLoadOption.Logger;
        private static object KCOXController;
        private static Dictionary<string, object> KCOXTexDataBackup = null;

        internal static string[] MaskKind = { "BodyMask", "InnerMask", "BraMask" };
        public static bool LoadAssembly() {
            if (null != Extension.Extension.TryGetPluginInstance("KCOX", new System.Version(5, 0))) {
                Logger.LogDebug("KCOX found");
                return true;
            } else {
                Logger.LogDebug("Load assembly FAILED: KCOX");
                return false;
            }
        }

        public static void BackupKCOXData(ChaControl chaCtrl, ChaFileClothes clothes) {
            KCOXController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KoiClothesOverlayX"));
            if (null == KCOXController) {
                Logger.LogDebug("No KCOX Controller found");
            } else {
                KCOXTexDataBackup = new Dictionary<string, object>();
                int cnt = 0;
                for (int i = 0; i < clothes.parts.Length; i++) {
                    cnt += GetOverlay(Patches.MainClothesNames[i]) ? 1 : 0;
                }
                for (int j = 0; j < clothes.subPartsId.Length; j++) {
                    cnt += GetOverlay(Patches.SubClothesNames[j]) ? 1 : 0;
                }
                foreach (string maskKind in MaskKind) {
                    cnt += GetOverlay(maskKind) ? 1 : 0;
                }
                Logger.LogDebug("Get Original Overlay/Mask Total: " + cnt);
            }
            return;
        }

        private static bool GetOverlay(string name) {
            KCOXTexDataBackup[name] = KCOXController.Invoke("GetOverlayTex", new object[] { name, false });

            if (null == KCOXTexDataBackup[name]) {
                //Logger.LogDebug(name + " not found");
                return false;
            } else {
                Logger.LogDebug("->Get Original Overlay/Mask: " + name);
                return true;
            }
        }

        public static void RollbackOverlay(bool main, int kind, ChaControl chaCtrl, string name = "texName") {
            if (string.Equals("texName", name)) {
                name = main ? Patches.MainClothesNames[kind] : Patches.SubClothesNames[kind];
            }

            if (null != KCOXController && null != KCOXTexDataBackup) {
                bool exist = KCOXTexDataBackup.TryGetValue(name, out object tex);

                //KCOXController.Invoke("SetOverlayTex", new object[] { tex, name });
                ChaFileDefine.CoordinateType coordinateType = (ChaFileDefine.CoordinateType)chaCtrl.fileStatus.coordinateType;
                Dictionary<ChaFileDefine.CoordinateType, object> _allOverlayTextures = KCOXController.GetField("_allOverlayTextures").ToDictionary<ChaFileDefine.CoordinateType, object>();
                if (KCOXController.GetProperty("CurrentOverlayTextures").ToDictionary<string, object>().TryGetValue(name, out object clothesTexData)) {
                    if (!exist || tex == null || (bool)tex.Invoke("IsEmpty")) {
                    clothesTexData.Invoke("Clear");
                    clothesTexData.SetField("Override", false);
                        _allOverlayTextures[coordinateType].Invoke("Remove", new object[] { name });
                        Logger.LogDebug($"->Clear Overlay/Mask: {name}");
                    } else {
                        clothesTexData.SetProperty("Texture", tex.GetProperty("Texture"));
                        if (null != tex.GetField("Override")) {
                            clothesTexData.SetField("Override", tex.GetField("Override"));
                        }
                        Logger.LogDebug($"->Overlay/Mask Rollback: {name} (Replace)");
                    }
                } else {
                    if (exist && tex != null && !(bool)tex.Invoke("IsEmpty")) {
                        _allOverlayTextures[coordinateType].Invoke("Add", new object[] { name, tex });
                        Logger.LogDebug($"->Overlay/Mask Rollback: {name} (Add)");
                    }
                }
            }
        }

        public static void SetExtDataFromController(ChaControl chaCtrl) {
            MonoBehaviour KCOXController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Namespace, "KoiClothesOverlayX"));
            KCOXController.Invoke("OnCardBeingSaved", new object[] { 1 });
        }

        public static void CleanKCOXBackup() {
            KCOXTexDataBackup = null;
            return;
        }
    }
}
