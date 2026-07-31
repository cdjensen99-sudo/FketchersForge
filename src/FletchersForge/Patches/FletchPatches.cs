using HarmonyLib;
using UnityEngine;

namespace FletchersForge.Patches;

[HarmonyPatch(typeof(Player), "Update")]
internal static class PlayerUpdateFletchPatch
{
    private static bool knifeWasInHand;

    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if (__instance == null || !__instance.IsOwner())
        {
            return;
        }

        bool knifeInHand = FletchersKnifeHelper.IsKnifeInHand(__instance);
        if (knifeInHand && !knifeWasInHand)
        {
            FletchUiService.Open(__instance);
        }
        else if (!knifeInHand && knifeWasInHand)
        {
            FletchUiService.Close();
        }

        knifeWasInHand = knifeInHand;

        if (!FletchUiService.IsFletchContainerOpen)
        {
            return;
        }

        if (ModConfig.ReforgeKey.Value != KeyCode.None && ZInput.GetKeyDown(ModConfig.ReforgeKey.Value))
        {
            FletchUiService.TryReforge(__instance);
        }

        if (ModConfig.SplitKey.Value != KeyCode.None && ZInput.GetKeyDown(ModConfig.SplitKey.Value))
        {
            FletchUiService.TrySplit(__instance);
        }
    }
}

[HarmonyPatch(typeof(InventoryGui), "Update")]
internal static class InventoryGuiUpdateFletchPatch
{
    [HarmonyPostfix]
    private static void Postfix(InventoryGui __instance)
    {
        bool benchOpen = FletchUiService.IsBenchUiOpen;
        FletchBenchButtonUi.Update(__instance, benchOpen);

        object containerWeight = Traverse.Create(__instance).Field("m_containerWeight").GetValue();
        if (containerWeight != null)
        {
            GameObject weightObject = Traverse.Create(containerWeight).Property<GameObject>("gameObject").Value;
            if (weightObject != null)
            {
                weightObject.SetActive(!benchOpen && __instance.IsContainerOpen());
            }
        }

        if (!benchOpen || __instance.m_container == null)
        {
            return;
        }

        HideContainerActionButtons(__instance.m_container, benchOpen);
    }

    private static void HideContainerActionButtons(RectTransform containerRoot, bool benchOpen)
    {
        foreach (Transform child in containerRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child == null)
            {
                continue;
            }

            string name = child.name.ToLowerInvariant();
            if (benchOpen && (name.Contains("takeall") || name.Contains("stackall")))
            {
                continue;
            }

            if (name.Contains("reclaim") ||
                name.Contains("takeall") ||
                name.Contains("stackall") ||
                name.Contains("place stacks") ||
                name.Contains("placestacks") ||
                name.Contains("quickstack") ||
                name.Contains("sortcontainer") ||
                name.Contains("restock"))
            {
                child.gameObject.SetActive(false);
            }
        }
    }
}

[HarmonyPatch(typeof(InventoryGui), "OnTakeAll")]
internal static class InventoryGuiOnTakeAllFletchPatch
{
    [HarmonyPrefix]
    private static bool Prefix()
    {
        if (!FletchUiService.IsBenchUiOpen)
        {
            return true;
        }

        Player local = Player.m_localPlayer;
        if (local != null)
        {
            FletchUiService.TryReforge(local);
        }

        return false;
    }
}

[HarmonyPatch(typeof(InventoryGui), "OnStackAll")]
internal static class InventoryGuiOnStackAllFletchPatch
{
    [HarmonyPrefix]
    private static bool Prefix()
    {
        if (!FletchUiService.IsBenchUiOpen)
        {
            return true;
        }

        Player local = Player.m_localPlayer;
        if (local != null)
        {
            FletchUiService.TrySplit(local);
        }

        return false;
    }
}

[HarmonyPatch(typeof(InventoryGui), "Hide")]
internal static class InventoryGuiHideFletchPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        if (FletchUiService.IsBenchUiOpen)
        {
            FletchUiService.NotifyGuiClosed();
        }
    }
}

[HarmonyPatch(typeof(InventoryGui), "IsContainerOpen")]
internal static class InventoryGuiIsContainerOpenFletchPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref bool __result)
    {
        if (FletchUiService.IsBenchUiOpen)
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(InventoryGui), "UpdateContainer")]
internal static class InventoryGuiUpdateContainerFletchPatch
{
    [HarmonyPrefix]
    private static bool Prefix(InventoryGui __instance, Player player)
    {
        if (!FletchUiService.IsBenchUiOpen || FletchBenchInventory.Inventory == null)
        {
            return true;
        }

        if (!InventoryGui.IsVisible())
        {
            return true;
        }

        Inventory benchInventory = FletchBenchInventory.Inventory;
        __instance.m_container.gameObject.SetActive(true);
        __instance.ContainerGrid.UpdateInventory(
            benchInventory,
            null,
            InventoryGuiAccess.GetDragItem(__instance));
        InventoryGuiAccess.SetContainerName(
            __instance,
            Localization.instance.Localize(benchInventory.GetName()));

        if (InventoryGuiAccess.GetFirstContainerUpdate(__instance))
        {
            __instance.ContainerGrid.ResetView();
            InventoryGuiAccess.SetFirstContainerUpdate(__instance, false);
            InventoryGuiAccess.ResetContainerHold(__instance);
        }

        return false;
    }
}

[HarmonyPatch(typeof(Inventory), "AddItem", typeof(ItemDrop.ItemData), typeof(Vector2i))]
internal static class InventoryAddItemGridFletchPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, Vector2i pos, ref bool __result)
    {
        if (!FletchUiService.IsFletchInventory(__instance) || item?.m_dropPrefab == null)
        {
            return true;
        }

        if (!FletchSlotRules.CanAccept(pos.x, pos.y, item.m_dropPrefab.name))
        {
            __result = false;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(Inventory), "CanAddItem", typeof(ItemDrop.ItemData), typeof(int))]
internal static class InventoryCanAddItemFletchPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
    {
        if (!FletchUiService.IsFletchInventory(__instance) || item?.m_dropPrefab == null)
        {
            return true;
        }

        bool allowed =
            FletchSlotRules.CanAccept(0, 0, item.m_dropPrefab.name) ||
            FletchSlotRules.CanAccept(1, 0, item.m_dropPrefab.name);

        if (!allowed)
        {
            __result = false;
            return false;
        }

        return true;
    }
}
