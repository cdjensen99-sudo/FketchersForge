using HarmonyLib;
using UnityEngine;

namespace FletchersForge.Patches;

[HarmonyPatch(typeof(Player), nameof(Player.Save))]
internal static class PlayerSaveQuiverPatch
{
    [HarmonyPrefix]
    private static void Prefix(Player __instance)
    {
        if (__instance != null && __instance.IsOwner())
        {
            QuiverInventory.SyncFromPlayer(__instance);
            QuiverInventory.SaveBound();
        }
    }
}

/// Quiver equip is RMB (FF_QuiverEquipped), not vanilla EquipItem. Skip AutoEquip / extra-slot EquipItem.
[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
internal static class HumanoidEquipItemSkipQuiverPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(ItemDrop.ItemData item, ref bool __result)
    {
        if (!QuiverInventory.IsQuiverItem(item))
        {
            return true;
        }

        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.UseHotbarItem))]
internal static class PlayerUseHotbarItemQuiverPatch
{
    [HarmonyPrefix]
    private static bool Prefix()
    {
        return !QuiverHud.IsSelectModifierHeld();
    }
}

[HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
internal static class GameCameraMouseCaptureQuiverPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        if (!QuiverHud.IsCursorMode())
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.SetMouseLook))]
internal static class PlayerSetMouseLookQuiverPatch
{
    [HarmonyPrefix]
    private static bool Prefix()
    {
        return !QuiverHud.IsCursorMode();
    }
}

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
internal static class HumanoidStartAttackQuiverPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Humanoid __instance)
    {
        return __instance != Player.m_localPlayer || !QuiverHud.IsCursorMode();
    }
}

[HarmonyPatch(typeof(Player), "PlayerAttackInput")]
internal static class PlayerAttackInputQuiverPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Player __instance)
    {
        if (__instance != Player.m_localPlayer || !QuiverHud.IsCursorMode())
        {
            return true;
        }

        QuiverHud.CancelPlayerCombatInput(__instance);
        return false;
    }
}

[HarmonyPatch(typeof(InventoryGui), "Update")]
internal static class InventoryGuiUpdateQuiverDockPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        QuiverHud.AfterInventoryGuiUpdate();
    }
}

/// Right-click the quiver in the backpack to equip / unequip (does not use the cape slot).
[HarmonyPatch(typeof(InventoryGui), "OnRightClickItem")]
internal static class InventoryGuiRightClickQuiverEquipPatch
{
    [HarmonyPrefix]
    private static bool Prefix(InventoryGrid grid, ItemDrop.ItemData item)
    {
        Player player = Player.m_localPlayer;
        if (player == null || item == null || !QuiverInventory.IsQuiverItem(item))
        {
            return true;
        }

        Inventory playerInventory = player.GetInventory();
        if (playerInventory == null || grid == null || grid.GetInventory() != playerInventory)
        {
            return true;
        }

        QuiverInventory.ToggleEquip(player, item);
        return false;
    }
}

/// Vanilla cancels any drag whose source is not the player inventory unless a chest is open.
/// Quiver stacks live in a separate Inventory, so pickup was cleared the same frame.
[HarmonyPatch(typeof(InventoryGui), "UpdateContainer")]
internal static class InventoryGuiUpdateContainerQuiverPatch
{
    [HarmonyPrefix]
    private static void Prefix(InventoryGui __instance, out Inventory __state)
    {
        __state = null;
        Container container = Traverse.Create(__instance).Field<Container>("m_currentContainer").Value;
        if (container != null && container.IsOwner())
        {
            return;
        }

        Inventory dragInventory = Traverse.Create(__instance).Field<Inventory>("m_dragInventory").Value;
        if (!QuiverInventory.Is(dragInventory) || Player.m_localPlayer == null)
        {
            return;
        }

        __state = dragInventory;
        Traverse.Create(__instance).Field("m_dragInventory").SetValue(Player.m_localPlayer.GetInventory());
    }

