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
using System.IO;
using System.Threading;
using UnityEngine;

namespace KK_SaveLoadCompression {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class KK_SaveLoadCompression : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Save Load Compression";
        internal const string GUID = "com.jim60105.kk.saveloadcompression";
        internal const string PLUGIN_VERSION = "20.06.10.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.3.2";
        public static ConfigEntry<DictionarySize> DictionarySize { get; private set; }
        public static ConfigEntry<bool> Enable { get; private set; }
        public static ConfigEntry<bool> Notice { get; private set; }
        public static ConfigEntry<bool> DeleteTheOri { get; private set; }
        public static ConfigEntry<bool> DisplayMessage { get; private set; }
        public static ConfigEntry<bool> SkipSaveCheck { get; private set; }

        internal static new ManualLogSource Logger;
        internal static DirectoryInfo CacheDirectory;
        public void Awake() {
            Logger = base.Logger;
            CleanCacheFolder();

            Enable = Config.Bind<bool>("Config", "Enable", false, "!!!NOTICE!!!");
            Notice = Config.Bind<bool>("Config", "I do realize that without this plugin, the save files will not be readable!!", false, "!!!NOTICE!!!");
            DeleteTheOri = Config.Bind<bool>("Settings", "Delete the original file", false, "The original saved file will be automatically overwritten.");
            DisplayMessage = Config.Bind<bool>("Settings", "Display compression message on screen", true);
            SkipSaveCheck = Config.Bind<bool>("Settings", "Skip bytes compare when saving", false, "!!!Use this at your own risk!!!!");
            DictionarySize = Config.Bind<DictionarySize>("Settings", "Compress Dictionary Size", SevenZip.DictionarySize.VeryLarge, "If compression FAILs, try changing it to a smaller size.");
            Harmony harmonyInstance = HarmonyWrapper.PatchAll(typeof(Patches));
            harmonyInstance.Patch(
                typeof(SceneInfo).GetMethod(nameof(SceneInfo.Load), new[] { typeof(string), typeof(Version).MakeByRefType() }),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Patches.LoadPrefix))
            );
        }

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

        void OnApplicationQuit() => CleanCacheFolder();
        void OnDestroy() => CleanCacheFolder();

        private static void CleanCacheFolder() {
            CacheDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), GUID));
            foreach (FileInfo file in CacheDirectory.GetFiles()) file.Delete();
            foreach (DirectoryInfo subDirectory in CacheDirectory.GetDirectories()) subDirectory.Delete(true);
            Logger.LogDebug("Clean cache folder");
        }
    }

    class Patches {
        private static ManualLogSource Logger = KK_SaveLoadCompression.Logger;

        //https://github.com/IllusionMods/DragAndDrop/blob/v1.2/src/DragAndDrop.Koikatu/DragAndDrop.cs#L12
        private const string StudioToken = "【KStudio】";
        private const string CharaToken = "【KoiKatuChara";
        private const string SexToken = "sex";
        private const string CoordinateToken = "【KoiKatuClothes】";
        //private const string PoseToken = "【pose】";

        #region Save
        //Studio Save
        [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), "Save", new Type[] { typeof(string) })]
        public static void SavePostfix(string _path)
            => Save(_path, StudioToken);

        //Chara Save
        [HarmonyPostfix, HarmonyPatch(typeof(ChaFileControl), "SaveCharaFile", new Type[] { typeof(string), typeof(byte), typeof(bool) })]
        public static void SaveCharaFilePostfix(ChaFileControl __instance, string filename, byte sex)
            => Save(__instance.ConvertCharaFilePath(filename, sex), CharaToken + SexToken + sex);

        //Coordinate Save
        [HarmonyPostfix, HarmonyPatch(typeof(ChaFileCoordinate), "SaveFile", new Type[] { typeof(string) })]
        public static void SaveFilePostfix(string path)
            => Save(path, CoordinateToken);

        private static void Save(string path, string token) {
            if (!KK_SaveLoadCompression.Enable.Value || !KK_SaveLoadCompression.Notice.Value) return;
            byte[] pngData;
            string TempPath = Path.Combine(KK_SaveLoadCompression.CacheDirectory.CreateSubdirectory("Compressed").FullName, Path.GetFileName(path));

            //這裡用cleanedPath作"_compressed"字串清理
            string cleanedPath = path;
            while (cleanedPath.Contains("_compressed")) {
                cleanedPath = cleanedPath.Replace("_compressed", "");
            }

            if (cleanedPath != path) {
                File.Copy(path, cleanedPath, true);
                Logger.LogDebug($"Clean Path: {cleanedPath}");
            }

            //Update Cache
            string decompressPath = Path.Combine(KK_SaveLoadCompression.CacheDirectory.CreateSubdirectory("Decompressed").FullName, Path.GetFileName(cleanedPath));
            File.Copy(path, decompressPath, true);

            using (FileStream fileStreamReader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                using (BinaryReader binaryReader = new BinaryReader(fileStreamReader)) {
                    Logger.LogInfo("Start Compress");
                    pngData = PngFile.LoadPngBytes(binaryReader);
                    Texture2D png = new Texture2D(2, 2);
                    png.LoadImage(pngData);

                    Texture2D watermark = Extension.Extension.LoadDllResource($"KK_SaveLoadCompression.Resources.zip_watermark.png");
                    float scaleTimes = (token == StudioToken) ? .14375f : .30423f;
                    watermark = Extension.Extension.Scale(watermark, Convert.ToInt32(png.width * scaleTimes), Convert.ToInt32(png.width * scaleTimes));
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
                bool successFlag = true;
                try {
                    using (FileStream fileStreamReader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                        float startTime = Time.time;
                        using (FileStream fileStreamWriter = new FileStream(TempPath, FileMode.Create, FileAccess.Write)) {
                            using (BinaryWriter binaryWriter = new BinaryWriter(fileStreamWriter)) {
                                binaryWriter.Write(pngData);

                                switch (token) {
                                    case StudioToken:
                                        //Studio
                                        binaryWriter.Write(new Version(101, 0, 0, 0).ToString());
                                        break;
                                    case CoordinateToken:
                                        //Coordinate
                                        binaryWriter.Write(101);
                                        break;
                                    default:
                                        //Chara
                                        if (token.IndexOf(CharaToken) >= 0) {
                                            binaryWriter.Write(101);
                                            break;
                                        }

                                        throw new Exception("Token not match.");
                                }

                                //為了通過 InvalidSceneFileProtection 和 DragAndDrop
                                binaryWriter.Write(token);

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
                                                    successFlag = false;
                                                    break;
                                                }
                                            }
                                            KK_SaveLoadCompression.Progress = "";
                                        }
                                        if (successFlag) {
                                            long newSize = msCompressed.Length + token.Length + pngData.Length;
                                            binaryWriter.Write(msCompressed.ToArray());
                                            LogLevel logLevel = KK_SaveLoadCompression.DisplayMessage.Value ? (LogLevel.Message | LogLevel.Info) : LogLevel.Info;
                                            Logger.LogInfo($"Compression test SUCCESS");
                                            Logger.Log(logLevel, $"Compression finish in {Math.Round(Time.time - startTime, 2)} seconds");
                                            Logger.Log(logLevel, $"Size compress from {fileStreamReader.Length} bytes to {newSize} bytes");
                                            Logger.Log(logLevel, $"Compress ratio: {Math.Round(Convert.ToDouble(fileStreamReader.Length) / newSize, 2)}, which means it is now {Math.Round(100 / (Convert.ToDouble(fileStreamReader.Length) / newSize), 2)}% big.");
                                        } else {
                                            Logger.LogError($"Compression test FAILED");
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //複製或刪除檔案
                    if (successFlag) {
                        string compressedPath = cleanedPath;
                        if (!KK_SaveLoadCompression.DeleteTheOri.Value) {
                            compressedPath = cleanedPath.Substring(0, cleanedPath.Length - 4) + "_compressed.png";
                        }

                        File.Copy(TempPath, compressedPath, true);
                        Logger.LogDebug($"Write to: {compressedPath}");

                        //因為File.Delete()不是立即執行完畢，不能有「砍掉以後立即在同位置寫入」的操作，所以是這個邏輯順序
                        //如果相同的話，上方就已經覆寫了；不同的話此處再做刪除
                        if (path != compressedPath && path != cleanedPath) {
                            File.Delete(path);
                            Logger.LogDebug($"Delete Original File: {path}");
                        }
                    }
                } catch (Exception e) {
                    if (e is IOException && successFlag) {
                        //覆寫時遇到讀取重整會IOException: Sharing violation on path，這在Compress太快時會發生
                        //Retry
                        try {
                            if (File.Exists(TempPath)) {
                                if (KK_SaveLoadCompression.DeleteTheOri.Value) {
                                    File.Copy(TempPath, path, true);
                                }
                            }
                        } catch (Exception) {
                            //Copy to a new name if failed twice
                            File.Copy(TempPath, path.Substring(0, path.Length - 4) + "_compressed2.png");
                            Logger.LogError("Overwrite was FAILED twice. Fallback to use the '_compressed2' path.");
                        }
                    } else {
                        Logger.LogError($"An unknown error occurred. If your files are lost, please find them at %TEMP%/{KK_SaveLoadCompression.GUID}");
                        throw;
                    }
                } finally {
                    if (File.Exists(TempPath)) File.Delete(TempPath);
                }
            }
        }
        #endregion

        #region Load
        //Studio Load
        [HarmonyPriority(Priority.First)]
        public static void LoadPrefix(ref string _path)
            => Load(ref _path, StudioToken);

        //Studio Import
        [HarmonyPrefix, HarmonyPriority(Priority.First), HarmonyPatch(typeof(SceneInfo), "Import", new Type[] { typeof(string) })]
        public static void ImportPrefix(ref string _path)
            => Load(ref _path, StudioToken);

        //Chara Load
        [HarmonyPrefix, HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaFile), "LoadFile", new Type[] { typeof(string), typeof(bool), typeof(bool) })]
        public static void LoadFilePrefix(ref string path)
            => Load(ref path, CharaToken);

        [HarmonyPrefix, HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaFileControl), "LoadCharaFile", new Type[] { typeof(string), typeof(byte), typeof(bool), typeof(bool) })]
        public static void LoadCharaFilePostfix(ChaFileControl __instance, ref string filename, byte sex) {
            filename = __instance.ConvertCharaFilePath(filename, sex);
            Load(ref filename, CharaToken);
        }

        //Coordinate Load
        [HarmonyPrefix, HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaFileCoordinate), "LoadFile", new Type[] { typeof(string) })]
        public static void LoadFilePrefix(ChaFileCoordinate __instance, ref string path)
            => Load(ref path, CoordinateToken);

        private static void Load(ref string path, string token) {
            string n = Path.GetFileName(path);
            DirectoryInfo d = KK_SaveLoadCompression.CacheDirectory.CreateSubdirectory("Decompressed");
            string tmpPath = Path.Combine(d.FullName, n);
            if (File.Exists(tmpPath)) {
                path = tmpPath;
                Logger.LogDebug("Load from cache: " + path);
                return;
            }

            using (FileStream fileStreamReader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                using (BinaryReader binaryReader = new BinaryReader(fileStreamReader)) {
                    byte[] pngData;
                    try {
                        bool checkfail = false;
                        pngData = PngFile.LoadPngBytes(binaryReader);

                        switch (token) {
                            case StudioToken:
                                checkfail = !new Version(binaryReader.ReadString()).Equals(new Version(101, 0, 0, 0));
                                break;
                            case CoordinateToken:
                            case CharaToken:
                                checkfail = 101 != binaryReader.ReadInt32();
                                break;
                        }

                        if (checkfail) {
                            return;
                        }
                    } catch (Exception) {
                        //在這裡發生讀取錯誤，那大概不是個正確的存檔
                        //因為已經有其它檢核的plugin存在，直接拋給他處理
                        Logger.Log(LogLevel.Error | LogLevel.Message,"Corrupted file: " + path);
                        return; 
                    }
                    try { 
                        //Discard token string
                        binaryReader.ReadString();

                        Logger.LogDebug("Start Decompress...");
                        //KK_Fix_CharacterListOptimizations依賴檔名做比對
                        using (FileStream fileStreamWriter = new FileStream(tmpPath, FileMode.Create, FileAccess.Write)) {
                            using (BinaryWriter binaryWriter = new BinaryWriter(fileStreamWriter)) {
                                binaryWriter.Write(pngData);

                                long fileStreamPos = fileStreamReader.Position;
                                LZMA.Decompress(fileStreamReader, fileStreamWriter,
                                    delegate (long inSize, long _) {
                                        KK_SaveLoadCompression.Progress = $"Decompressing: {Convert.ToInt32(inSize * 100 / (fileStreamReader.Length - fileStreamPos))}%";
                                    }
                                );
                                KK_SaveLoadCompression.Progress = "";
                            }
                        }

                        path = tmpPath;
                        Logger.LogDebug($"Decompression FINISH");
                    } catch (Exception) {
                        Logger.LogError($"Decompression FAILDED. The file was damaged during compression.");
                        Logger.LogError($"Do not disable the byte comparison setting next time to avoid this.");
                        return;
                    }
                }
            }
        }
        #endregion
    }
}
