using UnityEngine;

namespace FletchersForge;

internal static class FletchOperations
{
    internal static bool TryReforge(Player player, Inventory inv, out string message)
    {
        message = string.Empty;
        if (player == null || inv == null)
        {
            message = "No workbench.";
            return false;
        }
        ItemDrop.ItemData slot0 = inv.GetItemAt(0, 0);
        ItemDrop.ItemData slot1 = inv.GetItemAt(1, 0);

        if (slot0 == null)
        {
            message = "Place a shaft or arrow in the first slot.";
            return false;
        }

        string prefab0 = slot0.m_dropPrefab != null ? slot0.m_dropPrefab.name : string.Empty;

        if (ArrowAssemblyRegistry.IsArrowPrefab(prefab0))
        {
            return TryRehead(player, inv, slot0, slot1, out message);
        }

        if (ArrowAssemblyRegistry.IsShaftPrefab(prefab0))
        {
            return TryAssemble(player, inv, slot0, slot1, out message);
        }

        message = "Invalid item in first slot.";
        return false;
    }

    internal static bool TrySplit(Player player, Inventory inv, out string message)
    {
        message = string.Empty;
        if (player == null || inv == null)
        {
            message = "No workbench.";
            return false;
        }
        ItemDrop.ItemData slot0 = inv.GetItemAt(0, 0);
        ItemDrop.ItemData slot1 = inv.GetItemAt(1, 0);

        if (slot0 == null)
        {
            message = "Place arrows in the first slot.";
            return false;
        }

        if (slot1 != null)
        {
            message = "Clear the head slot before splitting.";
            return false;
        }

        string arrowPrefab = slot0.m_dropPrefab != null ? slot0.m_dropPrefab.name : string.Empty;
        if (!ArrowAssemblyRegistry.TryGetParts(arrowPrefab, out string shaftPrefab, out string headPrefab))
        {
            message = "That item cannot be split.";
            return false;
        }

        int batch = Mathf.Min(ModConstants.BatchSize, slot0.m_stack);
        if (!TryGiveComponents(player, shaftPrefab, headPrefab, batch))
        {
            message = "Inventory full.";
            return false;
        }

        slot0.m_stack -= batch;
        if (slot0.m_stack <= 0)
        {
            inv.RemoveItem(slot0);
        }

        message = $"Split {batch} arrows.";
        return true;
    }

    private static bool TryAssemble(
        Player player,
        Inventory inv,
        ItemDrop.ItemData shaftItem,
        ItemDrop.ItemData headItem,
        out string message)
    {
        message = string.Empty;
        string shaftPrefab = shaftItem.m_dropPrefab.name;
        string headPrefab = headItem?.m_dropPrefab != null ? headItem.m_dropPrefab.name : null;

        if (!ArrowAssemblyRegistry.TryGetArrow(shaftPrefab, headPrefab, out string arrowPrefab))
        {
            if (headItem == null)
            {
                message = "Place an arrowhead in the second slot.";
            }
            else
            {
                message = "Those parts do not match.";
            }

            return false;
        }

        int batch = Mathf.Min(ModConstants.BatchSize, shaftItem.m_stack);
        if (headItem != null)
        {
            batch = Mathf.Min(batch, headItem.m_stack);
        }

        if (!GiveArrows(player, arrowPrefab, batch))
        {
            message = "Inventory full.";
            return false;
        }

        shaftItem.m_stack -= batch;
        if (shaftItem.m_stack <= 0)
        {
            inv.RemoveItem(shaftItem);
        }

        if (headItem != null)
        {
            headItem.m_stack -= batch;
            if (headItem.m_stack <= 0)
            {
                inv.RemoveItem(headItem);
            }
        }

        message = $"Reforged {batch} arrows.";
        return true;
    }

    private static bool TryRehead(
        Player player,
        Inventory inv,
        ItemDrop.ItemData arrowItem,
        ItemDrop.ItemData newHeadItem,
        out string message)
    {
        message = string.Empty;
        if (newHeadItem == null)
        {
            message = "Place a new arrowhead in the second slot.";
            return false;
        }

        string arrowPrefab = arrowItem.m_dropPrefab.name;
        if (!ArrowAssemblyRegistry.TryGetParts(arrowPrefab, out string oldShaft, out string oldHead))
        {
            message = "That arrow cannot be reforged.";
            return false;
        }

        string newHeadPrefab = newHeadItem.m_dropPrefab.name;
        if (!ArrowAssemblyRegistry.TryGetArrow(oldShaft, newHeadPrefab, out string resultArrow))
        {
            message = "That head does not fit this arrow.";
            return false;
        }

        int batch = Mathf.Min(ModConstants.BatchSize, arrowItem.m_stack, newHeadItem.m_stack);
        if (!GiveArrows(player, resultArrow, batch))
        {
            message = "Inventory full.";
            return false;
        }

        if (!string.IsNullOrEmpty(oldHead) && !GiveComponents(player, oldHead, batch))
        {
            message = "Inventory full.";
            return false;
        }

        arrowItem.m_stack -= batch;
        newHeadItem.m_stack -= batch;

        if (arrowItem.m_stack <= 0)
        {
            inv.RemoveItem(arrowItem);
        }

        if (newHeadItem.m_stack <= 0)
        {
            inv.RemoveItem(newHeadItem);
        }

        message = $"Reforged {batch} arrows.";
        return true;
    }

    private static bool TryGiveComponents(Player player, string shaftPrefab, string headPrefab, int count)
    {
        if (!GiveComponents(player, shaftPrefab, count))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(headPrefab) && !GiveComponents(player, headPrefab, count))
        {
            return false;
        }

        return true;
    }

    private static bool GiveComponents(Player player, string prefabName, int count)
    {
        GameObject prefab = ObjectDB.instance.GetItemPrefab(prefabName);
        if (prefab == null)
        {
            return false;
        }

        return player.GetInventory().AddItem(prefab, count);
    }

    private static bool GiveArrows(Player player, string arrowPrefabName, int count)
    {
        GameObject prefab = ObjectDB.instance.GetItemPrefab(arrowPrefabName);
        if (prefab == null)
        {
            FletchersForgePlugin.Log?.LogError($"Arrow prefab missing: {arrowPrefabName}");
            return false;
        }

        return player.GetInventory().AddItem(prefab, count);
    }
}
