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
using BepInEx.Logging;
using Extension;
using Harmony;
using Studio;
using Logger = BepInEx.Logger;

namespace KK_StudioAutoCloseLoadScene {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioAutoCloseLoadScene : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Auto Close Load Scene";
        internal const string GUID = "com.jim60105.kk.studioautocloseloadscene";
        internal const string PLUGIN_VERSION = "19.07.10.0";

        public void Awake() {
            BepInEx.Config.ReloadConfig();
            if (string.Equals(BepInEx.Config.GetEntry("enabled", "True", PLUGIN_NAME), "True")) {
                HarmonyInstance.Create(GUID).PatchAll(typeof(Patches));
            }
        }
    }

    class Patches {
        [HarmonyPostfix, HarmonyPatch(typeof(SceneLoadScene), "OnClickLoad")]
        public static void OnClickLoadPostfix(SceneLoadScene __instance) => Close(__instance);
        [HarmonyPostfix, HarmonyPatch(typeof(SceneLoadScene), "OnClickImport")]
        public static void OnClickImportPostfix(SceneLoadScene __instance) => Close(__instance);

        private static void Close(SceneLoadScene __instance) {
            __instance.Invoke("OnClickClose");
            Logger.Log(LogLevel.Debug, "[KK_SACLS] Auto close load scene window");
        }
    }
}
