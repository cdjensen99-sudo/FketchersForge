using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace FletchersForge;

/// Death: keep Fletcher ammo in reserved real bag cells so tombstone/Take All are vanilla.
/// Loot: Fletcher-equip the remembered quiver and reclaim tagged stacks into reserved cells.
internal static class QuiverTombstoneDump
{
    internal const string QuiverIdKey = "FF_QuiverId";
    internal const string DumpIdKey = "FF_QuiverDumpId";
    internal const string ExtraRowsZdoKey = "FF_TombExtraRows";
    internal const string AbsoluteHeightZdoKey = "FF_TombHeight";

    private static readonly int ExtraRowsZdoHash = ExtraRowsZdoKey.GetStableHashCode();
    private static readonly int AbsoluteHeightZdoHash = AbsoluteHeightZdoKey.GetStableHashCode();

    private static readonly HashSet<string> PendingRestoreEquipIds = new HashSet<string>();
    private static string preferredRestoreEquipId;
    private static bool deferredRestoreRequested;
    private static bool deferredEquipRequested;
    private static ItemDrop.ItemData deferredEquipQuiver;

    internal static void ClearPending()
    {
        PendingRestoreEquipIds.Clear();
        preferredRestoreEquipId = null;
    }

    internal static string EnsureQuiverId(ItemDrop.ItemData quiver)
    {
        Dictionary<string, string> data = EnsureCustomData(quiver);
        if (!data.TryGetValue(QuiverIdKey, out string id) || string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString("N");
            data[QuiverIdKey] = id;
        }

        return id;
    }

    internal static void RememberEquippedQuiver(ItemDrop.ItemData quiver)
    {
        if (quiver == null)
        {
            return;
        }

        string id = EnsureQuiverId(quiver);
        PendingRestoreEquipIds.Add(id);
        preferredRestoreEquipId = id;
    }

    internal static bool ShouldRestoreEquipFor(ItemDrop.ItemData quiver)
    {
        if (!QuiverInventory.IsQuiverItem(quiver) || PendingRestoreEquipIds.Count == 0)
        {
            return false;
        }

        Dictionary<string, string> data = EnsureCustomData(quiver);
        return data.TryGetValue(QuiverIdKey, out string id) &&
               !string.IsNullOrEmpty(id) &&
               PendingRestoreEquipIds.Contains(id);
    }

    /// Push ammo into reserved bag cells, strip Fletcher equip, keep height until grave copy.
    internal static void PreparePlayerDeathDump(Player player)
    {
        if (player == null)
        {
            return;
        }

        QuiverInventory.PrepareEquippedQuiverForDeath(player);
        FletchersForgePlugin.Log?.LogInfo(
            $"Death: quiver ammo left in reserved bag cells; restore-equip pending={PendingRestoreEquipIds.Count}.");
    }

    internal static void AfterMoveInventoryToGrave(Inventory playerBag)
    {
        Player player = Player.m_localPlayer;
        if (player != null && playerBag == player.GetInventory())
        {
            QuiverInventory.ReleaseRowsAfterGrave(player);
        }
    }

    internal static void RequestDeferredRestore()
    {
        deferredRestoreRequested = true;
    }

    internal static void ProcessDeferredRestore()
    {
        if (!deferredRestoreRequested)
        {
            return;
        }

        deferredRestoreRequested = false;
        Player player = Player.m_localPlayer;
        if (player == null || !player.IsOwner() || player.IsDead())
        {
            return;
        }

        ItemDrop.ItemData quiver = null;
        if (!string.IsNullOrEmpty(preferredRestoreEquipId))
        {
            quiver = FindQuiverById(player.GetInventory(), preferredRestoreEquipId);
        }

        if (quiver == null)
        {
            foreach (string id in PendingRestoreEquipIds)
            {
                quiver = FindQuiverById(player.GetInventory(), id);
                if (quiver != null)
                {
                    break;
                }
            }
        }

        if (quiver == null)
        {
            return;
        }

        FletchersForgePlugin.Log?.LogInfo("Deferred restore: Fletcher-equipping looted quiver and reclaiming ammo cells.");
        deferredEquipQuiver = quiver;
        deferredEquipRequested = true;
    }

    internal static void ProcessDeferredEquip()
    {
        if (!deferredEquipRequested)
        {
            return;
        }

        deferredEquipRequested = false;
        ItemDrop.ItemData quiver = deferredEquipQuiver;
        deferredEquipQuiver = null;
        Player player = Player.m_localPlayer;
        if (player == null || player.IsDead() || quiver == null)
        {
            return;
        }

        Inventory bag = player.GetInventory();
        if (bag == null || !bag.ContainsItem(quiver))
        {
            return;
        }

        QuiverInventory.EquipOnly(player, quiver);

        Dictionary<string, string> data = EnsureCustomData(quiver);
        if (data.TryGetValue(QuiverIdKey, out string id) && !string.IsNullOrEmpty(id))
        {
            PendingRestoreEquipIds.Remove(id);
            if (preferredRestoreEquipId == id)
            {
                preferredRestoreEquipId = null;
            }
        }
    }

    internal static int GetHeight(Inventory inventory) => QuiverBagBridge.GetHeight(inventory);

    internal static void SetHeight(Inventory inventory, int height) => QuiverBagBridge.SetHeight(inventory, height);

