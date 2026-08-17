using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace FletchersForge.Patches;

/// Runs legacy cleanup after the world and ZDOs are loaded (local player spawn).
[HarmonyPatch(typeof(Player), "OnSpawned")]
internal static class PlayerSpawnCleanupPatch
{
    private static bool spawnCleanupDone;

    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if (__instance == null || !__instance.IsOwner() || spawnCleanupDone)
        {
            return;
        }

        spawnCleanupDone = true;
        RestoreVanillaInventoryHeight(__instance);
        QuiverInventory.MigrateLegacyPlayerData(__instance);
        // Backup if ZDOMan.Load already completed before player spawn.
        FletchLegacyCleanup.Run();
    }

    private static bool IsAzuEpiLoaded()
    {
        foreach (var plugin in BepInEx.Bootstrap.Chainloader.PluginInfos)
        {
            if (plugin.Key.IndexOf("AzuExtendedPlayerInventory", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// 0.1.38–0.1.39 added a 5th inventory row before the quiver item existed.
    private static void RestoreVanillaInventoryHeight(Player player)
    {
        if (IsAzuEpiLoaded())
        {
            return;
        }

        Inventory inventory = player.GetInventory();
        if (inventory == null || inventory.GetWidth() != 8)
        {
            return;
        }

        int height = Traverse.Create(inventory).Field<int>("m_height").Value;
        if (height != 5)
        {
            return;
        }

        var displaced = new List<ItemDrop.ItemData>();
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (item != null && item.m_gridPos.y >= 4)
            {
                displaced.Add(item);
            }
        }

        foreach (ItemDrop.ItemData item in displaced)
        {
            inventory.RemoveItem(item);
        }

        Traverse.Create(inventory).Field("m_height").SetValue(4);

        foreach (ItemDrop.ItemData item in displaced)
        {
            if (inventory.AddItem(item))
            {
                continue;
            }

            ItemDrop.DropItem(item, 0, player.transform.position + Vector3.up, Quaternion.identity);
        }

        FletchersForgePlugin.Log?.LogInfo("Removed premature quiver inventory row.");
    }
}
