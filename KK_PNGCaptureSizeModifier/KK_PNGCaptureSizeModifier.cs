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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace KK_PNGCaptureSizeModifier {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class KK_PNGCaptureSizeModifier : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "PNG Capture Size Modifier";
        internal const string GUID = "com.jim60105.kk.pngcapturesizemodifier";
        internal const string PLUGIN_VERSION = "20.04.30.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.3.0";

        public static ConfigEntry<float> TimesOfMaker { get; private set; }
        public static ConfigEntry<float> TimesOfStudio { get; private set; }
        public static ConfigEntry<int> PNGColumnCount { get; private set; }
        public static ConfigEntry<bool> StudioSceneWatermark { get; private set; }
        public static ConfigEntry<bool> CharaMakerWatermark { get; private set; }
        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            TimesOfMaker = Config.Bind<float>("Config", "Times of multiplication (Maker)", 3.0f, "The game needs to be restarted for changes to take effect.");
            TimesOfStudio = Config.Bind<float>("Config", "Times of multiplication (Studio)", 5.0f, "The game needs to be restarted for changes to take effect.");
            PNGColumnCount = Config.Bind<int>("Config", "Number of PNG rows in File List View", 3, "Must be a natural number");
            StudioSceneWatermark = Config.Bind<bool>("Config", "Use Studio Scene Watermark", true, "It is extremely NOT recommended to disable the watermark function, which is for distinguishing between scene data and normal image.");
            CharaMakerWatermark = Config.Bind<bool>("Config", "Use Character Watermark", true);
            HarmonyWrapper.PatchAll(typeof(Patches));
        }
    }

    class Patches {
        //PNG存檔放大
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaCustom.CustomCapture), "CapCharaCard")]
        public static IEnumerable<CodeInstruction> CapCharaCardTranspiler(IEnumerable<CodeInstruction> instructions) => PngTranspiler(instructions, KK_PNGCaptureSizeModifier.TimesOfMaker.Value);

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaCustom.CustomCapture), "CapCoordinateCard")]
        public static IEnumerable<CodeInstruction> CapCoordinateCardTranspiler(IEnumerable<CodeInstruction> instructions) => PngTranspiler(instructions, KK_PNGCaptureSizeModifier.TimesOfMaker.Value);

        [HarmonyTranspiler, HarmonyPatch(typeof(Studio.SceneInfo), "Save", new Type[] { typeof(string) })]
        public static IEnumerable<CodeInstruction> SaveTranspiler(IEnumerable<CodeInstruction> instructions) {
            return PngTranspiler(instructions, KK_PNGCaptureSizeModifier.TimesOfStudio.Value);
        }

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

        //CharaMaker存檔顯示放大
        [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CustomFileListCtrl), "Update")]
        public static void UpdatePostFix(ChaCustom.CustomFileListCtrl __instance) => ChangeRowCount((ChaCustom.CustomFileWindow)__instance.GetField("cfWindow"));

        [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CustomCoordinateFile), "Start")]
        public static void StartPostFix(ChaCustom.CustomCoordinateFile __instance) => ChangeRowCount((ChaCustom.CustomFileWindow)__instance.GetField("fileWindow"));

        public static void ChangeRowCount(ChaCustom.CustomFileWindow window) {
            GridLayoutGroup component = window.gameObject.GetComponentInChildren<GridLayoutGroup>();
            int count = KK_PNGCaptureSizeModifier.PNGColumnCount.Value;
            if (component.constraintCount != count) {
                if (count == 0) {
                    KK_PNGCaptureSizeModifier.PNGColumnCount.Value = (int)KK_PNGCaptureSizeModifier.PNGColumnCount.DefaultValue;
                    return;
                }
                RectTransform rect = component.transform as RectTransform;
                component.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                component.constraintCount = count;
                float width = (rect.rect.width - (9 * (count - 1)) - 20) / count;
                component.cellSize = new Vector2(width, width / component.cellSize.x * component.cellSize.y);
                UpdateLayout(component.transform as RectTransform);
            }
        }

        public static IEnumerator UpdateLayout(RectTransform rect) {
            //在某些奇妙的狀況會需要多呼叫兩次
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            yield return new WaitForEndOfFrame();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            yield return new WaitForEndOfFrame();
        }

        //Studio預覽放大
        [HarmonyPostfix, HarmonyPatch(typeof(Studio.SceneLoadScene), "Awake")]
        public static void AwakePostFix() => 
            GameObject.Find("SceneLoadScene/Canvas Load Work/root").transform.localScale = new Vector3(2, 2, 1);

        //加浮水印
        private static bool AddSDWatermarkFlag = false;
        [HarmonyPrefix, HarmonyPatch(typeof(Studio.SceneInfo), "Save", new Type[] { typeof(string) })]
        public static void SavePrefix() => AddSDWatermarkFlag = KK_PNGCaptureSizeModifier.StudioSceneWatermark.Value;

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.GameScreenShot), "CreatePngScreen")]
        public static void CreatePngScreenPostfix(ref byte[] __result) => AddWatermark(ref AddSDWatermarkFlag, ref __result, "sd_watermark.png",KK_PNGCaptureSizeModifier.TimesOfStudio);

        private static bool AddCharaWatermarkFlag = false;
        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CustomCapture), "CapCharaCard")]
        public static void CapCharaCardPrefix() => AddCharaWatermarkFlag = KK_PNGCaptureSizeModifier.CharaMakerWatermark.Value;

        [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CustomCapture), "CreatePng")]
        public static void CreatePngPostfix(ref byte[] pngData) => AddWatermark(ref AddCharaWatermarkFlag, ref pngData, "chara_watermark.png",KK_PNGCaptureSizeModifier.TimesOfMaker);

        private static void AddWatermark(ref bool doFlag, ref byte[] basePng,string wmFileName,ConfigEntry<float> times) {
            if (doFlag) {
                doFlag = false;
                Texture2D screenshot = new Texture2D(2, 2);
                screenshot.LoadImage(basePng);
                Texture2D watermark = Extension.Extension.LoadDllResource($"KK_PNGCaptureSizeModifier.Resources.{wmFileName}", 230, 230);
                TextureScaler.scale(watermark, Convert.ToInt32(230f / (float)times.DefaultValue * times.Value), Convert.ToInt32(230f /(float) times.DefaultValue * times.Value));

                screenshot = Extension.Extension.AddWatermark(screenshot, watermark, 0, screenshot.height - watermark.height);
                basePng = screenshot.EncodeToPNG();
                KK_PNGCaptureSizeModifier.Logger.LogDebug($"Add Watermark:{wmFileName}");
            }
        }
    }
}
