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
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace PNGCaptureSizeModifier {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class PNGCaptureSizeModifier : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "PNG Capture Size Modifier";
        internal const string GUID = "com.jim60105.kk.pngcapturesizemodifier";
        internal const string PLUGIN_VERSION = "20.02.14.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.0.0";

        public static ConfigEntry<float> TimesOfMaker { get; private set; }
        public static ConfigEntry<float> TimesOfStudio { get; private set; }
        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            TimesOfMaker = Config.Bind<float>("Config", "Times of multiplication (Maker)", 3.0f, "The game needs to be restarted for changes to take effect.");
            TimesOfStudio = Config.Bind<float>("Config", "Times of multiplication (Studio)", 5.0f, "The game needs to be restarted for changes to take effect.");
            HarmonyWrapper.PatchAll(typeof(Patches));
        }
    }

    class Patches {
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaCustom.CustomCapture), "CapCharaCard")]
        public static IEnumerable<CodeInstruction> CapCharaCardTranspiler(IEnumerable<CodeInstruction> instructions) => PngTranspiler(instructions, PNGCaptureSizeModifier.TimesOfMaker.Value);

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaCustom.CustomCapture), "CapCoordinateCard")]
        public static IEnumerable<CodeInstruction> CapCoordinateCardTranspiler(IEnumerable<CodeInstruction> instructions) => PngTranspiler(instructions, PNGCaptureSizeModifier.TimesOfMaker.Value);

        [HarmonyTranspiler, HarmonyPatch(typeof(Studio.SceneInfo), "Save", new Type[] { typeof(string) })]
        public static IEnumerable<CodeInstruction> SaveTranspiler(IEnumerable<CodeInstruction> instructions) => PngTranspiler(instructions, PNGCaptureSizeModifier.TimesOfStudio.Value);

        private static IEnumerable<CodeInstruction> PngTranspiler(IEnumerable<CodeInstruction> instructions, float times) {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Ldc_I4) //width
                {
                    if (codes[i + 1].opcode == OpCodes.Ldc_I4) //height
                    {
                        codes[i].operand = (int)((int)codes[i].operand * times);
                        codes[i + 1].operand = (int)((int)codes[i + 1].operand * times);
                        break;
                    }
                }
            }
            return codes.AsEnumerable();
        }
    }
}
