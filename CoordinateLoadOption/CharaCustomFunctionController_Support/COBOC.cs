using System;
using System.Collections;
using Extension;
using UnityEngine;

namespace CoordinateLoadOption {
    class COBOC : CharaCustomFunctionController_Support {
        public override string GUID => "com.jim60105.kk.charaoverlaysbasedoncoordinate";
        public override string ControllerName => "CharaOverlaysBasedOnCoordinateController";
        public override string CCFCName => "Chara Overlay Based On Coordinate";
        public COBOC(ChaControl chaCtrl) : base(chaCtrl)
            => isExist = CoordinateLoadOption._isCharaOverlayBasedOnCoordinateExist;

        public override bool LoadAssembly() => LoadAssembly(out _, new Version(20, 4, 28, 0));

        public int IrisDisplaySide = 0;

        public void SetIrisDisplaySide() => SetIrisDisplaySide(DefaultChaCtrl);
        public void SetIrisDisplaySide(ChaControl chaCtrl) {
            MonoBehaviour controller = GetController(chaCtrl);
            if (controller.GetField("IrisDisplaySide") is int[] arr) {
                arr[chaCtrl.fileStatus.coordinateType] = IrisDisplaySide;
                //Controller.SetField("IrisDisplaySide", arr);
            }
        }

        public void GetIrisDisplaySide() => GetIrisDisplaySide(DefaultChaCtrl);
        public void GetIrisDisplaySide(ChaControl chaCtrl) {
            MonoBehaviour Controller = GetController(chaCtrl);
            IrisDisplaySide = (Controller.GetField("IrisDisplaySide") as int[])[chaCtrl.fileStatus.coordinateType];
        }

        public override object GetDataFromController(ChaControl chaCtrl)
            => GetController(chaCtrl).GetProperty("CurrentOverlay").ToDictionaryWithoutType();

        public void CopyCurrentCharaOverlayByController(ChaControl sourceChaCtrl, bool[] isChecked) {
            if (sourceChaCtrl != SourceChaCtrl) { GetControllerAndBackupData(sourceChaCtrl: sourceChaCtrl); }
            if (DefaultChaCtrl != TargetChaCtrl) { GetControllerAndBackupData(targetChaCtrl: DefaultChaCtrl); }

            object result = SourceBackup.ToDictionaryWithoutType();   //Clone it to use the structure
            result.RemoveAll(x => true);

            _ = SourceBackup.ForEach((x) => {
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
                    } else if (TargetBackup.TryGetValue(d.Key, out object val)) {
                        result.Add(d.Key, ((byte[])val).Clone());
                    } else {
                        result.Add(d.Key, new byte[] { });
                    }
                }
            });

            if (TargetController.SetProperty("CurrentOverlay", result)) {
                SetIrisDisplaySide();
                //單眼
                if (isChecked[0]) {
                    int[] sourceIris = (int[])SourceController.GetField("IrisDisplaySide");
                    int[] targetIris = (int[])TargetController.GetField("IrisDisplaySide");
                    int sourceType = SourceChaCtrl.fileStatus.coordinateType;
                    int targetType = TargetChaCtrl.fileStatus.coordinateType;
                    if (sourceIris.Length > sourceType && targetIris.Length > targetType) {
                        targetIris[targetType] = sourceIris[sourceType];
                    }
                    TargetController.SetField("IrisDisplaySide", targetIris);
                }
                TargetController.Invoke("OverwriteOverlay");
                Logger.LogDebug($"Copy Current CharaOverlay {SourceChaCtrl.fileParam.fullname} -> {TargetChaCtrl.fileParam.fullname}: {isChecked[0]} {isChecked[1]} {isChecked[2]}");
            }
        }

        public override void SetExtDataFromController(ChaControl chaCtrl) {
            MonoBehaviour controller = GetController(chaCtrl);

            controller.SetField("BackCoordinateType", (ChaFileDefine.CoordinateType)(((int)controller.GetField("BackCoordinateType")) % 6 + 1));
            controller.Invoke("ChangeCoordinate");
            controller.Invoke("SavePluginData", new object[] { false });
        }
    }
}
