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
using UnityEngine;

namespace KK_SaveLoadCompression {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_SaveLoadCompression : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Save Load Compression";
        internal const string GUID = "com.jim60105.kk.saveloadcompression";
        internal const string PLUGIN_VERSION = "20.06.06.0";
        internal const string PLUGIN_RELEASE_VERSION = "0.0.0";
        public static ConfigEntry<DictionarySize> DictionarySize { get; private set; }
        public static ConfigEntry<bool> Enable { get; private set; }
        public static ConfigEntry<bool> Notice { get; private set; }
        public static ConfigEntry<bool> DeleteTheOri { get; private set; }

        internal static new ManualLogSource Logger;
        internal static string Path;
        public void Awake() {
            Logger = base.Logger;
            DictionarySize = Config.Bind<DictionarySize>("Settings", "Compress Dictionary Size", SevenZip.DictionarySize.VeryLarge, "If compression FAILs, try changing it to a smaller size.");
            Enable = Config.Bind<bool>("Config", "Enable", false, "!!!NOTICE!!!");
            Notice = Config.Bind<bool>("Config", "I do realize that without this plugin, the archive will not be readable.", false, "!!!NOTICE!!!");
            HarmonyWrapper.PatchAll(typeof(Patches));
        }
        public void Start() => Path = BepInEx.Paths.CachePath;
    }

    class Patches {
        private static ManualLogSource Logger = KK_SaveLoadCompression.Logger;
        [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), "Save", new Type[] { typeof(string) })]
        public static void SavePostfix(string _path) {
            if (!KK_SaveLoadCompression.Enable.Value || !KK_SaveLoadCompression.Notice.Value) return;

            using (FileStream fileStreamReader = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                using (BinaryReader binaryReader = new BinaryReader(fileStreamReader)) {
                    Logger.LogWarning("Start Compress");
                    string FileName = Path.GetFileNameWithoutExtension(_path);
                    string TempPath = Path.GetTempFileName();
                    bool success = true;

                    using (FileStream fileStreamWriter = new FileStream(TempPath, FileMode.Create, FileAccess.Write)) {
                        using (BinaryWriter binaryWriter = new BinaryWriter(fileStreamWriter)) {
                            byte[] pngData = PngFile.LoadPngBytes(binaryReader);
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

                            binaryWriter.Write(png.EncodeToPNG());
                            binaryWriter.Write(new Version(2, 0, 0, 0).ToString());

                            using (MemoryStream msCompressed = new MemoryStream()) {
                                long fileStreamPos = fileStreamReader.Position;
                                LZMA.Compress(fileStreamReader, msCompressed, LzmaSpeed.Fastest, KK_SaveLoadCompression.DictionarySize.Value,
                                    delegate(long inSize,long _) { 
                                        Logger.LogInfo($"Compressed Progress: {inSize}/{fileStreamReader.Length - fileStreamPos}"); 
                                    }
                                );

                                Logger.LogDebug("Start compression test...");
                                using (MemoryStream msDecompressed = new MemoryStream()) {
                                    msCompressed.Seek(0, SeekOrigin.Begin);
                                    LZMA.Decompress(msCompressed, msDecompressed);

                                    fileStreamReader.Seek(fileStreamPos, SeekOrigin.Begin);
                                    msDecompressed.Seek(0, SeekOrigin.Begin);

                                    for (int i = 0; i < msDecompressed.Length; i++) {
                                        int aByte = fileStreamReader.ReadByte();
                                        int bByte = msDecompressed.ReadByte();
                                        if (aByte.CompareTo(bByte) != 0) {
                                            success = false;
                                            break;
                                        }
                                    }
                                    if (success) {
                                        binaryWriter.Write(msCompressed.ToArray());
                                        Logger.LogDebug($"Compression test SUCCESS");
                                    } else {
                                        Logger.LogError($"Compression test FAILED");
                                    }
                                }
                            }
                        }
                    }
                    if (success) {
                        File.Copy(TempPath, _path.Substring(0, _path.Length - 4) + "_compressed.png");
                    }
                    File.Delete(TempPath);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(SceneLoadScene), "OnClickLoad")]
        public static void OnClickLoadPrefix(ref List<string> ___listPath, int ___select) {
            string _path = ___listPath[___select];
            using (FileStream fileStreamReader = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                using (BinaryReader binaryReader = new BinaryReader(fileStreamReader)) {
                    byte[] pngData = PngFile.LoadPngBytes(binaryReader);
                    Version dataVersion = new Version(binaryReader.ReadString());
                    if (!dataVersion.Equals(new Version(2, 0, 0, 0))) return;

                    Logger.LogWarning("Start Decompress...");
                    _path = _path.Substring(0, _path.Length - 4) + "_decompressed.png";
                    using (FileStream fileStreamWriter = new FileStream(_path, FileMode.Create, FileAccess.Write)) {
                        using (BinaryWriter binaryWriter = new BinaryWriter(fileStreamWriter)) {
                            binaryWriter.Write(pngData);

                            LZMA.Decompress(fileStreamReader, fileStreamWriter/*, Action<long, long> onProgress*/);
                            ___listPath[___select] = _path;
                        }
                    }

                    Logger.LogDebug($"Decompression FINISH");
                }
            }
        }
    }
}
