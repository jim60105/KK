using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Harmony;
using UnityEngine.SceneManagement;

namespace KK_StudioCoordinateLoadOption
{
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class KK_StudioCoordinateLoadOption : BaseUnityPlugin
    {
        internal const string PLUGIN_NAME = "Studio Coordinate Load Option";
        internal const string GUID = "com.jim60105.kk.studiocoordinateloadoption";
        internal const string PLUGIN_VERSION = "19.04.01.0";

        private bool _isInit = false;
        public static bool _isKCOXExist = false;
        public static bool _isABMXExist = false;

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            string name = SceneManager.GetActiveScene().name;
            bool flag2 = !_isInit && name == "Studio";
            if (flag2)
            {
                _isInit = true;
                UILib.UIUtility.Init();
                HarmonyInstance harmonyInstance = HarmonyInstance.Create(GUID);
                foreach (Type type2 in Assembly.GetExecutingAssembly().GetTypes())
                {
                    try
                    {
                        List<HarmonyMethod> harmonyMethods = HarmonyMethodExtensions.GetHarmonyMethods(type2);
                        if (harmonyMethods != null && harmonyMethods.Count > 0)
                        {
                            HarmonyMethod harmonyMethod = HarmonyMethod.Merge(harmonyMethods);
                            new PatchProcessor(harmonyInstance, type2, harmonyMethod).Patch();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Debug, "[KK_SCLO] Exception occured when patching: " + ex.ToString());
                    }
                }
                //HarmonyInstance.DEBUG = true;
                CostumeInfo_Patches.InitPatch(harmonyInstance);
            }
        }

        public void Awake()
        {
            SceneManager.sceneLoaded += this.OnSceneLoaded;
        }

        public void Start()
        {
            _isKCOXExist = IsPluginExist("KCOX") && KCOX_Support.LoadAssembly();
            _isABMXExist = IsPluginExist("KKABMX.Core") && ABMX_Support.LoadAssembly();
        }

        private bool IsPluginExist(string pluginName)
        {
            return Extension.Extensions.CheckRequiredPlugin(this, pluginName);
        }
    }
}
