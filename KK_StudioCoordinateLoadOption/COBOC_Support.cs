using Extension;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace KK_StudioCoordinateLoadOption {
    class COBOC_Support {
        private static readonly BepInEx.Logging.ManualLogSource Logger = KK_StudioCoordinateLoadOption.Logger;

        public static bool LoadAssembly() {
            if (null != Extension.Extension.TryGetPluginInstance("com.jim60105.kk.charaoverlaysbasedoncoordinate", new System.Version(20, 3, 21, 0))) {
                Logger.LogDebug("KK_CharaOverlayBasedOnCoordinate found");
                return true;
            } else {
                Logger.LogDebug("Load assembly FAILED: KK_CharaOverlayBasedOnCoordinate");
                return false;
            }
        }

        public static void CopyCurrentCharaOverlayByController(ChaControl sourceChaCtrl, ChaControl targetChaCtrl, bool[] isChecked) {
            MonoBehaviour sourceController = sourceChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "CharaOverlaysBasedOnCoordinateController"));
            MonoBehaviour targetController = targetChaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "CharaOverlaysBasedOnCoordinateController"));

            object sourceOverlays = sourceController.GetProperty("CurrentOverlay").ToDictionaryWithoutType();
            object targetOverlays = targetController.GetProperty("CurrentOverlay").ToDictionaryWithoutType();

            object result = sourceOverlays.ToDictionaryWithoutType();   //Clone it to use the structure
            result.RemoveAll(x => true);

            _ = sourceOverlays.ForEach((x) => {
                DictionaryEntry d = (DictionaryEntry)x;
                int checkCase = -1;
                switch (Convert.ToInt32(d.Key)) {
                    case 1:
                    case 3:
                        checkCase = 2;
                        break;
                    case 2:
                    case 4:
                        checkCase = 1;
                        break;
                    case 5:
                    case 6:
                        checkCase = 0;
                        break;
                    case 0:
                        break;
                    default:
                        Logger.LogWarning("Cast failed while reading overlays from KK_COBOC!");
                        break;
                }
                if (checkCase >= 0 && checkCase < isChecked.Length) {
                    if (isChecked[checkCase]) {
                        result.Add(d.Key, ((byte[])d.Value).Clone());
                    } else if (targetOverlays.TryGetValue(d.Key, out object val)) {
                        result.Add(d.Key, ((byte[])val).Clone());
                    } else {
                        result.Add(d.Key, new byte[] { });
                    }
                }
            });

            if (targetController.SetProperty("CurrentOverlay", result)) {
                //單眼
                if (isChecked[0]) {
                    int[] sourceIris = (int[])sourceController.GetField("IrisDisplaySide");
                    int[] targetIris = (int[])targetController.GetField("IrisDisplaySide");
                    int coor = targetChaCtrl.fileStatus.coordinateType;
                    if (sourceIris.Length > coor && targetIris.Length > coor) {
                        targetIris[coor] = sourceIris[coor];
                    }
                    targetController.SetField("IrisDisplaySide", targetIris);
                }
                targetController.Invoke("OverwriteOverlay");
                Logger.LogDebug($"Copy Current CharaOverlay {sourceChaCtrl.fileParam.fullname} -> {targetChaCtrl.fileParam.fullname}: {isChecked[0]} {isChecked[1]} {isChecked[2]}");
            }
        }

        public static void SetExtDataFromController(ChaControl chaCtrl) {
            MonoBehaviour COBOCController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "CharaOverlaysBasedOnCoordinateController"));

            COBOCController.Invoke("SavePluginData", new object[] { false });
        }

        public static void SetCoordinateExtDataFromController(ChaControl chaCtrl, ChaFileCoordinate coordinate = null) {
            if (null == coordinate) {
                coordinate = chaCtrl.nowCoordinate;
            }
            MonoBehaviour COBOCController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "CharaOverlaysBasedOnCoordinateController"));
            COBOCController.Invoke("OnCoordinateBeingSaved", new object[] { coordinate });
        }

        public static bool SetControllerFromCoordinate(ChaControl chaCtrl, ChaFileCoordinate coordinate = null) {
            if (null == coordinate) {
                coordinate = chaCtrl.nowCoordinate;
            }
            MonoBehaviour COBOCController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "CharaOverlaysBasedOnCoordinateController"));
            COBOCController.Invoke("OnCoordinateBeingLoaded", new object[] { coordinate, false });
            return true;
        }

        public static void SetControllerFromExtData(ChaControl chaCtrl) {
            MonoBehaviour COBOCController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "CharaOverlaysBasedOnCoordinateController"));
            COBOCController.Invoke("OnReload", new object[] { 2, false });
            return;
        }
    }
}
