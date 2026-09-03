using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace FletchersForge;

/// Death: unpack quiver contents into the player bag / tombstone extra rows.
/// Loot: put tagged stacks back onto the matching quiver, then Fletcher-equip.
internal static class QuiverTombstoneDump
{
    internal const string QuiverIdKey = "FF_QuiverId";
    internal const string DumpIdKey = "FF_QuiverDumpId";
    internal const string ExtraRowsZdoKey = "FF_TombExtraRows";
    internal const string AbsoluteHeightZdoKey = "FF_TombHeight";

    private static readonly int ExtraRowsZdoHash = ExtraRowsZdoKey.GetStableHashCode();
    private static readonly int AbsoluteHeightZdoHash = AbsoluteHeightZdoKey.GetStableHashCode();

    private static readonly List<ItemDrop.ItemData> PendingStacks = new List<ItemDrop.ItemData>();

    /// Extra tombstone rows needed for PendingStacks (set before Instantiate/Awake).
    internal static int PendingExtraRows { get; private set; }

    private static bool repacking;
    private static bool deathDumpActive;

    internal static void ClearPending()
    {
        PendingStacks.Clear();
        PendingExtraRows = 0;
        deathDumpActive = false;
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

    /// Unequip, unpack into empty player cells, queue overflow, wipe packed data.
    internal static void PreparePlayerDeathDump(Player player)
    {
        ClearPending();
        if (player == null)
        {
            return;
        }

        deathDumpActive = true;
        Inventory bag = player.GetInventory();
        if (bag == null)
        {
            deathDumpActive = false;
            return;
        }

        QuiverInventory.UnequipAllForDeath(player);

        List<ItemDrop.ItemData> quivers = new List<ItemDrop.ItemData>();
        foreach (ItemDrop.ItemData item in bag.GetAllItems())
        {
            if (QuiverInventory.IsQuiverItem(item))
            {
                quivers.Add(item);
            }
        }

        Inventory scratch = new Inventory("FF_QuiverDeathScratch", null, ModConstants.QuiverSlotCount, 1);
        foreach (ItemDrop.ItemData quiver in quivers)
        {
            string quiverId = EnsureQuiverId(quiver);
            Dictionary<string, string> quiverData = EnsureCustomData(quiver);
            if (!quiverData.TryGetValue(QuiverInventory.ContentsKey, out string packed) ||
                string.IsNullOrEmpty(packed))
            {
                continue;
            }

            scratch.RemoveAll();
            try
            {
                scratch.Load(new ZPackage(Convert.FromBase64String(packed)));
            }
            catch (Exception ex)
            {
                FletchersForgePlugin.Log?.LogWarning($"Death dump: failed to load quiver contents: {ex.Message}");
                continue;
            }

            List<ItemDrop.ItemData> contents = new List<ItemDrop.ItemData>(scratch.GetAllItems());
            foreach (ItemDrop.ItemData stack in contents)
            {
                if (stack == null)
                {
                    continue;
                }

                scratch.RemoveItem(stack);
                TagDumpStack(stack, quiverId);
                if (!bag.AddItem(stack))
                {
                    PendingStacks.Add(stack);
                }
            }

            quiverData.Remove(QuiverInventory.ContentsKey);
            quiverData[QuiverInventory.SelectedSlotKey] = "0";
        }

        int width = Mathf.Max(1, bag.GetWidth());
        PendingExtraRows = PendingStacks.Count <= 0
            ? 0
            : Mathf.CeilToInt(PendingStacks.Count / (float)width);

        QuiverBackVisual.SyncOwnerZdo(player);
        FletchersForgePlugin.Log?.LogInfo(
            $"Death dump: {quivers.Count} quiver(s), {PendingStacks.Count} overflow stack(s), +{PendingExtraRows} tombstone row(s).");
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

    /// Vanilla copies player height onto the grave. Bump both after AzuEPI's GetFullHeight reset.
    internal static void BumpHeightsForPending(Inventory grave, Inventory playerBag)
    {
        if (PendingExtraRows <= 0)
        {
            return;
        }

        if (grave != null)
        {
            SetHeight(grave, GetHeight(grave) + PendingExtraRows);
        }

        if (playerBag != null)
        {
            SetHeight(playerBag, GetHeight(playerBag) + PendingExtraRows);
        }
    }

    internal static void ApplyPendingExtraHeight(Container container, Inventory inventory)
    {
        if (PendingExtraRows <= 0 || container == null)
        {
            return;
        }

        container.m_height += PendingExtraRows;
        if (inventory != null)
        {
            SetHeight(inventory, GetHeight(inventory) + PendingExtraRows);
        }
    }

    internal static void WriteHeightZdo(TombStone tomb, int absoluteHeight, int extraRows)
    {
        if (tomb == null || absoluteHeight <= 0)
        {
            return;
        }

        ZNetView nview = tomb.GetComponent<ZNetView>();
        ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
        if (zdo == null || !nview.IsOwner())
        {
            return;
        }

        zdo.Set(AbsoluteHeightZdoHash, absoluteHeight);
        if (extraRows > 0)
        {
            zdo.Set(ExtraRowsZdoHash, extraRows);
        }
    }

    internal static int ReadAbsoluteHeightZdo(TombStone tomb)
    {
        if (tomb == null)
        {
            return 0;
        }

        ZNetView nview = tomb.GetComponent<ZNetView>();
        ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
        if (zdo == null)
        {
            return 0;
        }

        return Mathf.Max(0, zdo.GetInt(AbsoluteHeightZdoHash, 0));
    }

    internal static int ReadExtraRowsZdo(TombStone tomb)
    {
        if (tomb == null)
        {
            return 0;
        }

        ZNetView nview = tomb.GetComponent<ZNetView>();
        ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
        if (zdo == null)
        {
            return 0;
        }

        return Mathf.Max(0, zdo.GetInt(ExtraRowsZdoHash, 0));
    }

    /// After the grave list is filled, add overflow stacks into the extra rows.
    internal static void FlushPendingIntoGrave(Inventory grave)
    {
        if (grave == null || PendingStacks.Count == 0)
        {
            return;
        }

        List<ItemDrop.ItemData> still = new List<ItemDrop.ItemData>();
        foreach (ItemDrop.ItemData stack in PendingStacks)
        {
            if (stack == null)
            {
                continue;
            }

            if (!grave.AddItem(stack))
            {
                still.Add(stack);
                FletchersForgePlugin.Log?.LogWarning(
                    $"Death dump: could not place '{stack.m_shared?.m_name}' in tombstone after height bump.");
            }
        }

        PendingStacks.Clear();
        PendingStacks.AddRange(still);
    }

    internal static void RestorePlayerHeightAfterDump(Inventory playerBag, int extraRows)
    {
        if (playerBag == null || extraRows <= 0)
        {
            return;
        }

        SetHeight(playerBag, Mathf.Max(4, GetHeight(playerBag) - extraRows));
    }

    internal static void TryRepackPlayerInventory(Player player)
    {
        if (repacking || deathDumpActive || player == null)
        {
            return;
        }

        Inventory bag = player.GetInventory();
        if (bag == null)
        {
            return;
        }

        List<ItemDrop.ItemData> tagged = new List<ItemDrop.ItemData>();
        foreach (ItemDrop.ItemData item in bag.GetAllItems())
        {
            if (item?.m_customData != null &&
                item.m_customData.TryGetValue(DumpIdKey, out string dumpId) &&
                !string.IsNullOrEmpty(dumpId))
            {
                tagged.Add(item);
            }
        }

        if (tagged.Count == 0)
        {
            return;
        }

        repacking = true;
        try
        {
            Dictionary<string, List<ItemDrop.ItemData>> byId = new Dictionary<string, List<ItemDrop.ItemData>>();
            foreach (ItemDrop.ItemData item in tagged)
            {
                string id = item.m_customData[DumpIdKey];
                if (!byId.TryGetValue(id, out List<ItemDrop.ItemData> list))
                {
                    list = new List<ItemDrop.ItemData>();
                    byId[id] = list;
                }

                list.Add(item);
            }

            ItemDrop.ItemData lastRepacked = null;
            foreach (KeyValuePair<string, List<ItemDrop.ItemData>> pair in byId)
            {
                ItemDrop.ItemData quiver = FindQuiverById(bag, pair.Key);
                if (quiver == null)
                {
                    continue;
                }

                Inventory packed = new Inventory("FF_QuiverRepack", null, ModConstants.QuiverSlotCount, 1);
                Dictionary<string, string> quiverData = EnsureCustomData(quiver);
                if (quiverData.TryGetValue(QuiverInventory.ContentsKey, out string existing) &&
                    !string.IsNullOrEmpty(existing))
                {
                    try
                    {
                        packed.Load(new ZPackage(Convert.FromBase64String(existing)));
                    }
                    catch (Exception ex)
                    {
                        FletchersForgePlugin.Log?.LogWarning($"Repack: failed to load existing contents: {ex.Message}");
                        packed.RemoveAll();
                    }
                }

                bool any = false;
                foreach (ItemDrop.ItemData stack in pair.Value)
                {
                    bag.RemoveItem(stack);
                    ClearDumpTag(stack);
                    if (packed.AddItem(stack))
                    {
                        any = true;
                    }
                    else
                    {
                        bag.AddItem(stack);
                        FletchersForgePlugin.Log?.LogWarning(
                            $"Repack: quiver {pair.Key} full; left '{stack.m_shared?.m_name}' in backpack.");
                    }
                }

                if (any)
                {
                    ZPackage pkg = new ZPackage();
                    packed.Save(pkg);
                    quiverData[QuiverInventory.ContentsKey] = Convert.ToBase64String(pkg.GetArray());
                    lastRepacked = quiver;
                }
            }

            if (lastRepacked != null)
            {
                QuiverInventory.EquipOnly(player, lastRepacked);
            }
        }
        finally
        {
            repacking = false;
        }
    }

    private static ItemDrop.ItemData FindQuiverById(Inventory bag, string id)
    {
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

    private static void TagDumpStack(ItemDrop.ItemData stack, string quiverId)
    {
        Dictionary<string, string> data = EnsureCustomData(stack);
        data[DumpIdKey] = quiverId;
    }

    private static void ClearDumpTag(ItemDrop.ItemData stack)
    {
        if (stack?.m_customData == null)
        {
            return;
        }

        stack.m_customData.Remove(DumpIdKey);
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
