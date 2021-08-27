using System;
using BepInEx;

namespace Extension {
    class KoikatuHelper {
        public static BaseUnityPlugin TryGetPluginInstance(string pluginName, Version minimumVersion = null) {
            BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(pluginName, out PluginInfo target);
            if (null != target) {
                if (target.Metadata.Version >= minimumVersion) {
                    return target.Instance;
                }
                Logger.LogWarning($"{pluginName} v{target.Metadata.Version.ToString()} is detacted OUTDATED.");
                Logger.LogWarning($"Please update {pluginName} to at least v{minimumVersion.ToString()} to enable related feature.");
            }
            return null;
        }
    }
}
