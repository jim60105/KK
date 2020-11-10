using Extension;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KK_CoordinateLoadOption {
    //CCFC: CharaCustomFunctionController
    abstract class CCFCSupport {
        internal static readonly BepInEx.Logging.ManualLogSource Logger = KK_CoordinateLoadOption.Logger;
        public abstract string GUID { get; }
        public abstract string ControllerName { get; }
        public abstract string CCCName { get; }
        public CCFCSupport(ChaControl chaCtrl) {
            if (null != chaCtrl) {
                DefaultController = GetController(chaCtrl);
                DefaultChaCtrl = chaCtrl;
            }
        }
        internal ChaControl DefaultChaCtrl;
        internal MonoBehaviour DefaultController;

        public virtual bool LoadAssembly() => LoadAssembly(out _);

        internal bool LoadAssembly(out string path, Version version = null) {
            try {
                path = Extension.Extension.TryGetPluginInstance(GUID, version)?.Info.Location;
                if (!File.Exists(path)) {
                    throw new Exception($"Load assembly FAILED: {CCCName}");
                }
                Logger.LogDebug($"{CCCName} found");
                return true;
            } catch (Exception ex) {
                Logger.LogDebug(ex.Message);
                path = "";
                return false;
            }
        }

        /// <summary>
        /// 取得CharaCustomFunctionController
        /// </summary>
        /// <param name="chaCtrl">對象ChaControl</param>
        /// <returns>CharaCustomFunctionController</returns>
        public MonoBehaviour GetController(ChaControl chaCtrl) {
            if (!(chaCtrl is ChaControl)) {
                Logger.LogDebug("No ChaControl found");
                return null;
            }
            if (DefaultChaCtrl == chaCtrl) return DefaultController;

            MonoBehaviour controller = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, ControllerName));
            if (null == controller) { Logger.LogDebug($"No {CCCName} Controller found"); }

            return controller;
        }

        /// <summary>
        /// 由ExtData載入ExtData至Controller內
        /// </summary>
        /// <returns></returns>
        public void SetControllerFromExtData() => SetControllerFromExtData(DefaultChaCtrl);
        /// <summary>
        /// 由ExtData載入ExtData至Controller內
        /// </summary>
        /// <param name="chaCtrl">要被設定的ChaControl</param>
        /// <returns></returns>
        public void SetControllerFromExtData(ChaControl chaCtrl) {
            MonoBehaviour controller = GetController(chaCtrl);
            controller.Invoke("OnReload", new object[] { 2, false });
        }

        /// <summary>
        /// 由Controller載入ExtData至ExtData內
        /// </summary>
        /// <returns></returns>
        public void SetExtDataFromController() => SetExtDataFromController(DefaultChaCtrl);
        /// <summary>
        /// 由Controller載入ExtData至ExtData內
        /// </summary>
        /// <param name="chaCtrl">要被設定的ChaControl</param>
        /// <returns></returns>
        public void SetExtDataFromController(ChaControl chaCtrl) {
            MonoBehaviour controller = GetController(chaCtrl);
            controller.Invoke("OnCardBeingSaved", new object[] { 1 });
        }

        /// <summary>
        /// 由Coordinate載入ExtData至Controller內
        /// </summary>
        /// <param name="coordinate">要載入的coordibate</param>
        public void SetControllerFromCoordinate(ChaFileCoordinate coordinate = null) => SetControllerFromCoordinate(DefaultChaCtrl, coordinate);
        /// <summary>
        /// 由Coordinate載入ExtData至Controller內
        /// </summary>
        /// <param name="chaCtrl">要被設定的ChaControl</param>
        /// <param name="coordinate">要載入的coordibate</param>
        public void SetControllerFromCoordinate(ChaControl chaCtrl, ChaFileCoordinate coordinate = null) {
            if (null == coordinate) {
                coordinate = chaCtrl.nowCoordinate;
            }
            MonoBehaviour controller = GetController(chaCtrl);
            controller.Invoke("OnCoordinateBeingLoaded", new object[] { coordinate, false });
            return;
        }

        /// <summary>
        /// 將Controller內之ExtData儲存至Coordinate ExtendedData內
        /// </summary>
        /// <param name="coordinate">目標Coordinate</param>
        public void SetCoordinateExtDataFromController(ChaFileCoordinate coordinate = null) => SetCoordinateExtDataFromController(DefaultChaCtrl, coordinate);
        /// <summary>
        /// 將Controller內之ExtData儲存至Coordinate ExtendedData內
        /// </summary>
        /// <param name="chaCtrl">來源ChaControl</param>
        /// <param name="coordinate">目標Coordinate</param>
        public void SetCoordinateExtDataFromController(ChaControl chaCtrl, ChaFileCoordinate coordinate = null) {
            if (null == coordinate) {
                coordinate = chaCtrl.nowCoordinate;
            }
            MonoBehaviour controller = GetController(chaCtrl);
            controller.Invoke("OnCoordinateBeingSaved", new object[] { coordinate });
        }

        internal static ChaControl SourceChaCtrl;
        internal static ChaControl TargetChaCtrl;
        internal static MonoBehaviour SourceController;
        internal static MonoBehaviour TargetController;
        internal static object SourceBackup = null;
        internal static object TargetBackup = null;

        /// <summary>
        /// Copy前準備Source和Target資料
        /// </summary>
        /// <param name="sourceChaCtrl">來源ChaControl</param>
        /// <param name="targetChaCtrl">目標ChaControl</param>
        public bool GetControllerAndBackupData(ChaControl sourceChaCtrl = null, ChaControl targetChaCtrl = null) {
            if (null != sourceChaCtrl) {
                Logger.LogDebug($"Source {CCCName}-----");
                SourceChaCtrl = sourceChaCtrl;
                SourceController = GetController(sourceChaCtrl);
                if (null == SourceController) {
                    Logger.LogDebug($"No Source {CCCName} Controller found on {sourceChaCtrl.fileParam.fullname}");
                    return false;
                }
                SourceBackup = GetExtDataFromController(sourceChaCtrl);
            }

            if (null != targetChaCtrl) {
                Logger.LogDebug($"Target {CCCName}-----");
                TargetChaCtrl = targetChaCtrl;
                TargetController = GetController(targetChaCtrl);
                if (null == TargetController) {
                    Logger.LogDebug($"No Target {CCCName} Controller found on {targetChaCtrl.fileParam.fullname}");
                    return false;
                }
                TargetBackup = GetExtDataFromController(targetChaCtrl);
            }
            return true;
        }

        /// <summary>
        /// 從Controller取得給定ChaControl的ExtData
        /// </summary>
        /// <returns>ExtData</returns>
        public object GetExtDataFromController() => GetExtDataFromController(DefaultChaCtrl);

        /// <summary>
        /// 從Controller取得給定ChaControl的ExtData
        /// </summary>
        /// <param name="chaCtrl">要查詢的ChaControl</param>
        /// <returns>ExtData</returns>
        public abstract object GetExtDataFromController(ChaControl chaCtrl);

        public static void ClearBackup() {
            SourceChaCtrl = null;
            TargetChaCtrl = null;
            SourceBackup = null;
            TargetBackup = null;
            SourceController = null;
            TargetController = null;
        }
    }
}
