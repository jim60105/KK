using System;
using BepInEx;

namespace Extension {
    class KoikatuHelper {
        public static BaseUnityPlugin TryGetPluginInstance(string pluginName, Version minimumVersion = null) {
            minimumVersion = minimumVersion ?? new Version(0, 0, 0);
            if(BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(pluginName, out PluginInfo target) 
               && null != target) {
                if (target.Metadata.Version >= minimumVersion) {
                    return target.Instance;
                }
                Logger.LogWarning($"{pluginName} v{target.Metadata.Version} is detacted OUTDATED.");
                Logger.LogWarning($"Please update {pluginName} to at least v{minimumVersion} to enable related feature.");
            }
            return null;
        }
    }
}