    [HarmonyPostfix]
    private static void Postfix(InventoryGui __instance, Inventory __state)
    {
        if (__state != null)
        {
            Traverse.Create(__instance).Field("m_dragInventory").SetValue(__state);
        }
    }
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.GetTotalWeight))]
internal static class InventoryWeightQuiverPatch
{
    [HarmonyPostfix]
    private static void Postfix(Inventory __instance, ref float __result)
    {
        Player player = Player.m_localPlayer;
        if (player != null && player.GetInventory() == __instance)
        {
            QuiverInventory.SyncFromPlayer(player);
            __result += QuiverInventory.GetTotalWeight();
        }
    }
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.IsTeleportable))]
internal static class InventoryTeleportableQuiverPatch
{
    [HarmonyPostfix]
    private static void Postfix(Inventory __instance, ref bool __result)
    {
        if (!__result)
        {
            return;
        }

        Player player = Player.m_localPlayer;
        if (player != null && player.GetInventory() == __instance)
        {
            QuiverInventory.SyncFromPlayer(player);
            if (!QuiverInventory.IsTeleportable())
            {
                __result = false;
            }
        }
    }
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.ContainsItem), typeof(ItemDrop.ItemData))]
internal static class InventoryContainsItemQuiverPatch
{
    [HarmonyPostfix]
    private static void Postfix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
    {
        if (__result || !QuiverInventory.AllowPlayerInventoryContainQuiverItem)
        {
            return;
        }

        Player player = Player.m_localPlayer;
        if (player != null && player.GetInventory() == __instance && QuiverInventory.Contains(item))
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(Inventory), "AddItem", typeof(ItemDrop.ItemData), typeof(Vector2i))]
internal static class InventoryAddItemQuiverPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
    {
        return AcceptOrReject(__instance, item, ref __result);
    }

    internal static bool AcceptOrReject(Inventory inventory, ItemDrop.ItemData item, ref bool result)
    {
        QuiverInventory.StripIncomingIfNewToPlayerBag(inventory, item);
        if (!QuiverInventory.Is(inventory))
        {
            return true;
        }

        if (!QuiverInventory.CanAccept(item))
        {
            result = false;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(Inventory), "AddItem", typeof(ItemDrop.ItemData))]
internal static class InventoryAddItemNoPosQuiverPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
    {
        return InventoryAddItemQuiverPatch.AcceptOrReject(__instance, item, ref __result);
    }
}

[HarmonyPatch(typeof(Inventory), "AddItem", typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int))]
internal static class InventoryAddItemXYQuiverPatch
{
    [HarmonyPrefix]
    private static void Prefix(Inventory __instance, ItemDrop.ItemData item)
    {
        QuiverInventory.StripIncomingIfNewToPlayerBag(__instance, item);
    }
}

[HarmonyPatch(typeof(Inventory), "CanAddItem", typeof(ItemDrop.ItemData), typeof(int))]
internal static class InventoryCanAddItemQuiverPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
    {
        return InventoryAddItemQuiverPatch.AcceptOrReject(__instance, item, ref __result);
    }
}

[HarmonyPatch(typeof(Attack), "FindAmmo")]
internal static class AttackFindAmmoQuiverPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Humanoid character, ItemDrop.ItemData weapon, ref ItemDrop.ItemData __result)
    {
        if (character is not Player player || !player.IsOwner())
        {
            return true;
        }

        QuiverInventory.SyncFromPlayer(player);
        if (QuiverInventory.TryGetSelectedArrow(weapon, out ItemDrop.ItemData arrow))
        {
            __result = arrow;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(Attack), "HaveAmmo")]
internal static class AttackHaveAmmoQuiverPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Humanoid character, ItemDrop.ItemData weapon, ref bool __result)
    {
        if (character is not Player player || !player.IsOwner())
        {
            return true;
        }

        QuiverInventory.SyncFromPlayer(player);
        if (QuiverInventory.TryGetSelectedArrow(weapon, out _))
        {
            __result = true;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(Attack), "EquipAmmoItem")]
internal static class AttackEquipAmmoQuiverPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Humanoid character, ItemDrop.ItemData weapon, ref bool __result)
    {
        if (character is not Player player || !player.IsOwner())
        {
            return true;
        }

        QuiverInventory.SyncFromPlayer(player);
        if (!QuiverInventory.TryGetSelectedArrow(weapon, out ItemDrop.ItemData arrow))
        {
            return true;
        }

        if (!QuiverInventory.TryEquipFromQuiver(character, arrow, triggerEquipEffects: false))
        {
            QuiverInventory.EquipAsAmmo(character, arrow);
        }

        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(Attack), "UseAmmo")]
internal static class AttackUseAmmoQuiverPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Attack __instance, out ItemDrop.ItemData ammoItem, ref bool __result)
    {
        ammoItem = null;
        Humanoid character = Traverse.Create(__instance).Field("m_character").GetValue() as Humanoid;
        ItemDrop.ItemData weapon = Traverse.Create(__instance).Field("m_weapon").GetValue() as ItemDrop.ItemData;
        if (character is not Player player || !player.IsOwner())
        {
            return true;
        }

        QuiverInventory.SyncFromPlayer(player);
        if (!QuiverInventory.TryGetSelectedArrow(weapon, out ItemDrop.ItemData arrow))
        {
            return true;
        }

        ammoItem = arrow;
        QuiverInventory.ConsumeArrow(arrow, 1);
        Traverse.Create(__instance).Field("m_ammoItem").SetValue(arrow);
        __result = true;
        return false;
    }
}
