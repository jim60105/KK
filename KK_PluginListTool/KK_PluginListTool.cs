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
using System.Xml.Linq;

namespace KK_PluginListTool {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class KK_PluginListTool : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Plugin List Tool";
        internal const string GUID = "com.jim60105.kk.pluginlisttool";
        internal const string PLUGIN_VERSION = "19.12.19.1";
        internal static new ManualLogSource Logger;
        public static ConfigEntry<bool> Enable { get; private set; }
        public void Awake() {
            Enable = Config.Bind<bool>("Config", "Trigger log action", false, "Click to trigger log action.");
        }

        bool init = false;
        internal static List<string> strList = new List<string>();
        internal static List<Plugin> pluginList = new List<Plugin>();
        public void LateUpdate() {
            if (!init && Enable.Value) {
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
                    AddJsonString(
                        kv.Value.Metadata.GUID,
                        kv.Value.Metadata.Name,
                        kv.Value.Metadata.Version.ToString()
                    );
                    pluginList.Add(new Plugin(
                        kv.Value.Metadata.GUID,
                        kv.Value.Metadata.Name,
                        kv.Value.Metadata.Version.ToString()
                    ));
                }

                FileLog.logPath = Path.Combine(Path.GetDirectoryName(base.Info.Location), Path.GetFileNameWithoutExtension(Paths.ExecutablePath)) + "_LoadedPluginList_JSON.json";
                if (File.Exists(FileLog.logPath)) {
                    File.Delete(FileLog.logPath);
                }
                FileLog.Log(JsonHelper.FormatJson($"[{strList.Join(delimiter: ",")}]"));

                Logger.LogMessage($"Logged JSON into: {FileLog.logPath}");

                string xmlPath = Path.Combine(Path.GetDirectoryName(base.Info.Location), Path.GetFileNameWithoutExtension(Paths.ExecutablePath)) + "_LoadedPluginList_ExcelXML.xml";
                XDocument xdoc = pluginList.Select(x => (object)x).ToExcelXml();
                xdoc.Save(xmlPath);
                Logger.LogMessage($"Logged Excel XML into: {xmlPath}");

                Enable.Value = false;
            }
        }

        public static void GetIPA<T>(object obj) {
            if (obj is IEnumerable<T> iEnumerable) {
                List<T> newList = new List<T>(iEnumerable);
                Logger.LogDebug($">>Get {newList.Count} IPA Plugins.");
                foreach (var l in newList) {
                    AddJsonString(
                        "IPA." + ((string)l.GetProperty("Name")).Replace(' ', '.'),   //IPlugin結構內沒有GUID，姑且拼一個
                        (string)l.GetProperty("Name"),
                        (string)l.GetProperty("Version")
                    );
                    pluginList.Add(new Plugin(
                        "IPA." + ((string)l.GetProperty("Name")).Replace(' ', '.'),   //IPlugin結構內沒有GUID，姑且拼一個
                        (string)l.GetProperty("Name"),
                        (string)l.GetProperty("Version")
                    ));
                }
            }
        }

        public static void AddJsonString(string guid, string name, string version) {
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

    public class Plugin {
        private string guid;
        private string name;
        private string version;

        public string GUID { get => guid; set => guid = value; }
        public string Name { get => name; set => name = value; }
        public string Version { get => version; set => version = value; }

        public Plugin(string guid, string name, string version) {
            GUID = guid;
            Name = name;
            Version = version;
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

    #region ExcelXmlTool
    public static class ExcelExportExtensions {
        public static XDocument ToExcelXml(this IEnumerable<object> rows) {
            return rows.ToExcelXml("Sheet1");
        }

        public static XDocument ToExcelXml(this IEnumerable<object> rows, string sheetName) {
            sheetName = sheetName.Replace("/", "-");
            sheetName = sheetName.Replace("\\", "-");

            XNamespace mainNamespace = "urn:schemas-microsoft-com:office:spreadsheet";
            XNamespace o = "urn:schemas-microsoft-com:office:office";
            XNamespace x = "urn:schemas-microsoft-com:office:excel";
            XNamespace ss = "urn:schemas-microsoft-com:office:spreadsheet";
            XNamespace html = "http://www.w3.org/TR/REC-html40";

            XDocument xdoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                new XProcessingInstruction("mso-application", "progid=\"Excel.Sheet\""));

            var headerRow = from p in rows.First().GetType().GetProperties()
                            select new XElement(mainNamespace + "Cell",
                                new XElement(mainNamespace + "Data",
                                    new XAttribute(ss + "Type", "String"), p.Name)); //Generate header using reflection

            XElement workbook = new XElement(mainNamespace + "Workbook",
                new XAttribute(XNamespace.Xmlns + "html", html),
                new XAttribute(XName.Get("ss", "http://www.w3.org/2000/xmlns/"), ss),
                new XAttribute(XName.Get("o", "http://www.w3.org/2000/xmlns/"), o),
                new XAttribute(XName.Get("x", "http://www.w3.org/2000/xmlns/"), x),
                new XAttribute(XName.Get("xmlns", ""), mainNamespace),
                new XElement(o + "DocumentProperties",
                        new XAttribute(XName.Get("xmlns", ""), o),
                        new XElement(o + "Author", KK_PluginListTool.GUID),
                        new XElement(o + "LastAuthor", KK_PluginListTool.GUID),
                        new XElement(o + "Created", DateTime.Now.ToString())
                    ), //end document properties
                new XElement(x + "ExcelWorkbook",
                        new XAttribute(XName.Get("xmlns", ""), x),
                        new XElement(x + "WindowHeight", 12750),
                        new XElement(x + "WindowWidth", 24855),
                        new XElement(x + "WindowTopX", 240),
                        new XElement(x + "WindowTopY", 75),
                        new XElement(x + "ProtectStructure", "False"),
                        new XElement(x + "ProtectWindows", "False")
                    ), //end ExcelWorkbook
                new XElement(mainNamespace + "Styles",
                        new XElement(mainNamespace + "Style",
                            new XAttribute(ss + "ID", "Default"),
                            new XAttribute(ss + "Name", "Normal"),
                            new XElement(mainNamespace + "Alignment",
                                new XAttribute(ss + "Vertical", "Bottom")
                            ),
                            new XElement(mainNamespace + "Borders"),
                            new XElement(mainNamespace + "Font",
                                new XAttribute(ss + "FontName", "Calibri"),
                                new XAttribute(x + "Family", "Swiss"),
                                new XAttribute(ss + "Size", "11"),
                                new XAttribute(ss + "Color", "#000000")
                            ),
                            new XElement(mainNamespace + "Interior"),
                            new XElement(mainNamespace + "NumberFormat"),
                            new XElement(mainNamespace + "Protection")
                        ),
                        new XElement(mainNamespace + "Style",
                            new XAttribute(ss + "ID", "Header"),
                            new XElement(mainNamespace + "Font",
                                new XAttribute(ss + "FontName", "Calibri"),
                                new XAttribute(x + "Family", "Swiss"),
                                new XAttribute(ss + "Size", "11"),
                                new XAttribute(ss + "Color", "#000000"),
                                new XAttribute(ss + "Bold", "1")
                            )
                        )
                    ), // close styles
                    new XElement(mainNamespace + "Worksheet",
                        new XAttribute(ss + "Name", sheetName /* Sheet name */),
                        new XElement(mainNamespace + "Table",
                            new XAttribute(ss + "ExpandedColumnCount", headerRow.Count()),
                            new XAttribute(ss + "ExpandedRowCount", rows.Count() + 1),
                            new XAttribute(x + "FullColumns", 1),
                            new XAttribute(x + "FullRows", 1),
                            new XAttribute(ss + "DefaultRowHeight", 15),
                            new XElement(mainNamespace + "Column",
                                new XAttribute(ss + "Width", 81)
                            ),
                            new XElement(mainNamespace + "Row", new XAttribute(ss + "StyleID", "Header"), headerRow),
                            from contentRow in rows
                            select new XElement(mainNamespace + "Row",
                                new XAttribute(ss + "StyleID", "Default"),
                                    from p in contentRow.GetType().GetProperties()
                                    select new XElement(mainNamespace + "Cell",
                                         new XElement(mainNamespace + "Data", new XAttribute(ss + "Type", "String"), p.GetValue(contentRow, null))) /* Build cells using reflection */ )
                        ), //close table
                        new XElement(x + "WorksheetOptions",
                            new XAttribute(XName.Get("xmlns", ""), x),
                            new XElement(x + "PageSetup",
                                new XElement(x + "Header",
                                    new XAttribute(x + "Margin", "0.3")
                                ),
                                new XElement(x + "Footer",
                                    new XAttribute(x + "Margin", "0.3")
                                ),
                                new XElement(x + "PageMargins",
                                    new XAttribute(x + "Bottom", "0.75"),
                                    new XAttribute(x + "Left", "0.7"),
                                    new XAttribute(x + "Right", "0.7"),
                                    new XAttribute(x + "Top", "0.75")
                                )
                            ),
                            new XElement(x + "Print",
                                new XElement(x + "ValidPrinterInfo"),
                                new XElement(x + "HorizontalResolution", 600),
                                new XElement(x + "VerticalResolution", 600)
                            ),
                            new XElement(x + "Selected"),
                            new XElement(x + "Panes",
                                new XElement(x + "Pane",
                                    new XElement(x + "Number", 3),
                                    new XElement(x + "ActiveRow", 1),
                                    new XElement(x + "ActiveCol", 0)
                                )
                            ),
                            new XElement(x + "ProtectObjects", "False"),
                            new XElement(x + "ProtectScenarios", "False")
                        ) // close worksheet options
                    ) // close Worksheet
                );

            xdoc.Add(workbook);

            return xdoc;
        }

        public static XElement ToExcelXmlWorksheet(this IEnumerable<object> rows, string sheetName) {
            sheetName = sheetName.Replace("/", "-");
            sheetName = sheetName.Replace("\\", "-");

            XNamespace mainNamespace = "urn:schemas-microsoft-com:office:spreadsheet";
            XNamespace o = "urn:schemas-microsoft-com:office:office";
            XNamespace x = "urn:schemas-microsoft-com:office:excel";
            XNamespace ss = "urn:schemas-microsoft-com:office:spreadsheet";
            XNamespace html = "http://www.w3.org/TR/REC-html40";

            var headerRow = from p in rows.First().GetType().GetProperties()
                            select new XElement(mainNamespace + "Cell",
                                new XElement(mainNamespace + "Data",
                                    new XAttribute(ss + "Type", "String"), p.Name)); //Generate header using reflection

            XElement worksheet = new XElement(mainNamespace + "Worksheet",
                    new XAttribute(ss + "Name", sheetName /* Sheet name */),
                    new XElement(mainNamespace + "Table",
                        new XAttribute(ss + "ExpandedColumnCount", headerRow.Count()),
                        new XAttribute(ss + "ExpandedRowCount", rows.Count() + 1),
                        new XAttribute(x + "FullColumns", 1),
                        new XAttribute(x + "FullRows", 1),
                        new XAttribute(ss + "DefaultRowHeight", 15),
                        new XElement(mainNamespace + "Column",
                            new XAttribute(ss + "Width", 81)
                        ),
                        new XElement(mainNamespace + "Row", new XAttribute(ss + "StyleID", "Header"), headerRow),
                        from contentRow in rows
                        select new XElement(mainNamespace + "Row",
                            new XAttribute(ss + "StyleID", "Default"),
                                from p in contentRow.GetType().GetProperties()
                                select new XElement(mainNamespace + "Cell",
                                        new XElement(mainNamespace + "Data", new XAttribute(ss + "Type", "String"), p.GetValue(contentRow, null))) /* Build cells using reflection */ )
                    ), //close table
                    new XElement(x + "WorksheetOptions",
                        new XAttribute(XName.Get("xmlns", ""), x),
                        new XElement(x + "PageSetup",
                            new XElement(x + "Header",
                                new XAttribute(x + "Margin", "0.3")
                            ),
                            new XElement(x + "Footer",
                                new XAttribute(x + "Margin", "0.3")
                            ),
                            new XElement(x + "PageMargins",
                                new XAttribute(x + "Bottom", "0.75"),
                                new XAttribute(x + "Left", "0.7"),
                                new XAttribute(x + "Right", "0.7"),
                                new XAttribute(x + "Top", "0.75")
                            )
                        ),
                        new XElement(x + "Print",
                            new XElement(x + "ValidPrinterInfo"),
                            new XElement(x + "HorizontalResolution", 600),
                            new XElement(x + "VerticalResolution", 600)
                        ),
                        new XElement(x + "Selected"),
                        new XElement(x + "Panes",
                            new XElement(x + "Pane",
                                new XElement(x + "Number", 3),
                                new XElement(x + "ActiveRow", 1),
                                new XElement(x + "ActiveCol", 0)
                            )
                        ),
                        new XElement(x + "ProtectObjects", "False"),
                        new XElement(x + "ProtectScenarios", "False")
                    ) // close worksheet options
                ); // close Worksheet

            return worksheet;
        }
    }
    #endregion
}