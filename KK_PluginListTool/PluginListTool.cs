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
using Extension;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PluginListTool
{
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class PluginListTool : BaseUnityPlugin
    {
        internal const string PLUGIN_NAME = "Plugin List Tool";
        internal const string GUID = "com.jim60105.pluginlisttool";
        internal const string PLUGIN_VERSION = "21.08.28.0";
        internal const string PLUGIN_RELEASE_VERSION = "1.1.0";
        internal static new ManualLogSource Logger;
        public static ConfigEntry<bool> Enable { get; private set; }
        public static ConfigEntry<string> SavePath { get; private set; }
        public void Awake()
        {
            Enable = Config.Bind<bool>("Config", "Enable", true, "Re-enable to output again immediately");
            SavePath = Config.Bind<string>("Config", "Output Directory(Relative)", GetRelativePath(BepInEx.Paths.BepInExRootPath, Path.Combine(Path.GetDirectoryName(base.Info.Location), "KK_PluginListTool")), "Where do you want to store them?");


            Logger = base.Logger;
            Extension.Logger.logger = Logger;
            _isInited = !Enable.Value;
            Enable.SettingChanged += delegate
            {
                _isInited = !Enable.Value;
            };

            fullSavePath = Path.Combine(BepInEx.Paths.BepInExRootPath, SavePath.Value.Replace("\n", "\\n"));
            Logger.LogDebug($"FullSavePath: {fullSavePath}");
        }

        internal static List<string> strList = new List<string>();
        internal static List<Plugin> pluginList = new List<Plugin>();
        internal static bool _isInited = false;
        internal static string fullSavePath;
        public void LateUpdate()
        {
            //只觸發一次
            if (!_isInited)
            {
                _isInited = true;
                Logger.LogDebug($"--Start listing loaded plugin infos--");

                #region GetPlugins
                //IPA
                Logger.LogDebug($">>Try load IPA plugin infos");
                string IPAAssPath = KoikatuHelper.TryGetPluginInstance("BepInEx.IPALoader", new Version(1, 2))?.Info.Location;
                //Logger.LogDebug($"Path: {IPAAssPath}");
                if (null != IPAAssPath && File.Exists(IPAAssPath))
                {
                    Type IPlugin = Assembly.LoadFrom(IPAAssPath).GetType("IllusionPlugin.IPlugin");
                    Type PluginManager = Assembly.LoadFrom(IPAAssPath).GetType("IllusionInjector.PluginManager");

                    //呼叫 KK_PluginListTool.GetIPA<IPlugin>(Plugins);
                    MethodInfo method = typeof(PluginListTool).GetMethod(nameof(GetIPA), BindingFlags.Public | BindingFlags.Static);
                    method = method.MakeGenericMethod(IPlugin);
                    method.Invoke(null, new object[] { PluginManager.GetProperties()[0].GetValue(null, null) });
                }
                else
                {
                    Logger.LogDebug($">>No IPALoader found.");
                }

                //BepPlugin
                Logger.LogDebug($">>Get {BepInEx.Bootstrap.Chainloader.PluginInfos.Count} BepInEx Plugins.");
                foreach (KeyValuePair<string, PluginInfo> kv in BepInEx.Bootstrap.Chainloader.PluginInfos)
                {
                    pluginList.Add(new Plugin(
                        kv.Value.Metadata.GUID,
                        kv.Value.Metadata.Name,
                        kv.Value.Metadata.Version.ToString(),
                        kv.Value.Location.Replace(Paths.GameRootPath + "\\", "")
                    ));
                }
                #endregion

                #region WriteFile
                _ = Directory.CreateDirectory(fullSavePath);

                try
                {
                    File.WriteAllText(Path.Combine(fullSavePath, Path.GetFileNameWithoutExtension(Paths.ExecutablePath)) + "_LoadedPluginList.json",
                        JsonHelper.FormatJson($"[{pluginList.Select(x => MakeJsonString(x.GUID, x.Name, x.Version, x.Location)).Join(delimiter: ",")}]"));
                }
                catch (Exception e)
                {
                    Logger.LogError($">>Logged JSON FAILED");
                    Logger.LogError(e.Message);
                    Logger.LogError(e.StackTrace);
                }

                try
                {
                    File.WriteAllText(Path.Combine(fullSavePath, Path.GetFileNameWithoutExtension(Paths.ExecutablePath)) + "_LoadedPluginList.csv",
                        $"GUID, Name, Version, Location\n{pluginList.Select(x => MakeCsvString(x.GUID, x.Name, x.Version, x.Location)).Join(delimiter: "\n")}");
                }
                catch (Exception e)
                {
                    Logger.LogError($">>Logged CSV FAILED");
                    Logger.LogError(e.Message);
                    Logger.LogError(e.StackTrace);
                }
                #endregion
                Logger.LogDebug($"--List Finish--");
            }
        }

        public static void GetIPA<T>(object obj)
        {
            if (obj is IEnumerable<T> iEnumerable)
            {
                List<T> newList = new List<T>(iEnumerable);
                Logger.LogDebug($">>Get {newList.Count} IPA Plugins.");
                ConfigFile cf = new ConfigFile(Utility.CombinePaths(Paths.ConfigPath, "BepInEx.IPALoader.cfg"), false);
                string IPAPath = cf.Bind("Config", "Plugins Path", "Plugins", "Folder from which to load IPA plugins relative to the game root directory").Value;

                if (newList.Count > 0)
                {
                    foreach (T l in newList)
                    {
                        pluginList.Add(new Plugin(
                            "IPA." + ((string)l.GetProperty("Name")).Replace("_", "").Replace(" ", "."),   //IPlugin結構內沒有GUID，姑且拼一個
                            (string)l.GetProperty("Name"),
                            (string)l.GetProperty("Version"),
                            IPAPath + "\\" + (string)l.GetProperty("Name") + ".dll"
                        ));
                    }
                }
            }
        }

        public static string MakeJsonString(string guid, string name, string version, string location)
        {
            //Log to File
            List<string> strItem = new List<string> {
                "\"guid\": \"" + $"{guid}" + "\"",
                "\"name\": \"" + $"{name}" + "\"",
                "\"version\": \"" + $"{version}" + "\"",
                "\"location\": \"" + $"{location}" + "\"",
            };
            return "{" + $" {strItem.Join(delimiter: ", ")}" + "}";
        }

        public static string MakeCsvString(string guid, string name, string version, string location)
        {
            return $"{guid}, {name}, {version}, {location}";
        }

        static string GetRelativePath(string basePath, string targetPath)
        {
            // Require trailing backslash for path
            if (!basePath.EndsWith("\\"))
                basePath += "\\";

            return targetPath.Replace(basePath, "");
        }
    }

    public class Plugin
    {
        public string GUID { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Location { get; set; }

        public Plugin(string guid, string name, string version, string location)
        {
            GUID = guid;
            Name = name;
            Version = version;
            Location = location;
            PluginListTool.Logger.LogDebug($"{name} v{version}");
        }
    }

    #region JSONTool
    //JSON formatter in C#  - Stack Overflow
    //https://stackoverflow.com/a/6237866
    static class JsonHelper
    {
        private const string INDENT_STRING = "    ";
        public static string FormatJson(string str)
        {
            int indent = 0;
            bool quoted = false;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                char ch = str[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        int index = i;
                        while (index > 0 && str[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            quoted = !quoted;
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!quoted)
                            sb.Append(" ");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
        {
            foreach (T i in ie)
            {
                action(i);
            }
        }
    }
    #endregion
}