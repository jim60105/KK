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

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using Extension;
using HarmonyLib;
using Studio;

namespace KK_StudioAutoCloseLoadingSceneWindow {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    [BepInProcess("StudioNEOV2")]
    public class KK_StudioAutoCloseLoadingSceneWindow : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Auto Close Loading Scene Window";
        internal const string GUID = "com.jim60105.kk.studioautocloseloadingscenewindow";
        internal const string PLUGIN_VERSION = "19.11.08.0";
		internal const string PLUGIN_RELEASE_VERSION = "1.0.3";

        public static ConfigEntry<bool> EnableOnLoad { get; private set; }
        public static ConfigEntry<bool> EnableOnImport { get; private set; }

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            HarmonyWrapper.PatchAll(typeof(Patches));

            EnableOnLoad = Config.AddSetting("Config", "Auto close after scene Loaded.", true);
            EnableOnImport = Config.AddSetting("Config", "Auto close after scene Imported.", true);
        }
    }

    class Patches {
        private static bool isLoading = false;
        private static SceneLoadScene sceneLoadScene;

        [HarmonyPrefix, HarmonyPatch(typeof(SceneLoadScene), "OnClickLoad")]
        public static void OnClickLoadPrefix(SceneLoadScene __instance) {
            if (KK_StudioAutoCloseLoadingSceneWindow.EnableOnLoad.Value)
                StartLoad(__instance);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SceneLoadScene), "OnClickImport")]
        public static void OnClickImportPrefix(SceneLoadScene __instance) {
            if (KK_StudioAutoCloseLoadingSceneWindow.EnableOnImport.Value)
                StartLoad(__instance);
        }

        private static void StartLoad(SceneLoadScene __instance) {
            isLoading = true;
            sceneLoadScene = __instance;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Manager.Scene), nameof(Manager.Scene.LoadStart))]
        public static void LoadReservePostfix(Manager.Scene __instance, Manager.Scene.Data data) {
            if (isLoading && data.levelName == "StudioNotification") {
                isLoading = false;
                sceneLoadScene.Invoke("OnClickClose");
                KK_StudioAutoCloseLoadingSceneWindow.Logger.LogDebug("Auto close load scene window");
            }
        }
    }
}
