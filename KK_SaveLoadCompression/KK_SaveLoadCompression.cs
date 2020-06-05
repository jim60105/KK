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
using BepInEx.Harmony;
using BepInEx.Logging;
using HarmonyLib;
using SevenZip;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;

namespace KK_SaveLoadCompression {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_SaveLoadCompression : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Save Load Compression";
        internal const string GUID = "com.jim60105.kk.saveloadcompression";
        internal const string PLUGIN_VERSION = "20.06.05.0";
        internal const string PLUGIN_RELEASE_VERSION = "0.0.0";

        internal static new ManualLogSource Logger;
        public void Awake() {
            Logger = base.Logger;
            HarmonyWrapper.PatchAll(typeof(Patches));
        }
    }

    class Patches {
        private static ManualLogSource Logger = KK_SaveLoadCompression.Logger;
        [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), "Save", new Type[] { typeof(string) })]
        public static void SavePostfix(string _path) {
            using (FileStream fileStreamReader = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                using (BinaryReader binaryReader = new BinaryReader(fileStreamReader)) {
                    Logger.LogWarning("Start Compress");
                    using (FileStream fileStreamWriter = new FileStream(_path.Substring(0, _path.Length - 4) + "_compressed.png", FileMode.Create, FileAccess.Write)) {
                        using (BinaryWriter binaryWriter = new BinaryWriter(fileStreamWriter)) {
                            binaryWriter.Write(PngFile.LoadPngBytes(binaryReader));
                            binaryWriter.Write(new Version(2, 0, 0, 0).ToString());

                            LZMA.Compress(fileStreamReader, fileStreamWriter, LzmaSpeed.Fastest, DictionarySize.Larger/*, Action<long, long> onProgress*/);

                            //// LZ4MessagePack
                            //binaryWriter.Write(MessagePack.LZ4MessagePackSerializer.Serialize(allData));

                            //// C# LZF - Fast but poor compress ratio
                            //byte[] compressed = CLZF2.Compress(allData);
                            //binaryWriter.Write(compressed);
                        }
                    }
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

                    Logger.LogWarning("Start Decompress");
                    _path = _path.Substring(0, _path.Length - 4) + "_decompressed.png";
                    using (FileStream fileStreamWriter = new FileStream(_path, FileMode.Create, FileAccess.Write)) {
                        using (BinaryWriter binaryWriter = new BinaryWriter(fileStreamWriter)) {
                            binaryWriter.Write(pngData);

                            LZMA.Decompress(fileStreamReader, fileStreamWriter/*, Action<long, long> onProgress*/);
                            ___listPath[___select] = _path;
                        }
                    }
                }
            }
        }
    }
}
