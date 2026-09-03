using HarmonyLib;
using UnityEngine;

namespace FletchersForge.Patches;

internal static class QuiverTombstoneHarmonyIds
{
    internal const string AzuEpi = "Azumatt.AzuExtendedPlayerInventory";
}

/// Unequip + unpack packed quiver stacks before vanilla copies the bag.
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
internal static class TombStoneAwakeQuiverDumpPatch
{
    [HarmonyPostfix]
    [HarmonyAfter(new string[] { QuiverTombstoneHarmonyIds.AzuEpi })]
    private static void Postfix(TombStone __instance)
    {
        if (__instance == null || QuiverTombstoneDump.PendingExtraRows <= 0)
        {
            return;
        }

        Container container = __instance.GetComponent<Container>();
        QuiverTombstoneDump.ApplyPendingExtraHeight(container, container != null ? container.GetInventory() : null);
    }
}

[HarmonyPatch(typeof(Inventory), "MoveInventoryToGrave")]
internal static class MoveInventoryToGraveQuiverDumpPatch
{
    [HarmonyPrefix]
    [HarmonyAfter(new string[] { QuiverTombstoneHarmonyIds.AzuEpi })]
    private static void Prefix(Inventory __instance, Inventory original)
    {
        if (!IsLocalPlayerBag(original))
        {
            return;
        }

        // AzuEPI forced both to GetFullHeight; vanilla then copies original -> grave.
        QuiverTombstoneDump.BumpHeightsForPending(__instance, original);
    }

    [HarmonyPostfix]
    [HarmonyAfter(new string[] { QuiverTombstoneHarmonyIds.AzuEpi })]
    private static void Postfix(Inventory __instance, Inventory original)
    {
        if (!IsLocalPlayerBag(original))
        {
            return;
        }

        int extra = QuiverTombstoneDump.PendingExtraRows;
        QuiverTombstoneDump.FlushPendingIntoGrave(__instance);

        TombStone tomb = FindTombStoneForInventory(__instance);
        if (tomb != null)
        {
            Container container = tomb.GetComponent<Container>();
            if (container != null)
            {
                container.m_height = QuiverTombstoneDump.GetHeight(__instance);
            }

            QuiverTombstoneDump.WriteHeightZdo(tomb, QuiverTombstoneDump.GetHeight(__instance), extra);
        }

        QuiverTombstoneDump.RestorePlayerHeightAfterDump(original, extra);
        QuiverTombstoneDump.ClearPending();
    }

    private static bool IsLocalPlayerBag(Inventory original)
    {
        return original != null &&
               Player.m_localPlayer != null &&
               original == Player.m_localPlayer.GetInventory();
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

        int absolute = QuiverTombstoneDump.ReadAbsoluteHeightZdo(__instance);
        if (absolute <= 0)
        {
            int extra = QuiverTombstoneDump.ReadExtraRowsZdo(__instance);
            if (extra <= 0)
            {
                return;
            }

            absolute = ___m_container.m_height + extra;
        }

        if (___m_container.m_height < absolute)
        {
            ___m_container.m_height = absolute;
        }

        Inventory inv = ___m_container.GetInventory();
        if (inv != null && QuiverTombstoneDump.GetHeight(inv) < absolute)
        {
            QuiverTombstoneDump.SetHeight(inv, absolute);
        }

        // Force Container.Load so items in extra rows are restored after the height bump.
        Traverse.Create(___m_container).Field("m_lastRevision").SetValue(0u);
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
            QuiverTombstoneDump.TryRepackPlayerInventory(player);
        }
    }
}

[HarmonyPatch(typeof(Inventory), "AddItem", typeof(ItemDrop.ItemData))]
internal static class InventoryAddItemQuiverRepackPatch
{
    [HarmonyPostfix]
    private static void Postfix(Inventory __instance, ItemDrop.ItemData item, bool __result)
    {
        TryRepack(__instance, item, __result);
    }

    internal static void TryRepack(Inventory inventory, ItemDrop.ItemData item, bool added)
    {
        if (!added || item?.m_customData == null ||
            !item.m_customData.ContainsKey(QuiverTombstoneDump.DumpIdKey))
        {
            return;
        }

        Player player = Player.m_localPlayer;
        if (player != null && inventory == player.GetInventory())
        {
            QuiverTombstoneDump.TryRepackPlayerInventory(player);
        }
    }
}

[HarmonyPatch(typeof(Inventory), "AddItem", typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int))]
internal static class InventoryAddItemXYQuiverRepackPatch
{
    [HarmonyPostfix]
    private static void Postfix(Inventory __instance, ItemDrop.ItemData item, bool __result)
    {
        InventoryAddItemQuiverRepackPatch.TryRepack(__instance, item, __result);
    }
}
