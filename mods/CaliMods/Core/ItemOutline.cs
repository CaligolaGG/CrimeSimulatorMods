using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HighlightPlus;
using UnityEngine;

namespace CaliMods.Core;

/// <summary>
/// Decides the outline color of a looked-at item. Return true when the rule handles this item
/// (later rules are skipped); <paramref name="color"/> = null keeps the game's default (green) outline.
/// </summary>
internal delegate bool OutlineRule(Pickupable pickup, out Color? color);

/// <summary>Order in which outline rules are evaluated (lowest first).</summary>
internal static class OutlinePriority
{
    /// <summary>Skill leaflets: claim every leaflet so request coloring never applies to them.</summary>
    internal const int SkillLeaflets = 100;
    internal const int ItemRequests = 200;
}

/// <summary>
/// The looked-at object gets <c>Highlight.HighlightMe()</c> every frame (FPP_Player / DroneController),
/// which turns on its HighlightPlus effect loaded from <c>WorldManager.pickupableHighlightProfile</c>
/// (green outline, strength 0.75, no glow). Features register <see cref="OutlineRule"/>s in priority order;
/// the first one that handles the item picks the color. The effect's outline (+ glow, unused by the game
/// profile) is recolored per object at full strength, and the profile values are restored afterwards.
/// </summary>
[HarmonyPatch]
internal static class ItemOutline
{
    private static readonly List<(int Priority, OutlineRule Rule)> Rules = new();

    /// <summary>Lower priority values are evaluated first (see <see cref="OutlinePriority"/>).</summary>
    internal static void AddRule(int priority, OutlineRule rule)
    {
        Rules.Add((priority, rule));
        Rules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    private sealed class OriginalColors
    {
        public Color Outline;
        public float OutlineStrength;
        public ColorStyle OutlineStyle;
        public Color GlowHQ;
        public Color[] GlowPasses;
        public bool Modified;
        public Color Applied;
    }

    private static readonly ConditionalWeakTable<HighlightEffect, OriginalColors> Originals = new();
    private static bool _loggedProfile;

    [HarmonyPostfix, HarmonyPatch(typeof(Highlight), nameof(Highlight.HighlightMe))]
    private static void AfterHighlightMe(Highlight __instance)
    {
        var effect = __instance.mainHighlight;
        if (effect == null)
            return;

        Color? color = null;
        var pickup = __instance.GetComponent<Pickupable>();
        if (pickup != null && pickup.assignedItem != null && !pickup.assignedItem.isMoney)
        {
            foreach (var (_, rule) in Rules)
            {
                if (rule(pickup, out color))
                    break;
            }
        }

        var original = Originals.GetValue(effect, Capture);
        if (color == null)
        {
            if (original.Modified)
                Restore(effect, original);
            return;
        }

        if (original.Modified && original.Applied == color.Value)
            return;
        Apply(effect, original, color.Value);
    }

    private static OriginalColors Capture(HighlightEffect e)
    {
        if (!_loggedProfile)
        {
            _loggedProfile = true;
            Plugin.Log.LogInfo($"Pickup highlight profile: outline={e.outline} color={e.outlineColor} style={e.outlineColorStyle}, " +
                               $"glow={e.glow} glowHQColor={e.glowHQColor} passes={e.glowPasses?.Length ?? 0}, innerGlow={e.innerGlow}, overlay={e.overlay}");
        }

        var o = new OriginalColors
        {
            Outline = e.outlineColor,
            OutlineStrength = e.outline,
            OutlineStyle = e.outlineColorStyle,
            GlowHQ = e.glowHQColor,
        };
        if (e.glowPasses != null)
        {
            o.GlowPasses = new Color[e.glowPasses.Length];
            for (int i = 0; i < e.glowPasses.Length; i++)
                o.GlowPasses[i] = e.glowPasses[i].color;
        }
        return o;
    }

    private static void Apply(HighlightEffect e, OriginalColors o, Color color)
    {
        e.outlineColorStyle = ColorStyle.SingleColor;
        e.outlineColor = ColorUtil.WithAlpha(color, o.Outline.a);
        // The game's profile draws the outline at 0.75 strength; recolored items get full strength.
        e.outline = Mathf.Max(o.OutlineStrength, 1f);
        e.glowHQColor = ColorUtil.WithAlpha(color, o.GlowHQ.a);
        if (e.glowPasses != null && o.GlowPasses != null)
        {
            for (int i = 0; i < e.glowPasses.Length && i < o.GlowPasses.Length; i++)
                e.glowPasses[i].color = ColorUtil.WithAlpha(color, o.GlowPasses[i].a);
        }
        o.Modified = true;
        o.Applied = color;
        e.UpdateMaterialProperties();
    }

    private static void Restore(HighlightEffect e, OriginalColors o)
    {
        e.outlineColorStyle = o.OutlineStyle;
        e.outlineColor = o.Outline;
        e.outline = o.OutlineStrength;
        e.glowHQColor = o.GlowHQ;
        if (e.glowPasses != null && o.GlowPasses != null)
        {
            for (int i = 0; i < e.glowPasses.Length && i < o.GlowPasses.Length; i++)
                e.glowPasses[i].color = o.GlowPasses[i];
        }
        o.Modified = false;
        e.UpdateMaterialProperties();
    }
}

/// <summary>Outside the [HarmonyPatch] class: the Harmony analyzer flags struct parameter writes there.</summary>
internal static class ColorUtil
{
    internal static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);
}
