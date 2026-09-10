using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FletchersForge;

/// Reserved real bag rows while a quiver is Fletcher-equipped.
/// Movable quiver UI is a view; ammo in these cells dumps to tombstone like normal items.
internal static class QuiverBagBridge
{
    internal const string RowActiveKey = "FF_QuiverBagRows";

    private static readonly MethodInfo AzuGetFullHeight =
        AccessTools.Method(AccessTools.TypeByName("AzuEPI.API"), "GetFullHeight", new[] { typeof(int) })
        ?? AccessTools.Method(AccessTools.TypeByName("AzuExtendedPlayerInventory.API"), "GetFullHeight", new[] { typeof(int) });

    private static bool syncing;
    private static int activeBaseHeight = -1;
    private static int activeExtraRows;

    internal static bool IsSyncing => syncing;

    internal static int GetExtraRowCount(int width)
    {
        width = Mathf.Max(1, width);
        return Mathf.CeilToInt(ModConstants.QuiverSlotCount / (float)width);
    }

    internal static int GetAzuOrVanillaHeight(int width)
    {
        width = Mathf.Max(1, width);
        if (AzuGetFullHeight != null)
        {
            try
            {
                object result = AzuGetFullHeight.Invoke(null, new object[] { width });
                if (result is int h && h > 0)
                {
                    return h;
                }
            }
            catch (Exception ex)
            {
                FletchersForgePlugin.Log?.LogWarning($"Quiver bag bridge: GetFullHeight failed: {ex.Message}");
            }
        }

        // Vanilla 1.0: Haldor "Deeper Pockets" stores row count in unique key invrows (default 4, max 9).
        Player player = Player.m_localPlayer;
        if (player != null &&
            player.TryGetUniqueKeyValue(Player.InventoryRowsKey, out string invRows) &&
            int.TryParse(invRows, out int rows) &&
            rows > 0)
        {
            return Mathf.Clamp(rows, 1, 9);
        }

        return 4;
    }

    internal static int GetHeight(Inventory inventory)
    {
        if (inventory == null)
        {
            return 0;
        }

        return Traverse.Create(inventory).Field<int>("m_height").Value;
    }

    internal static void SetHeight(Inventory inventory, int height)
    {
        if (inventory == null)
        {
            return;
        }

        Traverse.Create(inventory).Field("m_height").SetValue(Mathf.Max(1, height));
    }

    /// First reserved row Y while rows are active; -1 if inactive.
    internal static int ReservedRowStart => activeBaseHeight;

    internal static int ExtraRowsActive => activeExtraRows;

    internal static bool IsReservedCell(Vector2i pos)
    {
        return activeExtraRows > 0 &&
               activeBaseHeight >= 0 &&
               pos.y >= activeBaseHeight &&
               pos.y < activeBaseHeight + activeExtraRows &&
               pos.x >= 0 &&
               pos.x < ModConstants.QuiverSlotCount;
    }

    internal static Vector2i SlotToBagPos(int slot)
    {
        int width = Mathf.Max(1, Player.m_localPlayer?.GetInventory()?.GetWidth() ?? 8);
        int row = activeBaseHeight + (slot / width);
        int col = slot % width;
        return new Vector2i(col, row);
    }

    internal static bool TryBagPosToSlot(Vector2i pos, out int slot)
    {
        slot = -1;
        if (!IsReservedCell(pos))
        {
            return false;
        }

        int width = Mathf.Max(1, Player.m_localPlayer?.GetInventory()?.GetWidth() ?? 8);
        slot = ((pos.y - activeBaseHeight) * width) + pos.x;
        return slot >= 0 && slot < ModConstants.QuiverSlotCount;
    }

