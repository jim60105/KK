using Extension;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace KK_CoordinateLoadOption {
    class COBOC_Support {
        private static readonly BepInEx.Logging.ManualLogSource Logger = KK_CoordinateLoadOption.Logger;
        public static readonly string GUID = "com.jim60105.kk.charaoverlaysbasedoncoordinate";
        public static int IrisDisplaySide = 0;

        public static bool LoadAssembly() {
            if (null != Extension.Extension.TryGetPluginInstance(GUID, new System.Version(20, 4, 28, 0))) {
                Logger.LogDebug("KK_CharaOverlayBasedOnCoordinate found");
                return true;
            } else {
                Logger.LogDebug("Load assembly FAILED: KK_CharaOverlayBasedOnCoordinate");
                return false;
            }
        }

        public static void SetIrisDisplaySide(ChaControl chaCtrl) {
            MonoBehaviour Controller = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "CharaOverlaysBasedOnCoordinateController"));
            if (Controller.GetField("IrisDisplaySide") is int[] arr) {
                arr[chaCtrl.fileStatus.coordinateType] = IrisDisplaySide;
                //Controller.SetField("IrisDisplaySide", arr);
            }
        }

        public static void GetIrisDisplaySide(ChaControl chaCtrl) {
            MonoBehaviour Controller = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "CharaOverlaysBasedOnCoordinateController"));
            IrisDisplaySide = (Controller.GetField("IrisDisplaySide") as int[])[chaCtrl.fileStatus.coordinateType];
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
                    int sourceType = sourceChaCtrl.fileStatus.coordinateType;
                    int targetType = targetChaCtrl.fileStatus.coordinateType;
                    if (sourceIris.Length > sourceType && targetIris.Length > targetType) {
                        targetIris[targetType] = sourceIris[sourceType];
                    }
                    targetController.SetField("IrisDisplaySide", targetIris);
                }
                targetController.Invoke("OverwriteOverlay");
                Logger.LogDebug($"Copy Current CharaOverlay {sourceChaCtrl.fileParam.fullname} -> {targetChaCtrl.fileParam.fullname}: {isChecked[0]} {isChecked[1]} {isChecked[2]}");
            }
        }

        public static void SetExtDataFromController(ChaControl chaCtrl) {
            MonoBehaviour COBOCController = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, "CharaOverlaysBasedOnCoordinateController"));

            COBOCController.SetField("BackCoordinateType", (ChaFileDefine.CoordinateType)10);
            COBOCController.Invoke("ChangeCoordinate");
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
