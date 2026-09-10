using HarmonyLib;
using UnityEngine;

namespace FletchersForge.Patches;

internal static class QuiverTombstoneHarmonyIds
{
    internal const string AzuEpi = "Azumatt.AzuExtendedPlayerInventory";
}

/// Leave Fletcher ammo in reserved bag cells; strip equip; keep height for grave copy.
[HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
internal static class PlayerCreateTombStoneQuiverDumpPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Prefix(Player __instance)
    {
        if (__instance != null && __instance.IsOwner())
        {
            QuiverTombstoneDump.PreparePlayerDeathDump(__instance);
        }
    }
}

[HarmonyPatch(typeof(TombStone), "Awake")]
internal static class TombStoneAwakeQuiverHeightPatch
{
    [HarmonyPostfix]
    [HarmonyAfter(new string[] { QuiverTombstoneHarmonyIds.AzuEpi })]
    private static void Postfix(TombStone __instance)
    {
        if (__instance == null)
        {
            return;
        }

        Container container = __instance.GetComponent<Container>();
        Player local = Player.m_localPlayer;
        if (local != null)
        {
            QuiverBagBridge.ApplyTombstoneHeightForPlayer(local, container);
        }
        else
        {
            QuiverBagBridge.ApplyTombstoneHeight(container);
        }
    }
}

[HarmonyPatch(typeof(Inventory), "MoveInventoryToGrave")]
internal static class MoveInventoryToGraveQuiverDumpPatch
{
    [HarmonyPrefix]
    [HarmonyAfter(new string[] { QuiverTombstoneHarmonyIds.AzuEpi })]
    private static void Prefix(Inventory __instance, Inventory original)
    {
        Player player = Player.m_localPlayer;
        if (player == null || original != player.GetInventory())
        {
            return;
        }

        // Keep player/grave height matched when quiver bag rows are active.
        if (QuiverBagBridge.ExtraRowsActive > 0)
        {
            int target = QuiverBagBridge.ReservedRowStart + QuiverBagBridge.ExtraRowsActive;
            QuiverBagBridge.SetHeight(original, Mathf.Max(QuiverBagBridge.GetHeight(original), target));
            QuiverBagBridge.SetHeight(__instance, Mathf.Max(QuiverBagBridge.GetHeight(__instance), target));
        }
    }

    [HarmonyPostfix]
    [HarmonyAfter(new string[] { QuiverTombstoneHarmonyIds.AzuEpi })]
    private static void Postfix(Inventory __instance, Inventory original)
    {
        QuiverTombstoneDump.AfterMoveInventoryToGrave(original);

        TombStone tomb = FindTombStoneForInventory(__instance);
        if (tomb != null)
        {
            Container container = tomb.GetComponent<Container>();
            if (container != null)
            {
                container.m_height = QuiverTombstoneDump.GetHeight(__instance);
            }
        }
    }

    private static TombStone FindTombStoneForInventory(Inventory inventory)
    {
        TombStone[] tombs = Object.FindObjectsByType<TombStone>(FindObjectsSortMode.None);
        foreach (TombStone tomb in tombs)
        {
            Container container = tomb != null ? tomb.GetComponent<Container>() : null;
            if (container != null && container.GetInventory() == inventory)
            {
                return tomb;
            }
        }

        return null;
    }
}

[HarmonyPatch(typeof(TombStone), "Interact")]
internal static class TombStoneInteractQuiverDumpPatch
{
    [HarmonyPrefix]
    [HarmonyAfter(new string[] { QuiverTombstoneHarmonyIds.AzuEpi })]
    private static void Prefix(TombStone __instance, bool hold, Container ___m_container)
    {
        if (hold || __instance == null || ___m_container == null)
        {
            return;
        }

        QuiverTombstoneDump.SanitizeOversizedTombstone(__instance, ___m_container);
    }
}

[HarmonyPatch(typeof(TombStone), "OnTakeAllSuccess")]
internal static class TombStoneOnTakeAllSuccessQuiverPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        QuiverTombstoneDump.RequestDeferredRestore();
    }
}

[HarmonyPatch(typeof(InventoryGui), "OnTakeAll")]
internal static class InventoryGuiOnTakeAllQuiverLogPatch
{
    [HarmonyPrefix]
    private static void Prefix(InventoryGui __instance)
    {
        Container container = Traverse.Create(__instance).Field<Container>("m_currentContainer").Value;
        Inventory inv = container != null ? container.GetInventory() : null;
        int height = inv != null ? QuiverTombstoneDump.GetHeight(inv) : -1;
        int count = inv != null ? inv.NrOfItems() : -1;
        FletchersForgePlugin.Log?.LogInfo(
            $"Take All pressed: containerHeight={height}, items={count}.");
    }

    [HarmonyPostfix]
    private static void Postfix()
    {
        FletchersForgePlugin.Log?.LogInfo("Take All MoveAll finished.");
        QuiverTombstoneDump.RequestDeferredRestore();
    }
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveAll))]
internal static class InventoryMoveAllQuiverRepackPatch
{
    [HarmonyPostfix]
    private static void Postfix(Inventory __instance)
    {
        Player player = Player.m_localPlayer;
        if (player != null && __instance == player.GetInventory())
        {
            QuiverTombstoneDump.RequestDeferredRestore();
        }
    }
}

[HarmonyPatch(typeof(Inventory), "AddItem", typeof(ItemDrop.ItemData))]
internal static class InventoryAddItemQuiverRepackPatch
{
    [HarmonyPostfix]
    private static void Postfix(Inventory __instance, ItemDrop.ItemData item, bool __result)
    {
        TryQueueRestore(__instance, item, __result);
    }

    internal static void TryQueueRestore(Inventory inventory, ItemDrop.ItemData item, bool added)
    {
        if (!added || item == null)
        {
            return;
        }

        Player player = Player.m_localPlayer;
        if (player == null || inventory != player.GetInventory())
        {
            return;
        }

        bool tagged = item.m_customData != null && item.m_customData.ContainsKey(QuiverTombstoneDump.DumpIdKey);
        bool restoreQuiver = QuiverInventory.IsQuiverItem(item) && QuiverTombstoneDump.ShouldRestoreEquipFor(item);
        if (tagged || restoreQuiver)
        {
            QuiverTombstoneDump.RequestDeferredRestore();
        }
    }
}

[HarmonyPatch(typeof(Inventory), "AddItem", typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool))]
internal static class InventoryAddItemXYQuiverRepackPatch
{
    [HarmonyPostfix]
    private static void Postfix(Inventory __instance, ItemDrop.ItemData item, bool __result)
    {
        InventoryAddItemQuiverRepackPatch.TryQueueRestore(__instance, item, __result);
    }
}
