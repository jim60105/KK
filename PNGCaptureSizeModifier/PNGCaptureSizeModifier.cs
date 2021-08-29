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
using ChaCustom;
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
using Logger = Extension.Logger;

namespace PNGCaptureSizeModifier
{
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class PNGCaptureSizeModifier : BaseUnityPlugin
    {
        internal const string PLUGIN_NAME = "PNG Capture Size Modifier";
        internal const string GUID = "com.jim60105.kks.pngcapturesizemodifier";
        internal const string PLUGIN_VERSION = "21.08.29.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.6.1";

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
        public void Awake()
        {
            Logger = base.Logger;
            Extension.Logger.logger = Logger;
            TimesOfMaker = Config.Bind("Config", "Times of multiplication (Maker)", 3.0f, "The game needs to be restarted for changes to take effect.");
            TimesOfStudio = Config.Bind("Config", "Times of multiplication (Studio)", 5.0f, "The game needs to be restarted for changes to take effect.");
            PNGColumnCount = Config.Bind("Config", "Number of PNG rows in File List View", 3, new ConfigDescription("Must be a natural number", new AcceptableValueRange<int>(1, 30)));
            StudioSceneWatermark = Config.Bind("Config", "Use Studio Scene Watermark", true, "It is extremely NOT recommended to disable the watermark function, which is for distinguishing between scene data and normal image.");
            CharaMakerWatermark = Config.Bind("Config", "Use Character Watermark", true);
            ResolutionWaterMark = Config.Bind("Config", "Use Resolution Watermark", true, "When the StudioScene/Character watermark is enabled, the resolution watermark will be forced to use.");

            PathToTheFontResource = Config.Bind("WaterMark", "Path of the font picture", "", "Full path to the font resource picture, must be a PNG or JPG.");
            PathToTheFontResource.SettingChanged += delegate { SetFontPic(); };
            SetFontPic();
            CharacterSize = Config.Bind("WaterMark", "Font Size", 1.0f);
            PositionX = Config.Bind("WaterMark", "Position X", 97, new ConfigDescription("0 = left, 100 = right", new AcceptableValueRange<int>(0, 100)));
            PositionY = Config.Bind("WaterMark", "Position Y", 0, new ConfigDescription("0 = bottom, 100 = top", new AcceptableValueRange<int>(0, 100)));

            if (TimesOfMaker.Value == 0) TimesOfMaker.Value = (float)TimesOfMaker.DefaultValue;
            if (TimesOfStudio.Value == 0) TimesOfStudio.Value = (float)TimesOfStudio.DefaultValue;
            if (PNGColumnCount.Value == 0) PNGColumnCount.Value = (int)PNGColumnCount.DefaultValue;
            Harmony.CreateAndPatchAll(typeof(Patches));
        }

        private void SetFontPic()
        {
            fontTexture = null;
            if (PathToTheFontResource.Value.Length != 0 && File.Exists(PathToTheFontResource.Value))
            {
                fontTexture = ImageHelper.LoadTexture(PathToTheFontResource.Value);
                if (null != fontTexture)
                {
                    Logger.LogDebug($"Load font pic: {PathToTheFontResource.Value}");
                }
                else
                {
                    Logger.LogError("Load font picture FAILED: " + PathToTheFontResource.Value);
                }
            }

            if (null == fontTexture)
            {
                fontTexture = ImageHelper.LoadDllResourceToTexture2D($"PNGCaptureSizeModifier.Resources.ArialFont.png", 1024, 1024);
                Logger.LogDebug($"Load original font pic");
            }

            TextToTexture.fontTexture = fontTexture;
        }
    }

    class Patches
    {
        #region PNG存檔放大
        [HarmonyTranspiler, HarmonyPatch(typeof(CustomCapture), "CapCharaCard")]
        public static IEnumerable<CodeInstruction> CapCharaCardTranspiler(IEnumerable<CodeInstruction> instructions)
            => PngTranspiler(instructions, PNGCaptureSizeModifier.TimesOfMaker.Value);

        [HarmonyTranspiler, HarmonyPatch(typeof(CustomCapture), "CapCoordinateCard")]
        public static IEnumerable<CodeInstruction> CapCoordinateCardTranspiler(IEnumerable<CodeInstruction> instructions)
            => PngTranspiler(instructions, PNGCaptureSizeModifier.TimesOfMaker.Value);

        [HarmonyTranspiler, HarmonyPatch(typeof(Studio.SceneInfo), "Save", new Type[] { typeof(string) })]
        public static IEnumerable<CodeInstruction> SaveTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return PngTranspiler(instructions, PNGCaptureSizeModifier.TimesOfStudio.Value);
        }

        private static IEnumerable<CodeInstruction> PngTranspiler(IEnumerable<CodeInstruction> instructions, float times)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
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
        #endregion

        #region CharaMaker存檔顯示放大
        [HarmonyPostfix, HarmonyPatch(typeof(CustomBase), "Start")]
        public static void StartPostFix() => ChangeRowCount();

        [HarmonyPostfix, HarmonyPatch(typeof(CustomFileListCtrl), "UpdateSort")]
        public static void UpdateSortPostFix() => ChangeRowCount();

