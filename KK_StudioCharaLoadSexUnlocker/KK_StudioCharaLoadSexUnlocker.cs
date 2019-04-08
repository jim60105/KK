using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Harmony;
using UnityEngine.SceneManagement;
using Extension;

namespace KK_StudioCharaLoadSexUnlocker
{
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class KK_StudioCharaLoadSexUnlocker: BaseUnityPlugin
    {
        internal const string PLUGIN_NAME = "Studio Chara Load Sex Unlocker";
        internal const string GUID = "com.jim60105.kk.studiocharaloadsexunlocker";
        internal const string PLUGIN_VERSION = "19.04.08.1";

        private bool _isInit = false;

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            string name = SceneManager.GetActiveScene().name;
            bool flag2 = !_isInit && name == "Studio";
            if (flag2)
            {
                _isInit = true;
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
                        Logger.Log(LogLevel.Debug, "[KK_SCLSU] Exception occured when patching: " + ex.ToString());
                    }
                }
                //HarmonyInstance.DEBUG = true;
                CharaList_Patches.InitPatch(harmonyInstance);
                if (!Extension.Extension.CheckRequiredPlugin(this, "marco.kkapi"))
                {
                    Logger.Log(LogLevel.Message, "[KK_SCLSU] KKAPI Not Found. "+PLUGIN_NAME+" CANNOT work correctly.");
                    Logger.Log(LogLevel.Message, "[KK_SCLSU] To be precise, there is no body change between male and female.");
                }
            }
        }

        public void Awake()
        {
            SceneManager.sceneLoaded += this.OnSceneLoaded;
        }
    }
}
