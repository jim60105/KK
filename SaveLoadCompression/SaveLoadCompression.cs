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
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Extension;
using HarmonyLib;
using SevenZip;
using Studio;
using UnityEngine;
using PngCompression;

namespace SaveLoadCompression
{
    [BepInProcess("CharaStudio")]
    [BepInProcess("KoikatsuSunshine")]
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class SaveLoadCompression : BaseUnityPlugin
    {
        internal const string PLUGIN_NAME = "Save Load Compression";
        internal const string GUID = "com.jim60105.kks.saveloadcompression";
        internal const string PLUGIN_VERSION = "21.08.28.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.5.0";
        public static ConfigEntry<DictionarySize> DictionarySize { get; private set; }
        public static ConfigEntry<bool> Enable { get; private set; }
        public static ConfigEntry<bool> Notice { get; private set; }
        public static ConfigEntry<bool> DeleteTheOri { get; private set; }
        public static ConfigEntry<bool> DisplayMessage { get; private set; }
        public static ConfigEntry<bool> SkipSaveCheck { get; private set; }

        internal static new ManualLogSource Logger;
        internal static DirectoryInfo CacheDirectory;
        public void Awake()
        {
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
        void OnGUI()
        {
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

        private void CleanCacheFolder()
        {
            CacheDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), GUID));
            foreach (FileInfo file in CacheDirectory.GetFiles()) file.Delete();
            foreach (DirectoryInfo subDirectory in CacheDirectory.GetDirectories()) subDirectory.Delete(true);
            Logger.LogDebug("Clean cache folder");
        }
    }

    class Patches
    {
        private static ManualLogSource Logger = SaveLoadCompression.Logger;

        #region Save
        //Studio Save
        [HarmonyPostfix, HarmonyPatch(typeof(SceneInfo), "Save", new Type[] { typeof(string) })]
        public static void SavePostfix(string _path)
            => Save(_path, Token.StudioToken);

        //Chara Save
        [HarmonyPostfix, HarmonyPatch(typeof(ChaFileControl), "SaveCharaFile", new Type[] { typeof(string), typeof(byte), typeof(bool) })]
        public static void SaveCharaFilePostfix(ChaFileControl __instance, string filename, byte sex)
            => Save(__instance.ConvertCharaFilePath(filename, sex), Token.CharaToken + "】" + Token.SexToken + sex);  //】:為了通過CharacterReplacer

        //Coordinate Save
        [HarmonyPostfix, HarmonyPatch(typeof(ChaFileCoordinate), "SaveFile", new Type[] { typeof(string) })]
        public static void SaveFilePostfix(string path)
            => Save(path, Token.CoordinateToken);

        public static void Save(string path, string token)
        {
            //這裡用cleanedPath作"_compressed"字串清理
            string cleanedPath = path;
            while (cleanedPath.Contains("_compressed"))
            {
                cleanedPath = cleanedPath.Replace("_compressed", "");
            }

            string compressedPath = cleanedPath;
            if (!SaveLoadCompression.DeleteTheOri.Value)
            {
                compressedPath = cleanedPath.Substring(0, cleanedPath.Length - 4) + "_compressed.png";
            }

            //Update Cache
            string decompressCacheDirName = SaveLoadCompression.CacheDirectory.CreateSubdirectory("Decompressed").FullName;
            if (!SaveLoadCompression.Enable.Value || !SaveLoadCompression.Notice.Value)
            {
                //Clear cache and out
                File.Delete(Path.Combine(decompressCacheDirName, Path.GetFileName(path)));
                File.Delete(Path.Combine(decompressCacheDirName, Path.GetFileName(cleanedPath)));
                File.Delete(Path.Combine(decompressCacheDirName, Path.GetFileName(compressedPath)));
                return;
            }
            File.Copy(path, Path.Combine(decompressCacheDirName, Path.GetFileName(compressedPath)), true);

            if (cleanedPath != path)
            {
                File.Copy(path, cleanedPath, true);
                Logger.LogDebug($"Clean Path: {cleanedPath}");
            }

            byte[] pngData = MakeWatermarkPic(ImageHelper.LoadPngBytes(path), token, true);
            byte[] unzipPngData = MakeWatermarkPic(ImageHelper.LoadPngBytes(path), token, false);

            //New Thread, No Freeze 
            Thread newThread = new Thread(saveThread);
            newThread.Start();

            void saveThread()
            {
                Logger.LogInfo("Start Compress");
                long newSize = 0;
                long originalSize = 0;
                float startTime = Time.time;
                string TempPath = Path.Combine(SaveLoadCompression.CacheDirectory.CreateSubdirectory("Compressed").FullName, Path.GetFileName(path));
                SaveLoadCompression.Progress = "";
                try
                {
                    originalSize = new FileInfo(path).Length;

                    newSize = new PngCompression.PngCompression().Save(
                        path,
                        TempPath,
                        token: token,
                        pngData: pngData,
                        compressProgress: (decimal progress) => SaveLoadCompression.Progress = $"Compressing: {progress:p2}",
                        doComapre: !SaveLoadCompression.SkipSaveCheck.Value,
                        compareProgress: (decimal progress) => SaveLoadCompression.Progress = $"Comparing: {progress:p2}");

                    //複製或刪除檔案
                    if (newSize > 0)
                    {
                        LogLevel logLevel = SaveLoadCompression.DisplayMessage.Value ? (LogLevel.Message | LogLevel.Info) : LogLevel.Info;
                        Logger.LogInfo($"Compression test SUCCESS");
                        Logger.Log(logLevel, $"Compression finish in {Time.time - startTime:n2} seconds");
                        Logger.Log(logLevel, $"Size compress from {originalSize} bytes to {newSize} bytes");
                        Logger.Log(logLevel, $"Compress ratio: {Convert.ToDecimal(originalSize) / newSize:n3}/1, which means it is now {Convert.ToDecimal(newSize) / originalSize:p3} big.");

                        //寫入壓縮結果
                        File.Copy(TempPath, compressedPath, true);
                        Logger.LogDebug($"Write to: {compressedPath}");

                        //如果壓縮路徑未覆寫，將原始圖檔加上unzip浮水印
                        if (cleanedPath != compressedPath)
                        {
                            ChangePNG(cleanedPath, unzipPngData);
                            Logger.LogDebug($"Overwrite unzip watermark: {cleanedPath}");
                        }

                        //如果原始路徑和上二存檔都不相同，刪除之
                        //因為File.Delete()不是立即執行完畢，不能有「砍掉以後立即在同位置寫入」的操作，所以是這個邏輯順序
                        //如果相同的話，上方就已經覆寫了；不同的話此處再做刪除
                        if (path != compressedPath && path != cleanedPath)
                        {
                            File.Delete(path);
                            Logger.LogDebug($"Delete Original File: {path}");
                        }
                    }
                    else
                    {
                        Logger.LogError($"Compression FAILED");
                    }
                }
                catch (Exception e)
                {
                    if (e is IOException && newSize > 0)
                    {
                        //覆寫時遇到讀取重整會IOException: Sharing violation on path，這在Compress太快時會發生
                        //Retry
                        try
                        {
                            if (File.Exists(TempPath))
                            {
                                if (SaveLoadCompression.DeleteTheOri.Value)
                                {
                                    File.Copy(TempPath, path, true);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            //Copy to a new name if failed twice
                            File.Copy(TempPath, path.Substring(0, path.Length - 4) + "_compressed2.png");
                            Logger.LogError("Overwrite was FAILED twice. Fallback to use the '_compressed2' path.");
                        }
                    }
                    else
                    {
                        Logger.Log(LogLevel.Error | LogLevel.Message, $"An unknown error occurred. If your files are lost, please find them at %TEMP%/{SaveLoadCompression.GUID}");
                        throw;
                    }
                }
                finally
                {
                    SaveLoadCompression.Progress = "";
                    if (File.Exists(TempPath)) File.Delete(TempPath);
                }
            }
        }
        #endregion

        #region Load
        //CheckData
        [HarmonyPrefix, HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaFile), "CheckData", new Type[] { typeof(string) })]
        public static void CheckDataPrefix(ref string path)
            => Load(ref path, Token.CharaToken);

        //Studio Load
        [HarmonyPriority(Priority.First)]
        public static void LoadPrefix(ref string _path)
            => Load(ref _path, Token.StudioToken);

        //Studio Import
        [HarmonyPrefix, HarmonyPriority(Priority.First), HarmonyPatch(typeof(SceneInfo), "Import", new Type[] { typeof(string) })]
        public static void ImportPrefix(ref string _path)
            => Load(ref _path, Token.StudioToken);

        //Chara Load
        [HarmonyPrefix, HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaFile), "LoadFile", new Type[] { typeof(string), typeof(bool), typeof(bool) })]
        public static void LoadFilePrefix(ref string path)
            => Load(ref path, Token.CharaToken);

        [HarmonyPrefix, HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaFileControl), "LoadCharaFile", new Type[] { typeof(string), typeof(byte), typeof(bool), typeof(bool) })]
        public static void LoadCharaFilePrefix(ChaFileControl __instance, ref string filename, byte sex)
        {
            filename = __instance.ConvertCharaFilePath(filename, sex);
            Load(ref filename, Token.CharaToken);
        }

        //KoikatsuConvertToSunshine
        [HarmonyPrefix, HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaFileControl), "LoadCharaFileKoikatsu", new Type[] { typeof(string), typeof(byte), typeof(bool), typeof(bool) })]
        public static void LoadCharaFileKoikatsuPrefix(ref string filename)
            => Load(ref filename, Token.CharaToken);

        //Coordinate Load
        [HarmonyPrefix, HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaFileCoordinate), "LoadFile", new Type[] { typeof(string) })]
        public static void LoadFilePrefix(ChaFileCoordinate __instance, ref string path)
            => Load(ref path, Token.CoordinateToken);

        public static void Load(ref string path, string token)
        {
            string fileName = Path.GetFileName(path);
            string tmpPath = Path.Combine(SaveLoadCompression.CacheDirectory.CreateSubdirectory("Decompressed").FullName, fileName);
            if (File.Exists(tmpPath))
            {
                path = tmpPath;
                Logger.LogDebug("Load from cache: " + path);
                return;
            }
            float startTime = Time.time;

            SaveLoadCompression.Progress = "";
            //KK_Fix_CharacterListOptimizations依賴檔名做比對
            //這裡必須寫為實體檔案供它使用
            try
            {
                if (0 != new PngCompression.PngCompression().Load(path,
                                                        tmpPath,
                                                        token,
                                                        (decimal progress) => SaveLoadCompression.Progress = $"Decompressing: {progress:p2}"))
                {
                    path = tmpPath; //change path result by ref
                    if (Time.time - startTime == 0)
                    {
                        Logger.LogDebug($"Decompressed: {fileName}");
                    }
                    else
                    {
                        Logger.LogDebug($"Decompressed: {fileName}, finish in {Time.time - startTime} seconds");
                    }
                }
                else
                {
                    // 非壓縮存檔，退出且不報錯
                    File.Delete(tmpPath);
                }
            }
            catch (Exception)
            {
                //在這裡發生讀取錯誤，那大概不是個正確的存檔
                //因為已經有其它檢核的plugin存在，直接返回
                Logger.Log(LogLevel.Error | LogLevel.Message, $"Decompressed failed: {fileName}");
                File.Delete(tmpPath);
                return;
            }
            finally
            {
                SaveLoadCompression.Progress = "";
            }
        }
        #endregion

        /// <summary>
        /// 以Unity方式壓上浮水印
        /// </summary>
        /// <param name="pngData"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal static byte[] MakeWatermarkPic(byte[] pngData, string token, bool zip)
        {
            Texture2D png = new Texture2D(1, 1);
            _ = ImageConversion.LoadImage(png, pngData);

            Texture2D watermark;
            if (zip)
            {
                watermark = ImageHelper.LoadDllResourceToTexture2D($"SaveLoadCompression.Resources.zip_watermark.png");
            }
            else
            {
                watermark = ImageHelper.LoadDllResourceToTexture2D($"SaveLoadCompression.Resources.unzip_watermark.png");
            }
            float scaleTimes = new PngCompression.PngCompression().GetScaleTimes(token);
            watermark = watermark.Scale(Convert.ToInt32(png.width * scaleTimes), Convert.ToInt32(png.width * scaleTimes));
            png = png.OverwriteTexture(
                watermark,
                0,
                png.height - watermark.height
            );
            Extension.Logger.LogDebug($"Add Watermark: zip");
            return png.EncodeToPNG();
        }

        private static void ChangePNG(string path, byte[] pngData)
        {
            byte[] data;
            using (FileStream fileStreamReader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                ImageHelper.SkipPng(fileStreamReader);
                data = ImageHelper.ReadToEnd(fileStreamReader);
            }
            string tmpPath = Path.GetTempFileName();
            using (FileStream fileStreamWriter = new FileStream(tmpPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter binaryWriter = new BinaryWriter(fileStreamWriter))
            {
                binaryWriter.Write(pngData);
                binaryWriter.Write(data);
            }
            File.Copy(tmpPath, path, true);
        }
    }
}
