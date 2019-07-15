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

using System.ComponentModel;
using BepInEx;
using BepInEx.Logging;
using Extension;
using Harmony;
using Studio;
using Logger = BepInEx.Logger;

namespace KK_StudioAutoCloseLoadingSceneWindow {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioAutoCloseLoadingSceneWindow : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Auto Close Loading Scene Window";
        internal const string GUID = "com.jim60105.kk.studioautocloseloadingscenewindow";
        internal const string PLUGIN_VERSION = "19.07.15.2";

        public void Awake() {
            HarmonyInstance.Create(GUID).PatchAll(typeof(Patches));
        }

        [DisplayName("Auto close after scene \"Load\"")]
        public static ConfigWrapper<bool> EnableOnLoad { get; } = new ConfigWrapper<bool>(nameof(EnableOnLoad), PLUGIN_NAME, true);

        [DisplayName("Auto close after scene \"Import\"")]
        public static ConfigWrapper<bool> EnableOnImport { get; } = new ConfigWrapper<bool>(nameof(EnableOnImport), PLUGIN_NAME, true);
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
                Logger.Log(LogLevel.Debug, "[KK_SACLS] Auto close load scene window");
            }
        }
    }
}