    internal static int GetSafeTombstoneHeight(int width) => QuiverBagBridge.GetAzuOrVanillaHeight(width);

    internal static int ReadAbsoluteHeightZdo(TombStone tomb)
    {
        ZDO zdo = GetTombZdo(tomb);
        return zdo == null ? 0 : Mathf.Max(0, zdo.GetInt(AbsoluteHeightZdoHash, 0));
    }

    internal static int ReadExtraRowsZdo(TombStone tomb)
    {
        ZDO zdo = GetTombZdo(tomb);
        return zdo == null ? 0 : Mathf.Max(0, zdo.GetInt(ExtraRowsZdoHash, 0));
    }

    /// Legacy 0.2.11 graves taller than AzuEPI — collapse overflow.
    internal static void SanitizeOversizedTombstone(TombStone tomb, Container container)
    {
        if (tomb == null || container == null)
        {
            return;
        }

        Inventory inv = container.GetInventory();
        if (inv == null)
        {
            return;
        }

        int safeHeight = GetSafeTombstoneHeight(container.m_width);
        // While dying with equipped quiver, tombstones must include our reserved rows.
        Player local = Player.m_localPlayer;
        if (local != null && QuiverBagBridge.ExtraRowsActive > 0)
        {
            QuiverBagBridge.ApplyTombstoneHeightForPlayer(local, container);
            safeHeight = Mathf.Max(safeHeight, QuiverBagBridge.ReservedRowStart + QuiverBagBridge.ExtraRowsActive);
        }

        int absolute = ReadAbsoluteHeightZdo(tomb);
        if (absolute <= 0)
        {
            int extra = ReadExtraRowsZdo(tomb);
            if (extra > 0)
            {
                absolute = GetSafeTombstoneHeight(container.m_width) + extra;
            }
        }

        int currentHeight = Mathf.Max(container.m_height, GetHeight(inv));
        int loadHeight = Mathf.Max(currentHeight, absolute);
        bool hasOverflowPos = false;
        foreach (ItemDrop.ItemData item in inv.GetAllItems())
        {
            if (item != null && item.m_gridPos.y >= safeHeight)
            {
                // Reserved quiver row on a matched-height grave is valid — not overflow.
                if (local != null &&
                    QuiverBagBridge.ExtraRowsActive > 0 &&
                    item.m_gridPos.y < QuiverBagBridge.ReservedRowStart + QuiverBagBridge.ExtraRowsActive)
                {
                    continue;
                }

                hasOverflowPos = true;
                break;
            }
        }

        if (loadHeight <= safeHeight && !hasOverflowPos)
        {
            return;
        }

        // Only sanitize legacy oversized graves, not matched quiver rows.
        if (absolute <= safeHeight && !hasOverflowPos)
        {
            return;
        }

        if (loadHeight > currentHeight || absolute > currentHeight)
        {
            container.m_height = loadHeight;
            SetHeight(inv, loadHeight);
            ForceContainerReload(container);
        }

        List<ItemDrop.ItemData> overflow = new List<ItemDrop.ItemData>();
        foreach (ItemDrop.ItemData item in inv.GetAllItems())
        {
            if (item != null && item.m_gridPos.y >= safeHeight)
            {
                overflow.Add(item);
            }
        }

        foreach (ItemDrop.ItemData item in overflow)
        {
            inv.RemoveItem(item);
        }

        container.m_height = safeHeight;
        SetHeight(inv, safeHeight);

        int refit = 0;
        int dropped = 0;
        Vector3 dropPos = tomb.transform.position + Vector3.up;
        foreach (ItemDrop.ItemData item in overflow)
        {
            if (inv.AddItem(item))
            {
                refit++;
            }
            else
            {
                ItemDrop.DropItem(item, 0, dropPos, Quaternion.identity);
                dropped++;
            }
        }

        FletchersForgePlugin.Log?.LogInfo(
            $"Tombstone sanitize: safeHeight={safeHeight}, overflow={overflow.Count}, refit={refit}, dropped={dropped}.");
    }

    private static void ForceContainerReload(Container container)
    {
        Traverse fields = Traverse.Create(container);
        fields.Field("m_lastRevision").SetValue(uint.MaxValue);
        fields.Field("m_lastDataString").SetValue("__ff_force_reload__");
        fields.Method("Load").GetValue();
    }

    private static ZDO GetTombZdo(TombStone tomb)
    {
        if (tomb == null)
        {
            return null;
        }

        ZNetView nview = tomb.GetComponent<ZNetView>();
        return nview != null && nview.IsValid() ? nview.GetZDO() : null;
    }

    private static ItemDrop.ItemData FindQuiverById(Inventory bag, string id)
    {
        if (bag == null || string.IsNullOrEmpty(id))
        {
            return null;
        }

        foreach (ItemDrop.ItemData item in bag.GetAllItems())
        {
            if (!QuiverInventory.IsQuiverItem(item))
            {
                continue;
            }

            Dictionary<string, string> data = EnsureCustomData(item);
            if (data.TryGetValue(QuiverIdKey, out string quiverId) && quiverId == id)
            {
                return item;
            }
        }

        return null;
    }

    private static Dictionary<string, string> EnsureCustomData(ItemDrop.ItemData item)
    {
        if (item.m_customData == null)
        {
            item.m_customData = new Dictionary<string, string>();
        }

        return item.m_customData;
    }
}
