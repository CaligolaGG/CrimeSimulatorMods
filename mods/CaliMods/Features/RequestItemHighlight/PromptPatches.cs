using HarmonyLib;
using I2.Loc;

namespace CaliMods.Features.RequestItemHighlight;

/// <summary>
/// <c>FPP_Player.InteractSensorDetected</c> rebuilds the interaction prompts whenever the looked-at
/// object changes; for items it calls <c>InteractUI.SpawnPrompt(key, "Pick up: &lt;name&gt;...")</c>.
/// While it runs we append "(owned:N)" to that pickup prompt.
/// </summary>
[HarmonyPatch]
internal static class PromptPatches
{
    private static FPP_Player _detectingPlayer;

    [HarmonyPrefix, HarmonyPatch(typeof(FPP_Player), nameof(FPP_Player.InteractSensorDetected))]
    private static void BeforeInteractSensorDetected(FPP_Player __instance)
    {
        _detectingPlayer = __instance;
        RequestTracker.Invalidate(); // looked-at object changed: counts may have changed since (pickups, drops)
    }

    [HarmonyFinalizer, HarmonyPatch(typeof(FPP_Player), nameof(FPP_Player.InteractSensorDetected))]
    private static void AfterInteractSensorDetected() => _detectingPlayer = null;

    [HarmonyPrefix, HarmonyPatch(typeof(InteractUI), nameof(InteractUI.SpawnPrompt))]
    private static void BeforeSpawnPrompt(ref string text)
    {
        var player = _detectingPlayer;
        if (player == null || player.currentDetected == null || string.IsNullOrEmpty(text))
            return;
        if (!RequestItemHighlightFeature.Feature.IsOn(RequestItemHighlightFeature.OwnedCount))
            return;

        var pickup = player.currentDetected.GetComponent<Pickupable>();
        if (pickup == null || pickup.assignedItem == null || pickup.assignedItem.isMoney)
            return;

        string pickUpPrefix = LocalizationManager.GetTranslation("pick up") + ": ";
        if (!text.StartsWith(pickUpPrefix))
            return;

        text += $" (owned:{RequestTracker.OwnedCount(pickup.assignedItem)})";
    }
}
