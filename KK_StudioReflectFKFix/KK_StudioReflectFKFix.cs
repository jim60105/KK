/*
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMM               MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM    M7    MZ    MMO    MMMMM
MMM               MMMMMMMMMMMMM   MMM     MMMMMMMMMM    M$    MZ    MMO    MMMMM
MMM               MMMMMMMMMM       ?M     MMMMMMMMMM    M$    MZ    MMO    MMMMM
MMMMMMMMMMMM8     MMMMMMMM       ~MMM.    MMMMMMMMMM    M$    MZ    MMO    MMMMM
MMMMMMMMMMMMM     MMMMM        MMM                 M    M$    MZ    MMO    MMMMM
MMMMMMMMMMMMM     MM.         ZMMMMMM     MMMM     MMMMMMMMMMMMZ    MMO    MMMMM
MMMMMMMMMMMMM     MM      .   ZMMMMMM     MMMM     MMMMMMMMMMMM?    MMO    MMMMM
MMMMMMMMMMMMM     MMMMMMMM    $MMMMMM     MMMM     MMMMMMMMMMMM?    MM8    MMMMM
MMMMMMMMMMMMM     MMMMMMMM    7MMMMMM     MMMM     MMMMMMMMMMMMI    MM8    MMMMM
MMM               MMMMMMMM    7MMMMMM     MMMM    .MMMMMMMMMMMM.    MMMM?ZMMMMMM
MMM               MMMMMMMM.   ?MMMMMM     MMMM     MMMMMMMMMM ,:MMMMMM?    MMMMM
MMM           ..MMMMMMMMMM    =MMMMMM     MMMM     M$ MM$M7M $MOM MMMM     ?MMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM .+Z: M   :M M  MM   ?MMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Harmony;
using UnityEngine.SceneManagement;

namespace KK_StudioReflectFKFix 
{
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class KK_StudioReflectFKFix : BaseUnityPlugin
    {
        internal const string PLUGIN_NAME = "Studio Reflect FK Fix";
        internal const string GUID = "com.jim60105.kk.studioreflectfkfix";
        internal const string PLUGIN_VERSION = "19.05.16.2";

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
                        Logger.Log(LogLevel.Debug, "[KK_SRFF] Exception occured when patching: " + ex.ToString());
                    }
                }
                //HarmonyInstance.DEBUG = true;

                Patches.InitPatch(harmonyInstance);
                Logger.Log(LogLevel.Debug, "[KK_SRFF] Patch Insert Complete");
            }
        }

        public void Awake()
        {
            SceneManager.sceneLoaded += this.OnSceneLoaded;
        }
    }
}
