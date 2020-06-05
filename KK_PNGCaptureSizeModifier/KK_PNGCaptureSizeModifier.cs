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
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace KK_PNGCaptureSizeModifier {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class KK_PNGCaptureSizeModifier : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "PNG Capture Size Modifier";
        internal const string GUID = "com.jim60105.kk.pngcapturesizemodifier";
        internal const string PLUGIN_VERSION = "20.06.05.1";
        internal const string PLUGIN_RELEASE_VERSION = "1.5.0";

        public static ConfigEntry<float> TimesOfMaker { get; private set; }
        public static ConfigEntry<float> TimesOfStudio { get; private set; }
        public static ConfigEntry<int> PNGColumnCount { get; private set; }
        public static ConfigEntry<bool> StudioSceneWatermark { get; private set; }
        public static ConfigEntry<bool> CharaMakerWatermark { get; private set; }
        public static ConfigEntry<bool> ResolutionWaterMark { get; private set; }

        public static ConfigEntry<string> PathToTheFontResource { get; private set; }
        public static ConfigEntry<float> CharacterSize { get; private set; }
        public static ConfigEntry<int> PositionX { get; private set; }
        public static ConfigEntry<int> PositionY { get; private set; }
        internal static new ManualLogSource Logger;
        internal static Texture2D fontTexture;
        public void Awake() {
            Logger = base.Logger;
            TimesOfMaker = Config.Bind<float>("Config", "Times of multiplication (Maker)", 3.0f, "The game needs to be restarted for changes to take effect.");
            TimesOfStudio = Config.Bind<float>("Config", "Times of multiplication (Studio)", 5.0f, "The game needs to be restarted for changes to take effect.");
            PNGColumnCount = Config.Bind<int>("Config", "Number of PNG rows in File List View", 3, "Must be a natural number");
            StudioSceneWatermark = Config.Bind<bool>("Config", "Use Studio Scene Watermark", true, "It is extremely NOT recommended to disable the watermark function, which is for distinguishing between scene data and normal image.");
            CharaMakerWatermark = Config.Bind<bool>("Config", "Use Character Watermark", true);
            ResolutionWaterMark = Config.Bind<bool>("Config", "Use Resolution Watermark", true, "When the StudioScene/Character watermark is enabled, the resolution watermark will be forced to use.");

            PathToTheFontResource = Config.Bind<String>("WaterMark", "Path of the font picture", "", "Full path to the font resource picture, must be a PNG or JPG.");
            PathToTheFontResource.SettingChanged += delegate { SetFontPic(); };
            SetFontPic();
            CharacterSize = Config.Bind<float>("WaterMark", "Character Size", 1.0f);
            PositionX = Config.Bind<int>("WaterMark", "Position X", 97, new ConfigDescription("0 = left, 100 = right", new AcceptableValueRange<int>(0, 100)));
            PositionY = Config.Bind<int>("WaterMark", "Position Y", 0, new ConfigDescription("0 = bottom, 100 = top", new AcceptableValueRange<int>(0, 100)));

            if (TimesOfMaker.Value == 0) TimesOfMaker.Value = (float)TimesOfMaker.DefaultValue;
            if (TimesOfStudio.Value == 0) TimesOfStudio.Value = (float)TimesOfStudio.DefaultValue;
            if (PNGColumnCount.Value == 0) PNGColumnCount.Value = (int)PNGColumnCount.DefaultValue;
            HarmonyWrapper.PatchAll(typeof(Patches));
        }

        private void SetFontPic() {
            fontTexture = null;
            if (PathToTheFontResource.Value.Length != 0 && File.Exists(PathToTheFontResource.Value)) {
                fontTexture = Extension.Extension.LoadTexture(PathToTheFontResource.Value);
                if (null != fontTexture) {
                    Logger.LogDebug($"Load font pic: {PathToTheFontResource.Value}");
                } else {
                    Logger.LogError("Load font picture FAILED: " + PathToTheFontResource.Value);
                }
            }

            if (null == fontTexture) {
                fontTexture = Extension.Extension.LoadDllResource($"KK_PNGCaptureSizeModifier.Resources.ArialFont.png", 1024, 1024);
                Logger.LogDebug($"Load original font pic");
            }

            TextToTexture.fontTexture = fontTexture;
        }
    }

    class Patches {
        //PNG存檔放大
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaCustom.CustomCapture), "CapCharaCard")]
        public static IEnumerable<CodeInstruction> CapCharaCardTranspiler(IEnumerable<CodeInstruction> instructions)
            => PngTranspiler(instructions, KK_PNGCaptureSizeModifier.TimesOfMaker.Value);

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaCustom.CustomCapture), "CapCoordinateCard")]
        public static IEnumerable<CodeInstruction> CapCoordinateCardTranspiler(IEnumerable<CodeInstruction> instructions)
            => PngTranspiler(instructions, KK_PNGCaptureSizeModifier.TimesOfMaker.Value);

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
        public static void UpdatePostFix(ChaCustom.CustomFileListCtrl __instance)
            => ChangeRowCount((ChaCustom.CustomFileWindow)__instance.GetField("cfWindow"));

        [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CustomCoordinateFile), "Start")]
        public static void StartPostFix(ChaCustom.CustomCoordinateFile __instance)
            => ChangeRowCount((ChaCustom.CustomFileWindow)__instance.GetField("fileWindow"));

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
                Singleton<ChaCustom.CustomBase>.Instance.StartCoroutine(UpdateLayout(component.transform as RectTransform));
            }
        }

        public static IEnumerator UpdateLayout(RectTransform rect) {
            yield return new WaitForEndOfFrame();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        //Studio預覽放大
        [HarmonyPostfix, HarmonyPatch(typeof(Studio.SceneLoadScene), "Awake")]
        public static void AwakePostFix() =>
            GameObject.Find("SceneLoadScene/Canvas Load Work/root").transform.localScale = new Vector3(2, 2, 1);

        //加浮水印
        private static bool SDFlag = false;
        private static bool CMFlag = false;

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.SceneInfo), "Save", new Type[] { typeof(string) })]
        public static void SavePrefix() => SDFlag = true;

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.GameScreenShot), "CreatePngScreen")]
        public static void CreatePngScreenPostfix(ref byte[] __result) {
            if (SDFlag) {
                AddWatermark(KK_PNGCaptureSizeModifier.StudioSceneWatermark.Value, ref __result, "sd_watermark.png", KK_PNGCaptureSizeModifier.TimesOfStudio);
                SDFlag = false;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CustomCapture), "CapCharaCard")]
        public static void CapCharaCardPrefix() => CMFlag = true;

        [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CustomCapture), "CreatePng")]
        public static void CreatePngPostfix(ref byte[] pngData) {
            if (CMFlag) {
                AddWatermark(CMFlag, ref pngData, "chara_watermark.png", KK_PNGCaptureSizeModifier.TimesOfMaker);
                CMFlag = false;
            }
        }

        private static void AddWatermark(bool doWatermarkFlag, ref byte[] basePng, string wmFileName, ConfigEntry<float> times) {
            if (!doWatermarkFlag && !KK_PNGCaptureSizeModifier.ResolutionWaterMark.Value) return;

            Texture2D screenshot = new Texture2D(2, 2);
            screenshot.LoadImage(basePng);

            //圖片分辨率
            if (KK_PNGCaptureSizeModifier.ResolutionWaterMark.Value || doWatermarkFlag) {
                string text = $"{screenshot.width}x{screenshot.height}";
                int textureWidth = TextToTexture.CalcTextWidthPlusTrailingBuffer(text, KK_PNGCaptureSizeModifier.CharacterSize.Value);
                Texture2D capsize = TextToTexture.CreateTextToTexture(text, 0, 0, textureWidth, KK_PNGCaptureSizeModifier.CharacterSize.Value);
                Extension.Extension.Scale(capsize, Convert.ToInt32(textureWidth / (float)times.DefaultValue * times.Value), Convert.ToInt32(textureWidth / (float)times.DefaultValue * times.Value));
                screenshot = Extension.Extension.OverwriteTexture(
                    screenshot,
                    capsize,
                    screenshot.width * KK_PNGCaptureSizeModifier.PositionX.Value / 100 - capsize.width,
                    screenshot.height * KK_PNGCaptureSizeModifier.PositionY.Value / 100
                );
                KK_PNGCaptureSizeModifier.Logger.LogDebug($"Add Resolution: {wmFileName}");
            }

            //浮水印
            if (doWatermarkFlag) {
                Texture2D watermark = Extension.Extension.LoadDllResource($"KK_PNGCaptureSizeModifier.Resources.{wmFileName}");
                Extension.Extension.Scale(watermark, Convert.ToInt32(230f / (float)times.DefaultValue * times.Value), Convert.ToInt32(230f / (float)times.DefaultValue * times.Value));
                screenshot = Extension.Extension.OverwriteTexture(
                    screenshot,
                    watermark,
                    0,
                    screenshot.height - watermark.height
                );
                KK_PNGCaptureSizeModifier.Logger.LogDebug($"Add Watermark: {wmFileName}");
            }

            basePng = screenshot.EncodeToPNG();
        }
    }
}
