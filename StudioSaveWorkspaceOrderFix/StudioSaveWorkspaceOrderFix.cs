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
using HarmonyLib;
using Studio;
using System.Collections.Generic;

namespace StudioSaveWorkspaceOrderFix {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class StudioSaveWorkspaceOrderFix : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Save Workspace Order Fix";
        internal const string GUID = "com.jim60105.kks.studiosaveworkspaceorderfix";
        internal const string PLUGIN_VERSION = "21.09.28.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.1.0";

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            Extension.Logger.logger = Logger;
            Harmony.CreateAndPatchAll(typeof(Patches));
        }
    }
    class Patches {
        [HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.SaveScene))]
        public static void SaveScenePrefix(Studio.Studio __instance) {
            Dictionary<int, ObjectInfo> dicObject = __instance.sceneInfo.dicObject;
            List<TreeNodeObject> treeNodeObj = __instance.treeNodeCtrl.GetField("m_TreeNodeObject") as List<TreeNodeObject>;
            Dictionary<int, ObjectInfo> resultDicObject = new Dictionary<int, ObjectInfo>();

            foreach (TreeNodeObject t in treeNodeObj) {
                if (null == t) continue;

                if (Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(t, out ObjectCtrlInfo item) &&
                    dicObject.TryGetValue(item.objectInfo.dicKey, out ObjectInfo objInfo) &&
                    null != objInfo &&
                    objInfo == item.objectInfo
                ) {
                    resultDicObject.Add(objInfo.dicKey, objInfo);
                }
            }

            Extension.Logger.LogDebug($"{dicObject.Count} -> {resultDicObject.Count}");

            __instance.sceneInfo.dicObject = resultDicObject;
        }
    }
}
