using System.Collections.Generic;
using UnityEngine;

namespace CaliMods.Features.RequestItemHighlight;

internal enum RequestStatus
{
    /// <summary>Not in any item request (or the requests aren't available).</summary>
    None,
    /// <summary>An open item request still needs it: you don't own enough copies yet.</summary>
    Needed,
    /// <summary>In an item request that is claimed, or you already own enough copies for the open ones.</summary>
    Fulfilled,
}

/// <summary>
/// Reads the item requests from the computer (<c>Computer.Instance.requests.allRequests</c>) and counts
/// owned items the same way the game's request page does (<c>ItemRequestUI.Refresh</c>): local inventory
/// + items inside the truck (<c>WorldManager.truckItemsSaver.sensor</c>), plus every item lying around
/// when in the hideout. Damaged copies don't count (damage above <c>MaxDamage</c>, default 0). Results are cached for a short time; only queried while looking at an item.
/// </summary>
internal static class RequestTracker
{
    private const float CacheSeconds = 0.5f;

    private static float _lastRefresh = -999f;
    private static bool _requestsAvailable;
    private static readonly Dictionary<Item, int> RequiredByOpenRequests = new();
    private static readonly HashSet<Item> InCompletedRequests = new();
    private static readonly Dictionary<Item, int> Owned = new();
    private static readonly HashSet<Pickupable> Seen = new();

    internal static RequestStatus GetStatus(Item item)
    {
        if (item == null)
            return RequestStatus.None;
        RefreshIfStale();

        if (RequiredByOpenRequests.TryGetValue(item, out int required))
            return OwnedCount(item) >= required ? RequestStatus.Fulfilled : RequestStatus.Needed;
        if (InCompletedRequests.Contains(item))
            return RequestStatus.Fulfilled;
        if (!_requestsAvailable && item.isRequested) // fallback: the game's own flag (open requests only)
            return OwnedCount(item) > 0 ? RequestStatus.Fulfilled : RequestStatus.Needed;
        return RequestStatus.None;
    }

    internal static int OwnedCount(Item item)
    {
        if (item == null)
            return 0;
        RefreshIfStale();
        return Owned.TryGetValue(item, out int n) ? n : 0;
    }

    /// <summary>Forces a refresh on the next query (e.g. right after a pickup changed the inventory).</summary>
    internal static void Invalidate() => _lastRefresh = -999f;

    private static void RefreshIfStale()
    {
        if (Time.unscaledTime - _lastRefresh < CacheSeconds)
            return;
        _lastRefresh = Time.unscaledTime;
        RefreshRequests();
        RefreshOwned();
    }

    private static void RefreshRequests()
    {
        RequiredByOpenRequests.Clear();
        InCompletedRequests.Clear();
        _requestsAvailable = false;

        var computer = Computer.Instance;
        var page = computer != null ? computer.requests : null;
        if (page == null || page.allRequests == null)
            return;
        _requestsAvailable = true;

        foreach (var request in page.allRequests)
        {
            if (request == null || request.requests == null)
                continue;
            if (!TryReadState(request, out bool completed, out bool dlcNotOwned) || dlcNotOwned)
                continue;

            foreach (var entry in request.requests)
            {
                var item = entry?.requestedItem;
                if (item == null)
                    continue;
                if (completed)
                    InCompletedRequests.Add(item);
                else
                    RequiredByOpenRequests[item] = RequiredByOpenRequests.TryGetValue(item, out int n) ? n + 1 : 1;
            }
        }
    }

    /// <summary><c>isCompleted</c>/<c>dlcNotOwned</c> are Fusion [Networked] properties: readable only once spawned.</summary>
    private static bool TryReadState(ItemRequestUI request, out bool completed, out bool dlcNotOwned)
    {
        completed = dlcNotOwned = false;
        try
        {
            if (request.Object == null || !request.Object.IsValid)
                return false;
            completed = request.isCompleted;
            dlcNotOwned = request.requiredDLC && request.dlcNotOwned;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void RefreshOwned()
    {
        Owned.Clear();
        Seen.Clear();

        var wm = WorldManager.Instance;
        if (wm == null)
            return;

        var player = wm.localPlayer;
        if (player != null && player.inventory != null && player.inventory.itemSlots != null)
        {
            bool carriedIsInSlot = false;
            var weapons = player.inventory.weaponManager;
            var carried = weapons != null && weapons.isCarrying ? weapons.carriedItem : null;
            foreach (var slot in player.inventory.itemSlots)
            {
                if (slot == null || slot.assignedItem == null)
                    continue;
                if (slot.assignedItem == carried)
                    carriedIsInSlot = true;

                // One damage value per stacked copy; items without damage tracking count as intact.
                var damages = slot.damagesPerItem;
                if (damages == null || damages.Count == 0)
                {
                    Add(slot.assignedItem, Mathf.Max(1, slot.amount));
                    continue;
                }
                int intact = 0;
                foreach (float d in damages)
                    if (IsIntact(d))
                        intact++;
                if (intact > 0)
                    Add(slot.assignedItem, intact);
            }
            // Big items (hasToBeCarried) are held in the hands, not in a slot.
            if (carried != null && !carriedIsInSlot && IsIntact(weapons.carriedItemDamage))
                Add(carried, 1);
        }

        var truck = wm.truckItemsSaver;
        if (truck != null && truck.sensor != null)
        {
            foreach (var go in truck.sensor.Detections)
                AddPickupable(go);
        }

        // In the hideout everything lying around is yours. (Not on job maps, where the scene is full of loot.)
        if (wm.loadedMapName == "PlayerHideout")
        {
            foreach (var go in GameObject.FindGameObjectsWithTag("Pickupable"))
                AddPickupable(go);
        }
    }

    private static void AddPickupable(GameObject go)
    {
        if (go == null)
            return;
        var p = go.GetComponent<Pickupable>();
        if (p == null || p.assignedItem == null || p.assignedItem.isMoney || !Seen.Add(p))
            return;
        if (IsIntact(p.currentDamage))
            Add(p.assignedItem, 1);
    }

    /// <summary>Damaged copies (damage % above the MaxDamage setting, default 0) aren't counted as owned.</summary>
    private static bool IsIntact(float damage) => damage <= RequestItemHighlightFeature.MaxDamage.Value + 0.001f;

    private static void Add(Item item, int n) => Owned[item] = Owned.TryGetValue(item, out int c) ? c + n : n;
}
