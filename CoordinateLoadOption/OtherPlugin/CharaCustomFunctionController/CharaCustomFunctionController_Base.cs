﻿using Extension;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CoordinateLoadOption.OtherPlugin.CharaCustomFunctionController
{
    abstract class CharaCustomFunctionController_Base
    {
        internal static readonly BepInEx.Logging.ManualLogSource Logger = CoordinateLoadOption.Logger;
        public abstract string GUID { get; }
        public abstract string ControllerName { get; }
        public abstract string CCFCName { get; }
        public bool isExist { get; internal set; }

        internal ChaControl DefaultChaCtrl;
        internal ChaControl SourceChaCtrl;
        internal ChaControl TargetChaCtrl;
        internal MonoBehaviour DefaultController;
        internal MonoBehaviour SourceController;
        internal MonoBehaviour TargetController;
        internal object SourceBackup = null;
        internal object TargetBackup = null;

        public CharaCustomFunctionController_Base(ChaControl chaCtrl)
        {
            if (null != chaCtrl)
            {
                DefaultChaCtrl = chaCtrl;
                DefaultController = GetController(DefaultChaCtrl);
            }
        }

        public virtual bool LoadAssembly() => LoadAssembly(out _);

        internal bool LoadAssembly(out string path, Version version = null)
        {
            try
            {
                path = KoikatuHelper.TryGetPluginInstance(GUID, version)?.Info.Location;
                if (!File.Exists(path))
                {
                    throw new Exception($"Load assembly FAILED: {CCFCName}");
                }
                Logger.LogDebug($"{CCFCName} found");
                isExist = true;
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex.Message);
                path = "";
                isExist = false;
            }
            return isExist;
        }

        /// <summary>
        /// 取得CharaCustomFunctionController
        /// </summary>
        /// <param name="chaCtrl">對象ChaControl</param>
        /// <returns>CharaCustomFunctionController</returns>
        public MonoBehaviour GetController(ChaControl chaCtrl)
        {
            if (!(chaCtrl is ChaControl))
            {
                Logger.LogDebug("No ChaControl found");
                return null;
            }
            if (DefaultChaCtrl == chaCtrl && null != DefaultController) return DefaultController;

            MonoBehaviour controller = chaCtrl.GetComponents<MonoBehaviour>().FirstOrDefault(x => Equals(x.GetType().Name, ControllerName));
            if (null == controller) { Logger.LogDebug($"No {CCFCName} Controller found"); }

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
        public void SetControllerFromExtData(ChaControl chaCtrl)
        {
            MonoBehaviour controller = GetController(chaCtrl);
            controller.Invoke("OnReload", new object[] { CoordinateLoadOption.insideStudio ? 2 : 1, false });
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
        public virtual void SetExtDataFromController(ChaControl chaCtrl)
        {
            MonoBehaviour controller = GetController(chaCtrl);
            controller.Invoke("OnCardBeingSaved", new object[] { 1 });
            controller.Invoke("OnCoordinateBeingSaved", new object[] { chaCtrl.nowCoordinate });
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
        public void SetControllerFromCoordinate(ChaControl chaCtrl, ChaFileCoordinate coordinate = null)
        {
            if (null == coordinate)
            {
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
        public void SetCoordinateDataFromController(ChaFileCoordinate coordinate = null) => SetCoordinateDataFromController(DefaultChaCtrl, coordinate);
        /// <summary>
        /// 將Controller內之ExtData儲存至Coordinate ExtendedData內
        /// </summary>
        /// <param name="chaCtrl">來源ChaControl</param>
        /// <param name="coordinate">目標Coordinate</param>
        public void SetCoordinateDataFromController(ChaControl chaCtrl, ChaFileCoordinate coordinate = null)
        {
            if (null == coordinate)
            {
                coordinate = chaCtrl.nowCoordinate;
            }
            MonoBehaviour controller = GetController(chaCtrl);
            controller.Invoke("OnCoordinateBeingSaved", new object[] { coordinate });
        }

        /// <summary>
        /// Copy前準備Source和Target資料
        /// </summary>
        /// <param name="sourceChaCtrl">來源ChaControl</param>
        /// <param name="targetChaCtrl">目標ChaControl</param>
        public virtual bool GetControllerAndBackupData(ChaControl sourceChaCtrl = null, ChaControl targetChaCtrl = null)
        {
            if (null != sourceChaCtrl)
            {
                Logger.LogDebug($"----- Get Source {CCFCName} -----");
                SourceChaCtrl = sourceChaCtrl;
                SourceController = GetController(sourceChaCtrl);
                if (null == SourceController)
                {
                    Logger.LogDebug($"No Source {CCFCName} Controller found on {sourceChaCtrl.fileParam.fullname}");
                    return false;
                }
                SourceBackup = GetDataFromController(sourceChaCtrl);
            }

            if (null != targetChaCtrl)
            {
                Logger.LogDebug($"----- Get Target {CCFCName} -----");
                TargetChaCtrl = targetChaCtrl;
                TargetController = GetController(targetChaCtrl);
                if (null == TargetController)
                {
                    Logger.LogDebug($"No Target {CCFCName} Controller found on {targetChaCtrl.fileParam.fullname}");
                    return false;
                }
                TargetBackup = GetDataFromController(targetChaCtrl);
            }
            return true;
        }

        /// <summary>
        /// 從Controller取得給定ChaControl的ExtData
        /// </summary>
        /// <returns>ExtData</returns>
        public object GetDataFromController() => GetDataFromController(DefaultChaCtrl);

        /// <summary>
        /// 從Controller取得給定ChaControl的ExtData
        /// </summary>
        /// <param name="chaCtrl">要查詢的ChaControl</param>
        /// <returns>ExtData</returns>
        public abstract object GetDataFromController(ChaControl chaCtrl);

        public virtual bool CheckControllerPrepared() => CheckControllerPrepared(DefaultChaCtrl);
        public virtual bool CheckControllerPrepared(ChaControl chaCtrl) => CheckControllerPrepared(chaCtrl, (_) => true);
        public virtual bool CheckControllerPrepared(Func<MonoBehaviour, bool> func) => CheckControllerPrepared(DefaultChaCtrl, func);
        public virtual bool CheckControllerPrepared(ChaControl chaCtrl, Func<MonoBehaviour, bool> func)
        {
            if (!isExist) return true;

            MonoBehaviour controller = GetController(chaCtrl);
            return null != controller && func(controller);
        }

        public virtual void ClearBackup()
        {
            SourceChaCtrl = null;
            TargetChaCtrl = null;
            SourceBackup = null;
            TargetBackup = null;
            SourceController = null;
            TargetController = null;
        }
    }
}
