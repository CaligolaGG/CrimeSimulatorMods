using BepInEx;
using BepInEx.Logging;
using CaliMods.Core;
using CaliMods.Features.RequestItemHighlight;
using CaliMods.Features.SkillLeafletHighlight;
using CaliMods.UI;
using HarmonyLib;

namespace CaliMods;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Crime Simulator.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;

    private void Awake()
    {
        Log = Logger;

        // Every feature binds its config entries and registers itself in the "Custom Mods" menu.
        RequestItemHighlightFeature.Register(Config);
        SkillLeafletHighlightFeature.Register(Config);

        new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll(typeof(Plugin).Assembly);

        // The menu lives on its own DontDestroyOnLoad object: never rely on the plugin's
        // own GameObject surviving scene loads.
        CustomModsMenu.CreateHost();

        Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded ({FeatureRegistry.All.Count} features).");
    }
}
