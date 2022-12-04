using Extension;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;

namespace CoordinateLoadOption.OtherPlugin
{
    internal static class FolderBrowser
    {
        public const string GUID = "marco.FolderBrowser";

        internal static void PatchFolderBrowser(Harmony harmony)
        {
            string path = KoikatuHelper.TryGetPluginInstance(GUID, new Version(2, 6))?.Info.Location;

            if (string.IsNullOrEmpty(path))
                return;

            path = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_Hooks" + Path.GetExtension(path));

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            var type = Assembly.LoadFrom(path).GetType("BrowserFolders.Hooks.KKS.MakerOutfitFolders");

            if (null == type)
                return;

            harmony.Patch(type.GetMethod("SaveFilePatch", AccessTools.all),
                prefix: new HarmonyMethod(typeof(FolderBrowser), nameof(ExtensionIsNotTMP)));
        }

        private static bool ExtensionIsNotTMP(ref string path)
            => Path.GetExtension(path) != ".tmp";
    }
}