    /// Grow player bag so quiver ammo can live as real cells (tombstone uses the same height).
    internal static void EnsureRows(Player player)
    {
        Inventory bag = player?.GetInventory();
        if (bag == null)
        {
            return;
        }

        int width = Mathf.Max(1, bag.GetWidth());
        int baseHeight = GetAzuOrVanillaHeight(width);
        int extra = GetExtraRowCount(width);
        int target = baseHeight + extra;

        // If AzuEPI already set a taller height, treat current non-quiver height as base.
        int current = GetHeight(bag);
        if (activeExtraRows <= 0 && current > baseHeight)
        {
            baseHeight = current;
            target = baseHeight + extra;
        }

        activeBaseHeight = baseHeight;
        activeExtraRows = extra;

        if (current < target)
        {
            SetHeight(bag, target);
        }

        FletchersForgePlugin.Log?.LogInfo(
            $"Quiver bag rows: base={baseHeight}, extra={extra}, height={GetHeight(bag)}, reservedY={activeBaseHeight}.");

        Container tombPrefab = player.m_tombstone != null
            ? player.m_tombstone.GetComponent<Container>()
            : null;
        if (tombPrefab != null && tombPrefab.m_height < target)
        {
            tombPrefab.m_height = target;
        }
    }

    internal static void ReleaseRows(Player player)
    {
        Inventory bag = player?.GetInventory();
        if (bag == null)
        {
            activeBaseHeight = -1;
            activeExtraRows = 0;
            return;
        }

        if (activeExtraRows > 0 && activeBaseHeight >= 0)
        {
            int target = Mathf.Max(1, activeBaseHeight);
            if (GetHeight(bag) > target)
            {
                SetHeight(bag, target);
            }

            Container tombPrefab = player.m_tombstone != null
                ? player.m_tombstone.GetComponent<Container>()
                : null;
            if (tombPrefab != null && tombPrefab.m_height > target)
            {
                tombPrefab.m_height = target;
            }
        }

        activeBaseHeight = -1;
        activeExtraRows = 0;
    }

    /// Move quiver UI inventory into reserved bag cells (same ItemData references).
    internal static void PushUiToBag(Player player)
    {
        if (syncing || player == null)
        {
            return;
        }

        Inventory bag = player.GetInventory();
        Inventory ui = QuiverInventory.Inventory;
        if (bag == null || ui == null)
        {
            return;
        }

        EnsureRows(player);
        if (activeExtraRows <= 0 || activeBaseHeight < 0)
        {
            FletchersForgePlugin.Log?.LogWarning("Quiver bag bridge: reserved rows inactive; cannot push.");
            return;
        }

        int height = GetHeight(bag);
        int target = activeBaseHeight + activeExtraRows;
        if (height < target)
        {
            SetHeight(bag, target);
            height = target;
        }

        syncing = true;
        try
        {
            ClearReservedCells(bag);

            List<ItemDrop.ItemData> bagList =
                Traverse.Create(bag).Field<List<ItemDrop.ItemData>>("m_inventory").Value;
            if (bagList == null)
            {
                FletchersForgePlugin.Log?.LogError("Quiver bag bridge: player m_inventory list is null.");
                return;
            }

            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>(ui.GetAllItems());
            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null)
                {
                    continue;
                }

                int slot = Mathf.Clamp(item.m_gridPos.x, 0, ModConstants.QuiverSlotCount - 1);
                Vector2i bagPos = SlotToBagPos(slot);
                if (bagPos.y < 0 || bagPos.y >= height || bagPos.x < 0 || bagPos.x >= bag.GetWidth())
                {
                    FletchersForgePlugin.Log?.LogWarning(
                        $"Quiver bag bridge: reserved pos ({bagPos.x},{bagPos.y}) out of bounds height={height}.");
                    continue;
                }

                ui.RemoveItem(item);
                // Bypass AzuEPI AddItem guards (equipment/quick slot validation) — these cells are ours.
                ItemDrop.ItemData occupant = bag.GetItemAt(bagPos.x, bagPos.y);
                if (occupant != null && occupant != item)
                {
                    bag.RemoveItem(occupant);
                }

                item.m_gridPos = bagPos;
                if (!bagList.Contains(item))
                {
                    bagList.Add(item);
                }
            }

