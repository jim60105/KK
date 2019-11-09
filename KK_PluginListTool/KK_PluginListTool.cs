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
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KK_PluginListTool {
	[BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
	public class KK_PluginListTool : BaseUnityPlugin {
		internal const string PLUGIN_NAME = "Plugin List Tool";
		internal const string GUID = "com.jim60105.kk.pluginlisttool";
		internal const string PLUGIN_VERSION = "19.11.10.0";

		internal static new ManualLogSource Logger;
		bool init = false;
		public void LateUpdate() {
			if (!init) {
				init = true;
				Logger = base.Logger;
				Logger.LogDebug($"Start listing loaded plugin infos...");
				FileLog.logPath = Path.Combine(Path.GetDirectoryName(base.Info.Location), Path.GetFileNameWithoutExtension(Paths.ExecutablePath)) + "_LoadedPluginList.json";

				List<string> strList = new List<string>();
				foreach (var kv in BepInEx.Bootstrap.Chainloader.PluginInfos) {
					try {
						if (kv.Value != null) {
							Logger.LogDebug($"{kv.Value.Metadata.Name} v{kv.Value.Metadata.Version}");
//							strList.Add(Json.JsonParser.Serialize(kv.Value.Metadata));

							List<string> strItem = new List<string>();
							strItem.Add("\"guid\": \"" + $"{kv.Value.Metadata.GUID}" + "\"");
							strItem.Add("\"name\": \"" + $"{kv.Value.Metadata.Name}" + "\"");
							strItem.Add("\"version\": \"" + $"{kv.Value.Metadata.Version}" + "\"");
							strList.Add("{" + $"{strItem.Join(delimiter: ", ")}" + "}");
						}
					} catch (Exception e) {
						Logger.LogError($"Logging Plugin Info ERROR: {kv.Key} : {e.Message}");
					};
				}

				if (File.Exists(FileLog.logPath)) {
					File.Delete(FileLog.logPath);
				}
				FileLog.Log(JsonHelper.FormatJson($"[{strList.Join(delimiter: ",")}]"));

				Logger.LogInfo($"Logged Plugin Info into: {FileLog.logPath}");
			}
		}
	}

	#region ToolStuff
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