        public static void ChangeRowCount() { 
            //Block HScene
            if (null == Singleton<CustomBase>.Instance) return;

            if (PNGCaptureSizeModifier.PNGColumnCount.Value == 0)
            {
                PNGCaptureSizeModifier.PNGColumnCount.Value = (int)PNGCaptureSizeModifier.PNGColumnCount.DefaultValue;
            }

            _ = Singleton<CustomBase>.Instance.StartCoroutine(
                UpdateWidth(GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/charaFileControl/charaFileWindow/WinRect/ListArea/Scroll View/Viewport/Content").GetComponentInChildren<GridLayoutGroup>()));
            _ = Singleton<CustomBase>.Instance.StartCoroutine(
                UpdateWidth(GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow/WinRect/ListArea/Scroll View/Viewport/Content").GetComponentInChildren<GridLayoutGroup>()));

            IEnumerator UpdateWidth(GridLayoutGroup glg)
            {
                //切換到System頁的下一幀再計算，否則width會不對
                Toggle tgl = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMainMenu/BaseTop/tglSystem").GetComponentInChildren<Toggle>();
                yield return new WaitUntil(() => tgl.isOn);
                yield return new WaitForFixedUpdate();

                int count = PNGCaptureSizeModifier.PNGColumnCount.Value; //重抓，以免設定值在此期間有改變
                glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                glg.constraintCount = count;

                RectTransform rect = glg.transform as RectTransform;
                float width = (rect.rect.width - (9 * (count - 1)) - 20) / count;
                glg.cellSize = new Vector2(width, width / glg.cellSize.x * glg.cellSize.y);
                yield return new WaitForEndOfFrame();
                LayoutRebuilder.ForceRebuildLayoutImmediate(glg.transform as RectTransform);
            }
        }
        #endregion

        //Studio預覽放大
        [HarmonyPostfix, HarmonyPatch(typeof(Studio.SceneLoadScene), "Awake")]
        public static void AwakePostFix() =>
            GameObject.Find("SceneLoadScene/Canvas Load Work/root").transform.localScale = new Vector3(2, 2, 1);

        #region 加浮水印
        private static bool SDFlag = false;
        private static bool CMFlag = false;

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.SceneInfo), "Save", new Type[] { typeof(string) })]
        public static void SavePrefix() => SDFlag = true;

        [HarmonyPriority(Priority.Last), HarmonyPostfix, HarmonyPatch(typeof(Studio.GameScreenShot), "CreatePngScreen")]
        public static void CreatePngScreenPostfix(ref byte[] __result)
        {
            if (SDFlag)
            {
                AddWatermark(PNGCaptureSizeModifier.StudioSceneWatermark.Value, ref __result, "sd_watermark.png", PNGCaptureSizeModifier.TimesOfStudio);
                SDFlag = false;
            }
        }

        [HarmonyPriority(Priority.Last), HarmonyPrefix, HarmonyPatch(typeof(CustomCapture), "CapCharaCard")]
        public static void CapCharaCardPrefix() => CMFlag = true;

        [HarmonyPriority(Priority.Last), HarmonyPostfix, HarmonyPatch(typeof(CustomCapture), "CreatePng")]
        public static void CreatePngPostfix(ref byte[] pngData)
        {
            if (CMFlag)
            {
                AddWatermark(PNGCaptureSizeModifier.CharaMakerWatermark.Value, ref pngData, "chara_watermark.png", PNGCaptureSizeModifier.TimesOfMaker);
                CMFlag = false;
            }
        }

        private static void AddWatermark(bool doWatermarkFlag, ref byte[] basePng, string wmFileName, ConfigEntry<float> times)
        {
            if (!doWatermarkFlag && !PNGCaptureSizeModifier.ResolutionWaterMark.Value) return;

            Texture2D screenshot = new Texture2D(2, 2);
            screenshot.LoadImage(basePng);

            //圖片分辨率
            if (PNGCaptureSizeModifier.ResolutionWaterMark.Value || doWatermarkFlag)
            {
                string text = $"{screenshot.width}x{screenshot.height}";
                int textureWidth = TextToTexture.CalcTextWidthPlusTrailingBuffer(text, PNGCaptureSizeModifier.CharacterSize.Value);
                Texture2D capsize = TextToTexture.CreateTextToTexture(text, 0, 0, textureWidth, PNGCaptureSizeModifier.CharacterSize.Value);
                capsize = capsize.Scale((int)(capsize.width * times.Value / (float)times.DefaultValue), (int)(capsize.height * times.Value / (float)times.DefaultValue));
                screenshot = screenshot.OverwriteTexture(
                    capsize,
                    screenshot.width * PNGCaptureSizeModifier.PositionX.Value / 100 - capsize.width,
                    screenshot.height * PNGCaptureSizeModifier.PositionY.Value / 100
                );
                PNGCaptureSizeModifier.Logger.LogDebug($"Add Resolution: {text}");
            }

            //浮水印
            if (doWatermarkFlag)
            {
                Texture2D watermark = ImageHelper.LoadDllResourceToTexture2D($"PNGCaptureSizeModifier.Resources.{wmFileName}");
                watermark = watermark.Scale((int)(watermark.width * times.Value / (float)times.DefaultValue), (int)(watermark.height * times.Value / (float)times.DefaultValue));
                screenshot = screenshot.OverwriteTexture(
                    watermark,
                    0,
                    screenshot.height - watermark.height
                );
                PNGCaptureSizeModifier.Logger.LogDebug($"Add Watermark: {wmFileName}");
            }

            basePng = screenshot.EncodeToPNG();
        }
        #endregion
    }
}
