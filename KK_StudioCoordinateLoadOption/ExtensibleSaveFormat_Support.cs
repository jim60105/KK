using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Logger = BepInEx.Logger;
using BepInEx;
using Harmony;

namespace KK_StudioCoordinateLoadOption
{
    class ExtensibleSaveFormat_Support
    {
        private static readonly BindingFlags priviteFlag = BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance|BindingFlags.Static;
        private static readonly BindingFlags publicFlag = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance|BindingFlags.Static;
        private static Type PluginData = null;
        private static Type ExtSaveFoType = null;

        public static bool LoadAssembly()
        {
            try
            {
                Assembly ass = Assembly.LoadFrom("BepInEx/ExtensibleSaveFormat.dll");

                PluginData = ass.GetType("ExtensibleSaveFormat.PluginData");
                ExtSaveFoType = ass.GetType("ExtensibleSaveFormat.ExtendedSave");
                if (null == ExtSaveFoType||null==PluginData)
                {
                    throw new Exception("[KK_SCLO] Load assembly FAILED: ExtensibleSaveFormat");
                }
                Logger.Log(LogLevel.Debug, "[KK_SCLO] ExtensibleSaveFormat found");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex.Message);
                return false;
            }
        }

        public static void CallCoordinateReadEvent(ChaFileCoordinate file)
        {
            if (null == ExtSaveFoType)
            {
                Logger.Log(LogLevel.Error, "[KK_SCLO] ExtType Null");
                return;
            }
            if(null==ExtSaveFoType.GetMethod("coordinateReadEvent", AccessTools.all))
            {
                Logger.Log(LogLevel.Error, "[KK_SCLO] Method Null");
                return;
            }
            if (null == file)
            {
                Logger.Log(LogLevel.Error, "[KK_SCLO] File Null");
                return;
            }
            ExtSaveFoType.GetMethod("coordinateReadEvent", priviteFlag).Invoke(null,new object[] { file });
            Logger.Log(LogLevel.Debug, "[KK_SCLO] ExtensibleSaveFormat: Called coordinateReadEvent()");
        }
        static object extendedDataById = null;
        public static void CopyExtendedData(ChaFileCoordinate file, string id)
        {
            extendedDataById = ExtSaveFoType.GetMethod("GetExtendedDataById", new Type[] { typeof(ChaFileCoordinate), typeof(string) }).Invoke(null, new object[] { file, id });
            if (null != extendedDataById)
            {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Get Extended Data Success");
            }
            else
            {
                Logger.Log(LogLevel.Error, "[KK_SCLO] Get Extended Data FAILED");

            }
        }

        public static void RollbackExtendedData(ChaFileCoordinate file, string id)
        {
            ExtSaveFoType.GetMethod("SetExtendedDataById", new Type[] { typeof(ChaFileCoordinate), typeof(string), PluginData }).Invoke(null, new object[] { file, id, extendedDataById });
            if (null != extendedDataById)
            {
                Logger.Log(LogLevel.Debug, "[KK_SCLO] Set Extended Data Success");
            }
            else
            {
                Logger.Log(LogLevel.Error, "[KK_SCLO] Set Extended Data FAILED");

            }
        }

    }
}
