using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using Harmony;
using UnityEngine.SceneManagement;

namespace KK_StudioCoordinateLoadOption
{
	[BepInPlugin("com.jim60105.kk.studiocoordinateloadoption", "StudioCoordinateLoadOption", "19.03.23.0")]
	public class KK_StudioCoordinateLoadOption: BaseUnityPlugin
	{
        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            string name = SceneManager.GetActiveScene().name;
            bool flag2 = !this.isInit && name == "Studio";
            if (flag2)
            {
                Console.WriteLine(flag2);
                this.isInit = true;
                UILib.UIUtility.Init();
                HarmonyInstance harmonyInstance = HarmonyInstance.Create("com.jim60105.kk.studiocoordinateloadoption");
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
                        UnityEngine.Debug.Log("KK_SCLO: Exception occured when patching: " + ex.ToString());
                    }
                }
                CostumeInfo_Init_Patches.ManualPatch(harmonyInstance);
                Console.WriteLine("KK_SCLO:Patch Insert Complete.");
            }
        }

        public void Awake()
        {
            SceneManager.sceneLoaded += this.OnSceneLoaded;
        }

		internal static readonly string PLUGIN_NAME = "StudioCoordinateLoadOption";

		internal static readonly string PLUGIN_VERSION = "19.03.23.0";

        private bool isInit = false;
	}
}
