using System;
using System.Reflection;
using BepInEx.Configuration;
using MonoMod.Utils;

namespace Moonswept.Config;

internal abstract class MoonsweptConfig {
    public abstract void Initialize(ConfigFile configFile);

    public static void InitializeConfigs(ConfigFile configFile) {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes()) {
            try {
                if (type == typeof(MoonsweptConfig)) continue;

                if (!type.IsSubclassOf(typeof(MoonsweptConfig))) continue;

                var config = Activator.CreateInstance(type) as MoonsweptConfig;

                config?.Initialize(configFile);
            } catch (Exception exception) {
                exception.LogDetailed();
            }
        }
    }
}

// ReSharper disable ArrangeMethodOrOperatorBody
public static class ConfigHelper {
    public static ConfigEntry<int> BindInt(this ConfigFile configFile, string section, string key, int defaultValue, string description, int min = 0,
                                           int max = 100) {
        return configFile.Bind(section, key, defaultValue, new ConfigDescription(description, new AcceptableValueRange<int>(min, max)));
    }

    public static ConfigEntry<float> BindFloat(this ConfigFile configFile, string section, string key, float defaultValue, string description, float min = 0F,
                                               float max = 1F) {
        return configFile.Bind(section, key, defaultValue, new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));
    }

    public static ConfigEntry<bool> BindBool(this ConfigFile configFile, string section, string key, bool defaultValue, string description) {
        return configFile.Bind(section, key, defaultValue, description);
    }

    public static ConfigEntry<string> BindString(this ConfigFile configFile, string section, string key, string defaultValue, string description) {
        return configFile.Bind(section, key, defaultValue, description);
    }
}