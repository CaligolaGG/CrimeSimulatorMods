using System.Collections.Generic;
using BepInEx.Configuration;

namespace CaliMods.Core;

/// <summary>A toggle shown under a feature in the "Custom Mods" menu.</summary>
public sealed class FeatureOption
{
    public string Name;
    public string Description;
    public ConfigEntry<bool> Value;
}

/// <summary>
/// One feature of the pack: a master toggle plus optional sub-options.
/// Values are BepInEx config entries (BepInEx/config/com.crimesimmods.calimods.cfg), saved on change.
/// </summary>
public sealed class ModFeature
{
    public string Name;
    public string Description;
    public ConfigEntry<bool> Enabled;
    public readonly List<FeatureOption> Options = new();

    /// <summary>True when the feature and the given option (if any) are both on.</summary>
    public bool IsOn(FeatureOption option = null) => Enabled.Value && (option == null || option.Value.Value);

    public FeatureOption AddOption(ConfigFile config, string key, string name, string description, bool defaultValue = true)
    {
        var option = new FeatureOption
        {
            Name = name,
            Description = description,
            Value = config.Bind(Name, key, defaultValue, description),
        };
        Options.Add(option);
        return option;
    }
}

public static class FeatureRegistry
{
    public static readonly List<ModFeature> All = new();

    /// <summary>Creates a feature whose config section is <paramref name="name"/>.</summary>
    public static ModFeature Register(ConfigFile config, string name, string description, bool defaultEnabled = true)
    {
        var feature = new ModFeature
        {
            Name = name,
            Description = description,
            Enabled = config.Bind(name, "Enabled", defaultEnabled, description),
        };
        All.Add(feature);
        return feature;
    }
}
