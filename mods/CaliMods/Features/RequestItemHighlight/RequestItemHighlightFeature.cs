using BepInEx.Configuration;
using CaliMods.Core;
using UnityEngine;

namespace CaliMods.Features.RequestItemHighlight;

/// <summary>
/// "Request item highlight": while looking at an item that appears in the computer's Item requests,
/// its outline is red (still needed) or yellow (request fulfilled), and every pickup prompt shows
/// how many of that item you already own: "[E] Pick up: Cheap toaster (owned:1)".
/// Client-side only (visuals), safe in co-op.
/// </summary>
internal static class RequestItemHighlightFeature
{
    internal static ModFeature Feature;
    internal static FeatureOption Outline;
    internal static FeatureOption OwnedCount;
    internal static ConfigEntry<Color> NeededColor;
    internal static ConfigEntry<Color> FulfilledColor;
    internal static ConfigEntry<float> MaxDamage;

    internal static void Register(ConfigFile config)
    {
        Feature = FeatureRegistry.Register(config, "Request item highlight",
            "Highlights items from the computer's Item requests and shows how many of each item you own.");

        Outline = Feature.AddOption(config, "ColoredOutline", "Colored outline",
            "Red outline on items still needed by an item request, yellow when that request is already fulfilled.");
        OwnedCount = Feature.AddOption(config, "OwnedCount", "Owned count in pickup prompt",
            "Adds \"(owned:N)\" after the item name: N = undamaged copies in your inventory, hands and truck (whole hideout when there).");

        NeededColor = config.Bind(Feature.Name, "NeededColor", new Color(1f, 0f, 0f, 1f),
            "Outline color of items still needed by an open item request.");
        FulfilledColor = config.Bind(Feature.Name, "FulfilledColor", new Color(1f, 0.9f, 0f, 1f),
            "Outline color of requested items whose request is already fulfilled.");
        MaxDamage = config.Bind(Feature.Name, "MaxDamage", 50f,
            new ConfigDescription("Copies damaged more than this (%) don't count as owned. 0 = only undamaged copies count.",
                new AcceptableValueRange<float>(0f, 100f)));

        ItemOutline.AddRule(OutlinePriority.ItemRequests, OutlineColor);
    }

    private static bool OutlineColor(Pickupable pickup, out Color? color)
    {
        color = null;
        if (!Feature.IsOn(Outline))
            return false;
        var status = RequestTracker.GetStatus(pickup.assignedItem);
        if (status == RequestStatus.None)
            return false;
        color = status == RequestStatus.Needed ? NeededColor.Value : FulfilledColor.Value;
        return true;
    }
}
