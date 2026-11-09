using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CaliMods.UI;

/// <summary>
/// Adds a "Custom Mods" button to the pause menu's main page by cloning one of its buttons
/// (keeps the game's look and layout), right above the last button of that group (usually Quit).
/// </summary>
internal static class PauseMenuButton
{
    internal const string ButtonObjectName = "CaliMods_CustomModsButton";
    internal const string Label = "Custom Mods";

    internal static void EnsureButton(PauseMenu menu)
    {
        try
        {
            AddButton(menu);
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"Pause menu: failed to add the \"{Label}\" button: {e}");
        }
    }

    private static void AddButton(PauseMenu menu)
    {
        if (menu.pages == null || menu.pages.Count == 0 || menu.pages[0] == null)
            return;

        var mainPage = menu.pages[0].transform;
        if (mainPage.GetComponentsInChildren<Transform>(true).Any(t => t.name == ButtonObjectName))
            return;

        var template = PickTemplate(menu, mainPage);
        if (template == null)
        {
            Plugin.Log.LogWarning("Pause menu: no button found to clone, \"Custom Mods\" button not added.");
            return;
        }

        var parent = template.transform.parent;
        var clone = Object.Instantiate(template.gameObject, parent);
        clone.name = ButtonObjectName;
        clone.transform.localScale = template.transform.localScale;
        clone.SetActive(true);

        // The game's I2 Localize components would overwrite our label with the template's translation.
        foreach (var loc in clone.GetComponentsInChildren<I2.Loc.Localize>(true))
            Object.DestroyImmediate(loc);

        foreach (var info in clone.GetComponentsInChildren<ButtonInformation>(true))
        {
            info.textOnUnlocked = "Enable or disable CaliMods features";
            info.textOnLocked = "";
            info.translate = false;
        }

        foreach (var tmp in clone.GetComponentsInChildren<TextMeshProUGUI>(true))
            tmp.text = Label;
        foreach (var txt in clone.GetComponentsInChildren<Text>(true))
            txt.text = Label;

        var button = clone.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent(); // drop the template's persistent listeners
        button.interactable = true;
        button.onClick.AddListener(() =>
        {
            menu.PlayClick();
            CustomModsMenu.Open(menu);
        });

        var siblings = parent.GetComponentsInChildren<Button>(true)
            .Where(b => b.transform.parent == parent && b.gameObject != clone)
            .Select(b => (RectTransform)b.transform)
            .ToList();

        // Place it just before the last sibling button (Quit).
        if (siblings.Count > 0)
            clone.transform.SetSiblingIndex(siblings.Max(r => r.GetSiblingIndex()));

        bool laidOut = parent.GetComponent<LayoutGroup>() != null;
        if (laidOut)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)parent);
        }
        else if (siblings.Count > 0)
        {
            // No layout group: put it under the lowest button so it doesn't overlap.
            var lowest = siblings.OrderBy(r => r.anchoredPosition.y).First();
            ((RectTransform)clone.transform).anchoredPosition =
                lowest.anchoredPosition - new Vector2(0f, lowest.rect.height * 1.25f);
        }

        if (menu.layoutRefresh != null)
            menu.layoutRefresh.RefreshImmediately();

        Plugin.Log.LogInfo($"Pause menu: added \"{Label}\" button (template '{PathOf(template.transform)}', layout group: {laidOut}).");
    }

    private static Button PickTemplate(PauseMenu menu, Transform mainPage)
    {
        // The quit button is usually the last entry of the main page's list; prefer it when it's there.
        if (menu.quitGameButton != null && menu.quitGameButton.transform.IsChildOf(mainPage))
            return menu.quitGameButton;

        return mainPage.GetComponentsInChildren<Button>(false)
            .Where(b => b.GetComponentInChildren<TextMeshProUGUI>(true) != null)
            .OrderByDescending(b => b.transform.parent.GetComponent<LayoutGroup>() != null)
            .FirstOrDefault();
    }

    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
}
