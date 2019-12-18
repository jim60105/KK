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
using BepInEx.Logging;
using Extension;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace KK_PluginListTool {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class KK_PluginListTool : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Plugin List Tool";
        internal const string GUID = "com.jim60105.kk.pluginlisttool";
        internal const string PLUGIN_VERSION = "19.12.18.0";
        internal static new ManualLogSource Logger;

        bool init = false;
        internal static List<string> strList = new List<string>();
        public void LateUpdate() {
            if (!init) {
                init = true;
                Logger = base.Logger;
                Logger.LogDebug($"Start listing loaded plugin infos...");

                //IPA
                Logger.LogDebug($"Try load IPA plugin infos...");
                string IPAAssPath = Extension.Extension.TryGetPluginInstance("BepInEx.IPALoader", new Version(1, 2))?.Info.Location;
                //Logger.LogDebug($"Path: {IPAAssPath}");
                if (null != IPAAssPath && File.Exists(IPAAssPath)) {
                    Type IPlugin = Assembly.LoadFrom(IPAAssPath).GetType("IllusionPlugin.IPlugin");
                    Type PluginManager = Assembly.LoadFrom(IPAAssPath).GetType("IllusionInjector.PluginManager");

                    //Call KK_PluginListTool.GetIPA<IPlugin>(Plugins);
                    MethodInfo method = typeof(KK_PluginListTool).GetMethod(nameof(GetIPA), BindingFlags.Public | BindingFlags.Static);
                    method = method.MakeGenericMethod(IPlugin);
                    method.Invoke(null, new object[] { PluginManager.GetProperties()[0].GetValue(null, null) });
                } else {
                    Logger.LogDebug($"No IPALoader found.");
                }

                //BepPlugin
                Logger.LogDebug($">>Get {BepInEx.Bootstrap.Chainloader.PluginInfos.Count} BepInEx Plugins.");
                foreach (var kv in BepInEx.Bootstrap.Chainloader.PluginInfos) {
                    AddPlugin(
                        kv.Value.Metadata.GUID,
                        kv.Value.Metadata.Name,
                        kv.Value.Metadata.Version.ToString()
                    );
                }

                FileLog.logPath = Path.Combine(Path.GetDirectoryName(base.Info.Location), Path.GetFileNameWithoutExtension(Paths.ExecutablePath)) + "_LoadedPluginList.json";
                if (File.Exists(FileLog.logPath)) {
                    File.Delete(FileLog.logPath);
                }
                FileLog.Log(JsonHelper.FormatJson($"[{strList.Join(delimiter: ",")}]"));

                Logger.LogInfo($"Logged Plugin Info into: {FileLog.logPath}");
            }
        }

        public static void GetIPA<T>(object obj) {
            if (obj is IEnumerable<T> iEnumerable) {
                List<T> newList = new List<T>(iEnumerable);
                Logger.LogDebug($">>Get {newList.Count} IPA Plugins.");
                foreach (var l in newList) {
                    AddPlugin(
                        "IPA." + ((string)l.GetProperty("Name")).Replace(' ', '.'),   //IPlugin結構內沒有GUID，姑且拼一個
                        (string)l.GetProperty("Name"),
                        (string)l.GetProperty("Version")
                    );
                }
            }
        }

        public static void AddPlugin(string guid, string name, string version) {
            try {
                //Log to Console
                Logger.LogDebug($"{name} v{version}");

                //Log to File
                List<string> strItem = new List<string> {
                    "\"guid\": \"" + $"{guid}" + "\"",
                    "\"name\": \"" + $"{name}" + "\"",
                    "\"version\": \"" + $"{version}" + "\""
                };
                strList.Add("{" + $" {strItem.Join(delimiter: ", ")}" + "}");
            } catch (Exception e) {
                Logger.LogError($"Logging Plugin Info ERROR: {name} : {e.Message}");
            };
        }
    }

    #region JSONTool 
    //JSON formatter in C#  - Stack Overflow
    //https://stackoverflow.com/a/6237866
    class JsonHelper {
        private const string INDENT_STRING = "    ";
        public static string FormatJson(string str) {
            var indent = 0;
            var quoted = false;
            var sb = new StringBuilder();
            for (var i = 0; i < str.Length; i++) {
                var ch = str[i];
                switch (ch) {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!quoted) {
                            sb.AppendLine();
                            Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted) {
                            sb.AppendLine();
                            Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && str[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            quoted = !quoted;
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!quoted) {
                            sb.AppendLine();
                            Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!quoted)
                            sb.Append(" ");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }
    }

    static class Extensions {
        public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action) {
            foreach (var i in ie) {
                action(i);
            }
        }
    }
    #endregion
}
