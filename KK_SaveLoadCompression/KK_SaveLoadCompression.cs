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

using System;
using System.IO;
using System.Linq;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Extension;
using HarmonyLib;
using SevenZip;
using Studio;
using UnityEngine;

namespace KK_SaveLoadCompression {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class KK_SaveLoadCompression : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Save Load Compression";
        internal const string GUID = "com.jim60105.kk.saveloadcompression";
        internal const string PLUGIN_VERSION = "20.09.07.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.3.5";
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
            Extension.Logger.logger = Logger;

            CleanCacheFolder();

            Enable = Config.Bind<bool>("Config", "Enable", false, "!!!NOTICE!!!");
            Notice = Config.Bind<bool>("Config", "I do realize that without this plugin, the save files will not be readable!!", false, "!!!NOTICE!!!");
            DeleteTheOri = Config.Bind<bool>("Settings", "Delete the original file", false, "The original saved file will be automatically overwritten.");
            DisplayMessage = Config.Bind<bool>("Settings", "Display compression message on screen", true);
            SkipSaveCheck = Config.Bind<bool>("Settings", "Skip bytes compare when saving", false, "!!!Use this at your own risk!!!!");
            Harmony harmonyInstance = Harmony.CreateAndPatchAll(typeof(Patches));
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

        private void CleanCacheFolder() {
            CacheDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), GUID));
            foreach (FileInfo file in CacheDirectory.GetFiles()) file.Delete();
            foreach (DirectoryInfo subDirectory in CacheDirectory.GetDirectories()) subDirectory.Delete(true);
            Logger.LogDebug("Clean cache folder");
        }
    }

    class Patches {
        private static ManualLogSource Logger = KK_SaveLoadCompression.Logger;

        #region Save
        //Studio Save
        [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), "Save", new Type[] { typeof(string) })]
        public static void SavePostfix(string _path)
            => Save(_path, SaveLoadCompression.Token.StudioToken);

        //Chara Save
        [HarmonyPostfix, HarmonyPatch(typeof(ChaFileControl), "SaveCharaFile", new Type[] { typeof(string), typeof(byte), typeof(bool) })]
        public static void SaveCharaFilePostfix(ChaFileControl __instance, string filename, byte sex)
            => Save(__instance.ConvertCharaFilePath(filename, sex), SaveLoadCompression.Token.CharaToken + "】" + SaveLoadCompression.Token.SexToken + sex);  //】:為了通過CharacterReplacer

        //Coordinate Save
        [HarmonyPostfix, HarmonyPatch(typeof(ChaFileCoordinate), "SaveFile", new Type[] { typeof(string) })]
        public static void SaveFilePostfix(string path)
            => Save(path, SaveLoadCompression.Token.CoordinateToken);

        public static void Save(string path, string token) {
            //這裡用cleanedPath作"_compressed"字串清理
            string cleanedPath = path;
            while (cleanedPath.Contains("_compressed")) {
                cleanedPath = cleanedPath.Replace("_compressed", "");
            }

            string compressedPath = cleanedPath;
            if (!KK_SaveLoadCompression.DeleteTheOri.Value) {
                compressedPath = cleanedPath.Substring(0, cleanedPath.Length - 4) + "_compressed.png";
            }

            //Update Cache
            string decompressCacheDirName = KK_SaveLoadCompression.CacheDirectory.CreateSubdirectory("Decompressed").FullName;
            if (!KK_SaveLoadCompression.Enable.Value || !KK_SaveLoadCompression.Notice.Value) {
                //Clear cache and out
                File.Delete(Path.Combine(decompressCacheDirName, Path.GetFileName(path)));
                File.Delete(Path.Combine(decompressCacheDirName, Path.GetFileName(cleanedPath)));
                File.Delete(Path.Combine(decompressCacheDirName, Path.GetFileName(compressedPath)));
                return;
            }
            File.Copy(path, Path.Combine(decompressCacheDirName, Path.GetFileName(compressedPath)), true);

            if (cleanedPath != path) {
                File.Copy(path, cleanedPath, true);
                Logger.LogDebug($"Clean Path: {cleanedPath}");
            }

            byte[] pngData = MakeWatermarkPic(ImageHelper.LoadPngBytes(path), token, true);
            byte[] unzipPngData = MakeWatermarkPic(ImageHelper.LoadPngBytes(path), token, false);

            //New Thread, No Freeze 
            Thread newThread = new Thread(saveThread);
            newThread.Start();

            void saveThread() {
                Logger.LogInfo("Start Compress");
                long newSize = 0;
                long originalSize = 0;
                float startTime = Time.time;
                string TempPath = Path.Combine(KK_SaveLoadCompression.CacheDirectory.CreateSubdirectory("Compressed").FullName, Path.GetFileName(path));
                KK_SaveLoadCompression.Progress = "";
                try {
                    originalSize = new FileInfo(path).Length;

                    newSize = new SaveLoadCompression().Save(
                        path,
                        TempPath,
                        token: token,
                        pngData: pngData,
                        compressProgress: (decimal progress) => KK_SaveLoadCompression.Progress = $"Compressing: {progress:p2}",
                        doComapre: !KK_SaveLoadCompression.SkipSaveCheck.Value,
                        compareProgress: (decimal progress) => KK_SaveLoadCompression.Progress = $"Comparing: {progress:p2}");

                    //複製或刪除檔案
                    if (newSize > 0) {
                        LogLevel logLevel = KK_SaveLoadCompression.DisplayMessage.Value ? (LogLevel.Message | LogLevel.Info) : LogLevel.Info;
                        Logger.LogInfo($"Compression test SUCCESS");
                        Logger.Log(logLevel, $"Compression finish in {Time.time - startTime:n2} seconds");
                        Logger.Log(logLevel, $"Size compress from {originalSize} bytes to {newSize} bytes");
                        Logger.Log(logLevel, $"Compress ratio: {Convert.ToDecimal(originalSize) / newSize:n3}/1, which means it is now {Convert.ToDecimal(newSize) / originalSize:p3} big.");

                        //寫入壓縮結果
                        File.Copy(TempPath, compressedPath, true);
                        Logger.LogDebug($"Write to: {compressedPath}");

                        //如果壓縮路徑未覆寫，將原始圖檔加上unzip浮水印
                        if(cleanedPath != compressedPath) {
                            ChangePNG(cleanedPath, unzipPngData);
                            Logger.LogDebug($"Overwrite unzip watermark: {cleanedPath}");
                        }

                        //如果原始路徑和上二存檔都不相同，刪除之
                        //因為File.Delete()不是立即執行完畢，不能有「砍掉以後立即在同位置寫入」的操作，所以是這個邏輯順序
                        //如果相同的話，上方就已經覆寫了；不同的話此處再做刪除
                        if (path != compressedPath && path != cleanedPath) {
                            File.Delete(path);
                            Logger.LogDebug($"Delete Original File: {path}");
                        }
                    } else {
                        Logger.LogError($"Compression FAILED");
                    }
                } catch (Exception e) {
                    if (e is IOException && newSize > 0) {
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
                        Logger.Log(LogLevel.Error | LogLevel.Message, $"An unknown error occurred. If your files are lost, please find them at %TEMP%/{KK_SaveLoadCompression.GUID}");
                        throw;
                    }
                } finally {
                    KK_SaveLoadCompression.Progress = "";
                    if (File.Exists(TempPath)) File.Delete(TempPath);
                }
            }
        }
        #endregion

        #region Load
        //Studio Load
        [HarmonyPriority(Priority.First)]
        public static void LoadPrefix(ref string _path)
            => Load(ref _path, SaveLoadCompression.Token.StudioToken);

        //Studio Import
        [HarmonyPrefix, HarmonyPriority(Priority.First), HarmonyPatch(typeof(SceneInfo), "Import", new Type[] { typeof(string) })]
        public static void ImportPrefix(ref string _path)
            => Load(ref _path, SaveLoadCompression.Token.StudioToken);

        //Chara Load
        [HarmonyPrefix, HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaFile), "LoadFile", new Type[] { typeof(string), typeof(bool), typeof(bool) })]
        public static void LoadFilePrefix(ref string path)
            => Load(ref path, SaveLoadCompression.Token.CharaToken);

        [HarmonyPrefix, HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaFileControl), "LoadCharaFile", new Type[] { typeof(string), typeof(byte), typeof(bool), typeof(bool) })]
        public static void LoadCharaFilePrefix(ChaFileControl __instance, ref string filename, byte sex) {
            filename = __instance.ConvertCharaFilePath(filename, sex);
            Load(ref filename, SaveLoadCompression.Token.CharaToken);
        }

        //Coordinate Load
        [HarmonyPrefix, HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaFileCoordinate), "LoadFile", new Type[] { typeof(string) })]
        public static void LoadFilePrefix(ChaFileCoordinate __instance, ref string path)
            => Load(ref path, SaveLoadCompression.Token.CoordinateToken);

        public static void Load(ref string path, string token) {
            string fileName = Path.GetFileName(path);
            string tmpPath = Path.Combine(KK_SaveLoadCompression.CacheDirectory.CreateSubdirectory("Decompressed").FullName, fileName);
            if (File.Exists(tmpPath)) {
                path = tmpPath;
                Logger.LogDebug("Load from cache: " + path);
                return;
            }
            float startTime = Time.time;

            KK_SaveLoadCompression.Progress = "";
            //KK_Fix_CharacterListOptimizations依賴檔名做比對
            //這裡必須寫為實體檔案供它使用
            try {
                if (0 != new SaveLoadCompression().Load(path,
                                                        tmpPath,
                                                        token,
                                                        (decimal progress) => KK_SaveLoadCompression.Progress = $"Decompressing: {progress:p2}")) {
                    path = tmpPath; //change path result by ref
                    if (Time.time - startTime == 0) {
                        Logger.LogDebug($"Decompressed: {fileName}");
                    } else {
                        Logger.LogDebug($"Decompressed: {fileName}, finish in {Time.time - startTime} seconds");
                    }
                } else {
                    // 非壓縮存檔，退出且不報錯
                    File.Delete(tmpPath);
                }
            } catch (Exception) {
                //在這裡發生讀取錯誤，那大概不是個正確的存檔
                //因為已經有其它檢核的plugin存在，直接返回
                Logger.Log(LogLevel.Error | LogLevel.Message, $"Decompressed failed: {fileName}");
                File.Delete(tmpPath);
                return;
            } finally {
                KK_SaveLoadCompression.Progress = "";
            }
        }
        #endregion

        /// <summary>
        /// 以Unity方式壓上浮水印
        /// </summary>
        /// <param name="pngData"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal static byte[] MakeWatermarkPic(byte[] pngData, string token, bool zip) {
            Texture2D png = new Texture2D(1, 1);
            png.LoadImage(pngData);

            Texture2D watermark;
            if (zip) {
                watermark = ImageHelper.LoadDllResourceToTexture2D($"KK_SaveLoadCompression.Resources.zip_watermark.png");
            } else {
                watermark = ImageHelper.LoadDllResourceToTexture2D($"KK_SaveLoadCompression.Resources.unzip_watermark.png");
            }
            float scaleTimes = new SaveLoadCompression().GetScaleTimes(token);
            watermark = watermark.Scale(Convert.ToInt32(png.width * scaleTimes), Convert.ToInt32(png.width * scaleTimes));
            png = png.OverwriteTexture(
                watermark,
                0,
                png.height - watermark.height
            );
            Extension.Logger.LogDebug($"Add Watermark: zip");
            return png.EncodeToPNG();
        }

        private static void ChangePNG(string path, byte[] pngData) {
            byte[] data;
            using (FileStream fileStreamReader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                ImageHelper.SkipPng(fileStreamReader);
                data = ImageHelper.ReadToEnd(fileStreamReader);
            }
            string tmpPath = Path.GetTempFileName();
            using (FileStream fileStreamWriter = new FileStream(tmpPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter binaryWriter = new BinaryWriter(fileStreamWriter)) {
                binaryWriter.Write(pngData);
                binaryWriter.Write(data);
            }
            File.Copy(tmpPath, path, true);
        }
    }

    public class SaveLoadCompression {
        public struct Token {
            //https://github.com/IllusionMods/DragAndDrop/blob/v1.2/src/DragAndDrop.Koikatu/DragAndDrop.cs#L12
            public const string StudioToken = "【KStudio】";
            public const string CharaToken = "【KoiKatuChara";
            public const string SexToken = "sex";
            public const string CoordinateToken = "【KoiKatuClothes】";
            //private const string PoseToken = "【pose】";
        }

        /// <summary>
        /// 取得浮水印的縮放倍率
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public float GetScaleTimes(string token) => (token == Token.StudioToken) ? .14375f : .30423f;

        public long Save(string inputPath, string outputPath, string token = null, byte[] pngData = null, Action<decimal> compressProgress = null, bool doComapre = true, Action<decimal> compareProgress = null) {
            using (FileStream fileStreamReader = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (FileStream fileStreamWriter = new FileStream(outputPath, FileMode.Create, FileAccess.Write)) {
                return Save(fileStreamReader,
                            fileStreamWriter,
                            token: token,
                            pngData: pngData,
                            compressProgress: compressProgress,
                            doComapre: doComapre,
                            compareProgress: compareProgress);
            }
        }

        public long Save(Stream inputStream,
                         Stream outputStream,
                         string token = null,
                         byte[] pngData = null,
                         Action<decimal> compressProgress = null,
                         bool doComapre = true,
                         Action<decimal> compareProgress = null) {
            long dataSize = 0;

            Action<long, long> _compressProgress = null;
            if (null != compressProgress) {
                _compressProgress = (long inSize, long _) => compressProgress(Convert.ToDecimal(inSize) / dataSize);
            }

            //Make png watermarked
            using (BinaryReader binaryReader = new BinaryReader(inputStream))
            using (BinaryWriter binaryWriter = new BinaryWriter(outputStream)) {
                if (null == pngData) {
                    pngData = ImageHelper.LoadPngBytes(binaryReader);
                } else {
                    ImageHelper.SkipPng(binaryReader);
                    Extension.Logger.LogDebug("Skip Png:" + inputStream.Position);
                }

                dataSize = inputStream.Length - inputStream.Position;

                binaryWriter.Write(pngData);

                if (null == token) {
                    token = GuessToken(binaryReader);
                }

                switch (token) {
                    case Token.StudioToken:
                        //Studio
                        binaryWriter.Write(new Version(101, 0, 0, 0).ToString());
                        break;
                    case Token.CoordinateToken:
                        //Coordinate
                        binaryWriter.Write(101);
                        break;
                    default:
                        //Chara
                        if (token.IndexOf(Token.CharaToken) >= 0) {
                            binaryWriter.Write(101);
                            break;
                        }

                        throw new Exception("Token not match.");
                }

                //為了通過 InvalidSceneFileProtection 和 DragAndDrop
                binaryWriter.Write(token);

                using (MemoryStream msCompressed = new MemoryStream()) {
                    //PngFile.SkipPng(inputStream);
                    long fileStreamPos = inputStream.Position;

                    LZMA.Compress(
                        inputStream,
                        msCompressed,
                        LzmaSpeed.Fastest,
                        DictionarySize.VeryLarge,
                        _compressProgress
                    );

                    Extension.Logger.LogDebug("Start compression test...");
                    if (doComapre) {
                        using (MemoryStream msDecompressed = new MemoryStream()) {
                            msCompressed.Seek(0, SeekOrigin.Begin);

                            LZMA.Decompress(msCompressed, msDecompressed);
                            inputStream.Seek(fileStreamPos, SeekOrigin.Begin);
                            msDecompressed.Seek(0, SeekOrigin.Begin);
                            int bufferSize = 1 << 10;
                            byte[] aByteA = new byte[(int)bufferSize];
                            byte[] bByteA = new byte[(int)bufferSize];

                            if ((inputStream.Length - inputStream.Position) != msDecompressed.Length) {
                                return 0;
                            }

                            for (long i = 0; i < msDecompressed.Length;) {
                                if (null != compressProgress) {
                                    compareProgress(Convert.ToDecimal(i) / msDecompressed.Length);
                                }

                                inputStream.Read(aByteA, 0, (int)bufferSize);
                                i += msDecompressed.Read(bByteA, 0, (int)bufferSize);
                                if (!aByteA.SequenceEqual(bByteA)) {
                                    return 0;
                                }
                            }
                        }
                    }
                    binaryWriter.Write(msCompressed.ToArray());
                    return binaryWriter.BaseStream.Length;
                }
            }
        }

        public long Load(string inputPath, string outputPath, string token = null, Action<decimal> decompressProgress = null) {
            using (FileStream fileStreamReader = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (FileStream fileStreamWriter = new FileStream(outputPath, FileMode.Create, FileAccess.Write)) {
                return Load(
                    fileStreamReader,
                    fileStreamWriter,
                    token: token,
                    decompressProgress: (decimal progress) => KK_SaveLoadCompression.Progress = $"Decompressing: {progress:p2}");
            }
        }

        public long Load(Stream inputStream,
                         Stream outputStream,
                         string token = null,
                         byte[] pngData = null,
                         Action<decimal> decompressProgress = null) {
            long dataSize = 0;
            Action<long, long> _decompressProgress = null;
            if (null != decompressProgress) {
                _decompressProgress = (long inSize, long _) => decompressProgress(Convert.ToDecimal(inSize) / dataSize);
            }

            using (BinaryReader binaryReader = new BinaryReader(inputStream))
            using (BinaryWriter binaryWriter = new BinaryWriter(outputStream)) {
                if (null == pngData) {
                    pngData = ImageHelper.LoadPngBytes(binaryReader);
                } else {
                    ImageHelper.SkipPng(binaryReader);
                    Extension.Logger.LogDebug("Skip Png:" + inputStream.Position);
                }

                if (!GuessCompressed(binaryReader)) {
                    //Extension.Logger.LogDebug("Not a compressed file.");
                    return 0;
                }

                try {
                    if (null == token) {
                        token = GuessToken(binaryReader);
                        if (null == token) {
                            throw new FileLoadException();
                        }
                    }
                    bool checkfail = false;

                    switch (token) {
                        case Token.StudioToken:
                            checkfail = !new Version(binaryReader.ReadString()).Equals(new Version(101, 0, 0, 0));
                            break;
                        case Token.CoordinateToken:
                        case Token.CharaToken:
                            checkfail = 101 != binaryReader.ReadInt32();
                            break;
                    }

                    if (checkfail) {
                        throw new FileLoadException();
                    }
                } catch (FileLoadException e) {
                    Extension.Logger.LogError("Corrupted file");
                    throw e;
                }
                try {
                    //Discard token string
                    binaryReader.ReadString();

                    Extension.Logger.LogDebug("Start Decompress...");
                    binaryWriter.Write(pngData);

                    dataSize = inputStream.Length - inputStream.Position;
                    LZMA.Decompress(inputStream, outputStream, _decompressProgress);
                } catch (Exception e) {
                    Extension.Logger.LogError($"Decompression FAILDED. The file was corrupted during compression or storage.");
                    Extension.Logger.LogError($"Do not disable the byte comparison setting next time to avoid this.");
                    throw e;
                }
                return binaryWriter.BaseStream.Length;
            }
        }

        /// <summary>
        /// 偵測token。BinaryReader之Position必須處在pngData之後。
        /// </summary>
        /// <param name="binaryReader"></param>
        /// <returns></returns>
        public string GuessToken(BinaryReader binaryReader) {
            long position = binaryReader.BaseStream.Position;
            try {
                int r = binaryReader.ReadInt32();
                if (r != 101 && r != 100) {
                    return Token.StudioToken;
                }
                string token = binaryReader.ReadString();
                if (token.IndexOf(Token.CharaToken) >= 0) {
                    // 這裡不知道角色性別，直接給1(女性)
                    // 跨性別讀取基本上夠完善，我想可以略過判別
                    return Token.CharaToken + "】" + Token.SexToken + 1;
                } else if (token == Token.CoordinateToken) {
                    return Token.CoordinateToken;
                }
            } finally {
                binaryReader.BaseStream.Seek(position, SeekOrigin.Begin);
            }
            return null;
        }

        /// <summary>
        /// 偵測是否為已壓縮存檔。BinaryReader之Position必須處在pngData之後。
        /// </summary>
        /// <param name="binaryReader"></param>
        /// <returns></returns>
        public bool GuessCompressed(BinaryReader binaryReader) {
            long position = binaryReader.BaseStream.Position;
            try {
                int r = binaryReader.ReadInt32();
                switch (r) {
                    case 101:
                        return true;
                    case 100:
                        return false;
                    default:
                        // Studio
                        binaryReader.BaseStream.Seek(position, SeekOrigin.Begin);
                        string st = binaryReader.ReadString();
                        Version version = new Version(st);
                        return version.Major == 101;
                }
            } finally {
                binaryReader.BaseStream.Seek(position, SeekOrigin.Begin);
            }
        }
    }
}
