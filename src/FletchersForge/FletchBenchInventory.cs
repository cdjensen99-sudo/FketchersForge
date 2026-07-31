using Jotunn.Managers;
using UnityEngine;

namespace FletchersForge;

/// Two-slot bench storage with no world Container — only used by InventoryGui.
internal static class FletchBenchInventory
{
    private static Inventory inventory;

    internal static Inventory Inventory => inventory;

    internal static void Initialize()
    {
        if (inventory != null)
        {
            return;
        }

        Sprite background = null;
        GameObject chestPrefab = PrefabManager.Instance.GetPrefab("chest");
        Container vanillaContainer = chestPrefab != null ? chestPrefab.GetComponent<Container>() : null;
        if (vanillaContainer != null)
        {
            background = vanillaContainer.m_bkg;
        }

        inventory = new Inventory("$FF_FletchContainer", background, 2, 1);
        FletchersForgePlugin.Log?.LogInfo("Initialized virtual Fletcher bench inventory.");
    }

    internal static void ClearSlots()
    {
        if (inventory != null)
        {
            inventory.RemoveAll();
        }
    }
}
