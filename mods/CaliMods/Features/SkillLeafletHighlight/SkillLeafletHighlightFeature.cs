using BepInEx.Configuration;
using CaliMods.Core;
using UnityEngine;

namespace CaliMods.Features.SkillLeafletHighlight;

/// <summary>
/// "Skill leaflet highlight": a skill leaflet (<c>Item.isSkillBook</c>, teaches <c>Item.skillLearnID</c>)
/// gets a red outline when the local player already learned its skill
/// (<c>PlayerSkills.CheckIfSkillLearned</c>, the same check the game makes when you read one).
/// While on, it claims every leaflet, so the item-request coloring never applies to leaflets
/// (unlearned ones keep the game's green outline). The "(owned:N)" prompt is unaffected.
/// Client-side only (visuals), safe in co-op.
/// </summary>
internal static class SkillLeafletHighlightFeature
{
    internal static ModFeature Feature;
    internal static ConfigEntry<Color> LearnedColor;

    internal static void Register(ConfigFile config)
    {
        Feature = FeatureRegistry.Register(config, "Skill leaflet highlight",
            "Red outline on skill leaflets whose skill you already learned. Leaflets ignore the item request colors.");

        LearnedColor = config.Bind(Feature.Name, "LearnedColor", new Color(1f, 0f, 0f, 1f),
            "Outline color of skill leaflets whose skill you already learned.");

        ItemOutline.AddRule(OutlinePriority.SkillLeaflets, OutlineColor);
    }

    private static bool OutlineColor(Pickupable pickup, out Color? color)
    {
        color = null;
        var item = pickup.assignedItem;
        if (!Feature.IsOn() || !item.isSkillBook)
            return false;

        var skills = WorldManager.Instance != null && WorldManager.Instance.localPlayer != null
            ? WorldManager.Instance.localPlayer.skillManager
            : null;
        if (skills != null && !string.IsNullOrEmpty(item.skillLearnID) && skills.CheckIfSkillLearned(item.skillLearnID))
            color = LearnedColor.Value;
        return true; // handled: leaflets never get the request colors
    }
}
