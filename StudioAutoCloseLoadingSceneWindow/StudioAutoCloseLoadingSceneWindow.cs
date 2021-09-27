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
using BepInEx.Logging;
using Extension;
using HarmonyLib;
using Manager;
using Studio;
using System.Collections;
using UnityEngine;

namespace StudioAutoCloseLoadingSceneWindow
{
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class StudioAutoCloseLoadingSceneWindow : BaseUnityPlugin
    {
        internal const string PLUGIN_NAME = "Studio Auto Close Loading Scene Window";
        internal const string GUID = "com.jim60105.kks.studioautocloseloadingscenewindow";
        internal const string PLUGIN_VERSION = "21.09.27.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.1.1";

        public static ConfigEntry<bool> EnableOnLoad { get; private set; }
        public static ConfigEntry<bool> EnableOnImport { get; private set; }

        internal static new ManualLogSource Logger;
        public void Awake()
        {
            Logger = base.Logger;
            Extension.Logger.logger = Logger;
            Harmony.CreateAndPatchAll(typeof(Patches));

            EnableOnLoad = Config.Bind("Config", "Auto close after scene Loaded.", true);
            EnableOnImport = Config.Bind("Config", "Auto close after scene Imported.", true);
        }
    }

    class Patches
    {
        private static bool isLoading = false;
        private static SceneLoadScene sceneLoadScene;

        [HarmonyPrefix, HarmonyPatch(typeof(SceneLoadScene), "OnClickLoad")]
        public static void OnClickLoadPrefix(SceneLoadScene __instance)
            => StartLoad(StudioAutoCloseLoadingSceneWindow.EnableOnLoad.Value, __instance);

        [HarmonyPrefix, HarmonyPatch(typeof(SceneLoadScene), "OnClickImport")]
        public static void OnClickImportPrefix(SceneLoadScene __instance)
            => StartLoad(StudioAutoCloseLoadingSceneWindow.EnableOnImport.Value, __instance);

        private static void StartLoad(bool doLoad, SceneLoadScene __instance)
        {
            if (!doLoad) return;

            isLoading = true;
            sceneLoadScene = __instance;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Scene), "LoadStart")]
        public static void LoadStartPostfix(Scene.Data data)
        {
            if (isLoading && data.levelName == "StudioNotification")
            {
                _ = sceneLoadScene.StartCoroutine(UnloadLoadScene());
                isLoading = false;
            }
        }

        private static IEnumerator UnloadLoadScene()
        {
            // 原本的彈窗動畫是1秒，多加0.5秒以確保它回到LoadScene
            yield return new WaitForSeconds(1.5f);
            _ = sceneLoadScene.Invoke("OnClickClose");
        }
    }
}