            Traverse.Create(bag).Method("Changed").GetValue();
        }
        finally
        {
            syncing = false;
        }
    }

    /// Move reserved bag cells into the quiver UI inventory for the movable row.
    internal static void PullBagToUi(Player player)
    {
        if (syncing || player == null)
        {
            return;
        }

        Inventory bag = player.GetInventory();
        Inventory ui = QuiverInventory.Inventory;
        if (bag == null || ui == null || activeExtraRows <= 0 || activeBaseHeight < 0)
        {
            return;
        }

        syncing = true;
        try
        {
            List<ItemDrop.ItemData> bagList =
                Traverse.Create(bag).Field<List<ItemDrop.ItemData>>("m_inventory").Value;
            if (bagList == null)
            {
                return;
            }

            bool anyInBag = false;
            for (int slot = 0; slot < ModConstants.QuiverSlotCount; slot++)
            {
                Vector2i bagPos = SlotToBagPos(slot);
                if (bag.GetItemAt(bagPos.x, bagPos.y) != null)
                {
                    anyInBag = true;
                    break;
                }
            }

            // Never wipe UI if bag reserved cells are empty (failed push / mid-sync).
            if (!anyInBag)
            {
                return;
            }

            ui.RemoveAll();
            for (int slot = 0; slot < ModConstants.QuiverSlotCount; slot++)
            {
                Vector2i bagPos = SlotToBagPos(slot);
                ItemDrop.ItemData item = bag.GetItemAt(bagPos.x, bagPos.y);
                if (item == null)
                {
                    continue;
                }

                bagList.Remove(item);
                item.m_gridPos = new Vector2i(slot, 0);
                ui.AddItem(item);
            }

            Traverse.Create(bag).Method("Changed").GetValue();
        }
        finally
        {
            syncing = false;
        }
    }

    internal static void ClearReservedCells(Inventory bag)
    {
        if (bag == null || activeExtraRows <= 0 || activeBaseHeight < 0)
        {
            return;
        }

        List<ItemDrop.ItemData> bagList =
            Traverse.Create(bag).Field<List<ItemDrop.ItemData>>("m_inventory").Value;
        if (bagList == null)
        {
            return;
        }

        List<ItemDrop.ItemData> remove = new List<ItemDrop.ItemData>();
        foreach (ItemDrop.ItemData item in bag.GetAllItems())
        {
            if (item != null && IsReservedCell(item.m_gridPos))
            {
                remove.Add(item);
            }
        }

        foreach (ItemDrop.ItemData item in remove)
        {
            bagList.Remove(item);
        }

        if (remove.Count > 0)
        {
            Traverse.Create(bag).Method("Changed").GetValue();
        }
    }

    /// Hide reserved-row cells on the main backpack grid so only the movable quiver row is used.
    internal static void HideReservedOnPlayerGrid(InventoryGrid playerGrid)
    {
        if (playerGrid == null || activeExtraRows <= 0 || activeBaseHeight < 0)
        {
            return;
        }

        System.Collections.IList elements =
            Traverse.Create(playerGrid).Field("m_elements").GetValue() as System.Collections.IList;
        if (elements == null)
        {
            return;
        }

        foreach (object element in elements)
        {
            if (element is not InventoryElement invElement)
            {
                continue;
            }

            Vector2i pos = invElement.Position;
            GameObject go = invElement.gameObject;
            bool hide = IsReservedCell(pos);
            if (go.activeSelf == hide)
            {
                go.SetActive(!hide);
            }
        }
    }

    /// Apply matching height on a live tombstone after AzuEPI Awake.
    internal static void ApplyTombstoneHeight(Container container)
    {
        if (container == null || activeExtraRows <= 0 || activeBaseHeight < 0)
        {
            return;
        }

        int target = activeBaseHeight + activeExtraRows;
        if (container.m_height < target)
        {
            container.m_height = target;
        }

        Inventory inv = container.GetInventory();
        if (inv != null && GetHeight(inv) < target)
        {
            SetHeight(inv, target);
        }
    }

    internal static void ApplyTombstoneHeightForPlayer(Player player, Container container)
    {
        if (player == null || container == null)
        {
            return;
        }

        Inventory bag = player.GetInventory();
        int width = bag != null ? bag.GetWidth() : container.m_width;
        int baseHeight = GetAzuOrVanillaHeight(width);
        int current = bag != null ? GetHeight(bag) : baseHeight;
        // Prefer live bag height (includes our rows while equipped).
        int target = Mathf.Max(current, baseHeight);
        if (QuiverInventory.PlayerHasEquippedQuiver(player) && activeExtraRows > 0)
        {
            target = Mathf.Max(target, activeBaseHeight + activeExtraRows);
        }

        if (container.m_height < target)
        {
            container.m_height = target;
        }

        Inventory inv = container.GetInventory();
        if (inv != null && GetHeight(inv) < target)
        {
            SetHeight(inv, target);
        }
    }
}
