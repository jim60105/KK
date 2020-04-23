using Extension;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_StudioCoordinateLoadOption {
    class COBOC_Support {
        private static readonly BepInEx.Logging.ManualLogSource Logger = KK_StudioCoordinateLoadOption.Logger;
        private static object COBOCController;

        public static bool LoadAssembly() {
            if(null != Extension.Extension.TryGetPluginInstance("com.jim60105.kk.charaoverlaysbasedoncoordinate", new System.Version(20,3,21,0))) {
                Logger.LogDebug("KK_CharaOverlayBasedOnCoordinate found");
                return true;
            } else {
                Logger.LogDebug("Load assembly FAILED: KK_CharaOverlayBasedOnCoordinate");
                return false;
            }
        }

        public static void CleanKCOXBackup() {
            return;
        }
    }
}
