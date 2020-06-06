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
using SevenZip;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace KK_SaveLoadCompression {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_SaveLoadCompression : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Save Load Compression";
        internal const string GUID = "com.jim60105.kk.saveloadcompression";
        internal const string PLUGIN_VERSION = "20.06.06.1";
        internal const string PLUGIN_RELEASE_VERSION = "1.0.0";
        public static ConfigEntry<DictionarySize> DictionarySize { get; private set; }
        public static ConfigEntry<bool> Enable { get; private set; }
        public static ConfigEntry<bool> Notice { get; private set; }
        public static ConfigEntry<bool> DeleteTheOri { get; private set; }
        public static ConfigEntry<bool> DisplayMessage { get; private set; }
        public static ConfigEntry<bool> SkipSaveCheck { get; private set; }

        internal static new ManualLogSource Logger;
        internal static string Path;
        public void Awake() {
            Logger = base.Logger;
            Enable = Config.Bind<bool>("Config", "Enable", false, "!!!NOTICE!!!");
            Notice = Config.Bind<bool>("Config", "I do realize that without this plugin, the save files will not be readable!!", false, "!!!NOTICE!!!");
            DeleteTheOri = Config.Bind<bool>("Settings", "Delete the original file", false, "The original saved file will be automatically overwritten.");
            DisplayMessage = Config.Bind<bool>("Settings", "Display compression message on screen", true);
            SkipSaveCheck = Config.Bind<bool>("Settings", "Skip bytes check when saving", false, "Use it at your own risk!!!!");
            DictionarySize = Config.Bind<DictionarySize>("Settings", "Compress Dictionary Size", SevenZip.DictionarySize.VeryLarge, "If compression FAILs, try changing it to a smaller size.");
            HarmonyWrapper.PatchAll(typeof(Patches));
        }
        public void Start() => Path = BepInEx.Paths.CachePath;

        internal static string Progress = "";
        void OnGUI() {
            if (Progress.Length == 0) return;
            float margin = 20f;
            GUIStyle style = GUI.skin.GetStyle("box");
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 25;
            GUIContent content = new GUIContent(Progress, "Please wait until compression finish.");
            Vector2 v2 = style.CalcSize(content);
            GUI.Box(
                new Rect(
                    Screen.width - v2.x - margin,
                    Screen.height - v2.y - margin,
                    v2.x,
                    v2.y
                ),
                content,
                style
            );
        }
    }

    class Patches {
        private static ManualLogSource Logger = KK_SaveLoadCompression.Logger;
        [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), "Save", new Type[] { typeof(string) })]
        public static void SavePostfix(string _path) {
            if (!KK_SaveLoadCompression.Enable.Value || !KK_SaveLoadCompression.Notice.Value) return;
            byte[] pngData;
            string TempPath;
            float startTime = Time.time;

            using (FileStream fileStreamReader = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                using (BinaryReader binaryReader = new BinaryReader(fileStreamReader)) {
                    Logger.LogInfo("Start Compress");
                    TempPath = Path.GetTempFileName();
                    pngData = PngFile.LoadPngBytes(binaryReader);
                    Texture2D png = new Texture2D(2, 2);
                    png.LoadImage(pngData);

                    Texture2D watermark = Extension.Extension.LoadDllResource($"KK_SaveLoadCompression.Resources.zip_watermark.png");
                    watermark = Extension.Extension.Scale(watermark, Convert.ToInt32(png.width * .14375f), Convert.ToInt32(png.width * .14375f));
                    png = Extension.Extension.OverwriteTexture(
                        png,
                        watermark,
                        0,
                        png.height - watermark.height
                    );
                    //Logger.LogDebug($"Add Watermark: zip");
                    pngData = png.EncodeToPNG();
                }
            }

            Thread newThread = new Thread(doMain);
            newThread.Start();

            void doMain() {
                using (FileStream fileStreamReader = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    bool success = true;
                    using (FileStream fileStreamWriter = new FileStream(TempPath, FileMode.Create, FileAccess.Write)) {
                        using (BinaryWriter binaryWriter = new BinaryWriter(fileStreamWriter)) {

                            binaryWriter.Write(pngData);
                            binaryWriter.Write(new Version(100, 0, 0, 0).ToString());

                            using (MemoryStream msCompressed = new MemoryStream()) {
                                PngFile.SkipPng(fileStreamReader);

                                long fileStreamPos = fileStreamReader.Position;
                                LZMA.Compress(fileStreamReader, msCompressed, LzmaSpeed.Fastest, KK_SaveLoadCompression.DictionarySize.Value,
                                    delegate (long inSize, long _) {
                                        KK_SaveLoadCompression.Progress = $"Compressing: {Convert.ToInt32(inSize * 100 / (fileStreamReader.Length - fileStreamPos))}%";
                                    }
                                );
                                KK_SaveLoadCompression.Progress = "";

                                Logger.LogInfo("Start compression test...");
                                using (MemoryStream msDecompressed = new MemoryStream()) {
                                    if (!KK_SaveLoadCompression.SkipSaveCheck.Value) {
                                        msCompressed.Seek(0, SeekOrigin.Begin);
                                        LZMA.Decompress(msCompressed, msDecompressed,
                                            delegate (long inSize, long _) {
                                                KK_SaveLoadCompression.Progress = $"Decompressing: {Convert.ToInt32(inSize * 100 / (fileStreamReader.Length - fileStreamPos))}%";
                                            }
                                        );
                                        KK_SaveLoadCompression.Progress = "";

                                        fileStreamReader.Seek(fileStreamPos, SeekOrigin.Begin);
                                        msDecompressed.Seek(0, SeekOrigin.Begin);

                                        for (int i = 0; i < msDecompressed.Length; i++) {
                                            KK_SaveLoadCompression.Progress = $"Comparing: {i * 100 / msDecompressed.Length}%";
                                            int aByte = fileStreamReader.ReadByte();
                                            int bByte = msDecompressed.ReadByte();
                                            if (aByte.CompareTo(bByte) != 0) {
                                                success = false;
                                                break;
                                            }
                                        }
                                        KK_SaveLoadCompression.Progress = "";
                                    } 
                                    if (success) {
                                        binaryWriter.Write(msCompressed.ToArray());
                                        LogLevel logLevel = (KK_SaveLoadCompression.DisplayMessage.Value) ? (LogLevel.Message | LogLevel.Info) : LogLevel.Info;
                                        Logger.LogInfo($"Compression test SUCCESS");
                                        Logger.Log(logLevel, $"Compression finish in {Math.Round(Time.time - startTime, 2)} seconds");
                                        Logger.Log(logLevel, $"Size compress from {fileStreamReader.Length - fileStreamPos} bytes to {msCompressed.Length} bytes");
                                        Logger.Log(logLevel, $"Compress ratio: {Math.Round(Convert.ToDouble(fileStreamReader.Length - fileStreamPos) / msCompressed.Length, 2)}, which means it is now {Math.Round(100 / (Convert.ToDouble(fileStreamReader.Length - fileStreamPos) / msCompressed.Length), 2)}% big.");
                                    } else {
                                        Logger.LogError($"Compression test FAILED");
                                    }
                                }
                            }
                        }
                    }
                    if (success) {
                        if (KK_SaveLoadCompression.DeleteTheOri.Value) {
                            File.Copy(TempPath, _path, true);
                        } else {
                            File.Copy(TempPath, _path.Substring(0, _path.Length - 4) + "_compressed.png");
                        }
                    }
                    File.Delete(TempPath);
                }
            }
        }

        internal static string OriginalPath;
        internal static string TempPath;
        internal static List<string> listPath;
        internal static int select;
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(SceneLoadScene), "OnClickLoad")]
        public static void OnClickLoadPrefix(ref List<string> ___listPath, int ___select) {
            OriginalPath = ___listPath[___select];
            listPath = ___listPath;
            select = ___select;
            using (FileStream fileStreamReader = new FileStream(OriginalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                using (BinaryReader binaryReader = new BinaryReader(fileStreamReader)) {
                    byte[] pngData = PngFile.LoadPngBytes(binaryReader);
                    Version dataVersion = new Version(binaryReader.ReadString());
                    if (!dataVersion.Equals(new Version(100, 0, 0, 0))) {
                        OriginalPath = null;
                        return;
                    }

                    Logger.LogInfo("Start Decompress...");
                    TempPath = Path.GetTempFileName();
                    using (FileStream fileStreamWriter = new FileStream(TempPath, FileMode.Create, FileAccess.Write)) {
                        using (BinaryWriter binaryWriter = new BinaryWriter(fileStreamWriter)) {
                            binaryWriter.Write(pngData);

                            long fileStreamPos = fileStreamReader.Position;
                            LZMA.Decompress(fileStreamReader, fileStreamWriter,
                                delegate (long inSize, long _) {
                                    KK_SaveLoadCompression.Progress = $"Decompressing: {Convert.ToInt32(inSize * 100 / (fileStreamReader.Length - fileStreamPos))}%";
                                }
                            );
                            KK_SaveLoadCompression.Progress = "";
                            ___listPath[___select] = TempPath;
                        }
                    }

                    Logger.LogInfo($"Decompression FINISH");
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), "Load", new Type[] { typeof(string) })]
        public static void LoadPostfix() {
            if (null == OriginalPath || OriginalPath.Length == 0) return;
            listPath[select] = OriginalPath;
            File.Delete(TempPath);
        }
    }
}
