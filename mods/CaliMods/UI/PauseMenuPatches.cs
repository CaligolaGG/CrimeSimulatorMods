using HarmonyLib;

namespace CaliMods.UI;

[HarmonyPatch]
internal static class PauseMenuPatches
{
    /// <summary>The pause menu is opened: make sure our button exists (added once per PauseMenu instance).</summary>
    [HarmonyPostfix, HarmonyPatch(typeof(FPP_Player), nameof(FPP_Player.ActivatePauseMenu))]
    private static void AfterActivatePauseMenu(FPP_Player __instance)
    {
        if (__instance.pauseActive && __instance.pauseMenu != null)
            PauseMenuButton.EnsureButton(__instance.pauseMenu);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(FPP_Player), nameof(FPP_Player.DisablePauseMenu))]
    private static void AfterDisablePauseMenu() => CustomModsMenu.Close();

    /// <summary>Any other page (ESC back to the main page, settings, quit...) closes our window.</summary>
    [HarmonyPostfix, HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.ActivatePage))]
    private static void AfterActivatePage() => CustomModsMenu.OnPauseMenuPageChanged();
}
