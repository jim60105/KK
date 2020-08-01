using Extension;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_StudioCoordinateLoadOption {
    class KCOX_Support {
        private static readonly BepInEx.Logging.ManualLogSource Logger = KK_StudioCoordinateLoadOption.Logger;

        internal static string[] MaskKind = { "BodyMask", "InnerMask", "BraMask" };
        internal static readonly string[] MainClothesNames = {
            "ct_clothesTop",
            "ct_clothesBot",
            "ct_bra",
            "ct_shorts",
            "ct_gloves",
            "ct_panst",
            "ct_socks",
            "ct_shoes_inner",
            "ct_shoes_outer"
        };
        internal static readonly string[] SubClothesNames = {
            "ct_top_parts_A",
            "ct_top_parts_B",
            "ct_top_parts_C"
        };

        private static ChaControl sourceChaCtrl;
        private static ChaControl targetChaCtrl;
        private static MonoBehaviour SourceKCOXController;
        private static MonoBehaviour TargetKCOXController;
        private static Dictionary<string, object> SourceKCOXBackup = null;

        public static bool LoadAssembly() {
            if (null != Extension.Extension.TryGetPluginInstance("KCOX", new System.Version(5, 2))) {
                Logger.LogDebug("KCOX found");
                return true;
            } else {
                Logger.LogDebug("Load assembly FAILED: KCOX");
                return false;
            }
        }

        /// <summary>
        /// Copy前準備Source和Target資料
        /// </summary>
        /// <param name="sourceChaCtrl">來源ChaControl</param>
        /// <param name="targetChaCtrl">目標ChaControl</param>
        public static bool GetControllerAndBackupData(ChaControl sourceChaCtrl = null, ChaControl targetChaCtrl = null) {
            if (null != sourceChaCtrl) {
                Logger.LogDebug("Source Overlay-----");
                KCOX_Support.sourceChaCtrl = sourceChaCtrl;
                SourceKCOXController = GetExtendedDataFromController(sourceChaCtrl, out SourceKCOXBackup);
                if (null == SourceKCOXController) {
                    Logger.LogDebug($"No Source KCOXController found on {sourceChaCtrl.fileParam.fullname}");
                    return false;
                }
            }

            if (null != targetChaCtrl) {
                Logger.LogDebug("Target Overlay-----");
                KCOX_Support.targetChaCtrl = targetChaCtrl;
                TargetKCOXController = GetExtendedDataFromController(targetChaCtrl, out _);
                if (null == TargetKCOXController) {
                    Logger.LogDebug($"No Target KCOXController found on {targetChaCtrl.fileParam.fullname}");
                    return false;
                }
            }
            return true;
        }

        public static MonoBehaviour GetExtendedDataFromController(ChaControl chaCtrl, out Dictionary<string, object> dict) {
            MonoBehaviour controller = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "KoiClothesOverlayController"));

            dict = new Dictionary<string, object>();
            if (null == controller) {
                Logger.LogDebug($"No KCOX Controller found on {chaCtrl.fileParam.fullname}");
            } else {
                int cnt = 0;
                foreach (string mainName in MainClothesNames) {
                    cnt += GetOverlay(controller, dict, mainName) ? 1 : 0;
                }
                foreach (string subName in SubClothesNames) {
                    cnt += GetOverlay(controller, dict, subName) ? 1 : 0;
                }
                foreach (string maskKind in MaskKind) {
                    cnt += GetOverlay(controller, dict, maskKind) ? 1 : 0;
                }
                Logger.LogDebug("Get Overlay/Mask Total: " + cnt);
            }
            return controller;
        }

        private static bool GetOverlay(object controller, Dictionary<string, object> dict, string name) {
            dict[name] = controller.Invoke("GetOverlayTex", new object[] { name, false });

            if (null == dict[name]) {
                //Logger.LogDebug(name + " not found");
                return false;
            } else {
                Logger.LogDebug("->Get Overlay/Mask: " + name);
                return true;
            }
        }

        public static void CopyKCOXData(ChaControl sourceChaCtrl, ChaControl targetChaCtrl, int kind, bool? main = true) {
            if (sourceChaCtrl != KCOX_Support.sourceChaCtrl || targetChaCtrl != KCOX_Support.targetChaCtrl) {
                if (!GetControllerAndBackupData(sourceChaCtrl, targetChaCtrl)) {
                    Logger.LogError("Skip on KCOX Controller not found.");
                    CleanKCOXBackup();
                    return;
                }
            }

            string name = "";
            switch (main) {
                case true:
                    name = MainClothesNames[kind];
                    switch (kind) {
                        case 0:
                            //換上衣時處理sub和BodyMask、InnerMask
                            for (int i = 0; i < 3; i++) {
                                CopyKCOXData(sourceChaCtrl, targetChaCtrl, i, false);
                            }
                            for (int i = 0; i < 2; i++) {
                                CopyKCOXData(sourceChaCtrl, targetChaCtrl, i, null);
                            }
                            break;
                        case 2:
                            //換胸罩時處理BraMask
                            CopyKCOXData(sourceChaCtrl, targetChaCtrl, 2, null);
                            break;
                    }
                    break;
                case false:
                    name = SubClothesNames[kind];
                    break;
                case null:
                    name = MaskKind[kind];
                    break;
            }

            bool exist = SourceKCOXBackup.TryGetValue(name, out object tex);

            //KCOXController.Invoke("SetOverlayTex", new object[] { tex, name });
            ChaFileDefine.CoordinateType coordinateType = (ChaFileDefine.CoordinateType)targetChaCtrl.fileStatus.coordinateType;
            Dictionary<ChaFileDefine.CoordinateType, object> _allOverlayTextures = TargetKCOXController.GetField("_allOverlayTextures").ToDictionary<ChaFileDefine.CoordinateType, object>();
            if (TargetKCOXController.GetProperty("CurrentOverlayTextures").ToDictionary<string, object>().TryGetValue(name, out object clothesTexData)) {
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
                    Logger.LogDebug($"->Replace Overlay/Mask: {name}");
                }
            } else {
                if (exist && tex != null && !(bool)tex.Invoke("IsEmpty")) {
                    _allOverlayTextures[coordinateType].Invoke("Add", new object[] { name, tex });
                    Logger.LogDebug($"->Add Overlay/Mask: {name}");
                }
            }
        }

        /// <summary>
        /// 重新整理Overlay，Mask需要這個
        /// </summary>
        public static IEnumerator Update(ChaControl chaCtrl) {
            yield return null;
            MonoBehaviour KCOXController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "KoiClothesOverlayController"));
            KCOXController.Invoke("RefreshAllTextures", new object[] { false });
        }

        public static void SetExtDataFromController(ChaControl chaCtrl) {
            MonoBehaviour KCOXController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "KoiClothesOverlayController"));
            KCOXController.Invoke("OnCardBeingSaved", new object[] { 1 });
        }

        public static bool CheckControllerPrepared(ChaControl chaCtrl) {
            if (!KK_StudioCoordinateLoadOption._isKCOXExist) return true;

            MonoBehaviour controller = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "KoiClothesOverlayController"));
            return null != controller && (bool)controller?.GetProperty("Started") && null != controller.GetProperty("CurrentOverlayTextures");
        }

        public static void CleanKCOXBackup() {
            sourceChaCtrl = null;
            targetChaCtrl = null;
            SourceKCOXBackup = null;
            SourceKCOXController = null;
            TargetKCOXController = null;
            return;
        }
    }
}
