using System;
using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;

namespace FletchersForge;

/// Eight fletcher slots stored on the quiver item itself.
internal static class QuiverInventory
{
    internal const string ContentsKey = "FF_QuiverInventory";
    internal const string SelectedSlotKey = "FF_QuiverSlot";
    internal const string EquippedKey = "FF_QuiverEquipped";

    private static Inventory inventory;
    private static ItemDrop.ItemData boundQuiver;
    private static bool saving;

    internal static Inventory Inventory
    {
        get
        {
            EnsureCreated();
            return inventory;
        }
    }

    internal static int SelectedSlot { get; private set; }

    internal static bool Is(Inventory other)
    {
        return inventory != null && other == inventory;
    }

    internal static void EnsureCreated()
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

        inventory = new Inventory("$FF_Quiver", background, ModConstants.QuiverSlotCount, 1);
        inventory.m_onChanged += OnInventoryChanged;
    }

    internal static void SyncFromPlayer(Player player)
    {
        EnsureCreated();
        RefreshEquippedFlags(player);
        ItemDrop.ItemData quiver = FindEquippedQuiver(player);
        if (quiver == boundQuiver)
        {
            return;
        }

        SaveBound();
        boundQuiver = quiver;
        LoadBound();
    }

    private static void RefreshEquippedFlags(Player player)
    {
        Inventory playerInventory = player?.GetInventory();
        if (playerInventory == null)
        {
            return;
        }

        foreach (ItemDrop.ItemData item in playerInventory.GetAllItems())
        {
            if (IsQuiverItem(item))
            {
                // Blue inventory highlight (same flag vanilla gear uses).
                item.m_equipped = IsEquipped(item);
            }
        }
    }

    internal static ItemDrop.ItemData FindFirstQuiver(Player player)
    {
        Inventory playerInventory = player?.GetInventory();
        if (playerInventory == null)
        {
            return null;
        }

        foreach (ItemDrop.ItemData item in playerInventory.GetAllItems())
        {
            if (IsQuiverItem(item))
            {
                return item;
            }
        }

        return null;
    }

    internal static ItemDrop.ItemData FindEquippedQuiver(Player player)
    {
        Inventory playerInventory = player?.GetInventory();
        if (playerInventory == null)
        {
            return null;
        }

        foreach (ItemDrop.ItemData item in playerInventory.GetAllItems())
        {
            if (IsQuiverItem(item) && IsEquipped(item))
            {
                return item;
            }
        }

        return null;
    }

    internal static bool PlayerHasQuiver(Player player) => FindFirstQuiver(player) != null;

    /// Active quiver: right-click equip in inventory. Gates HUD, inventory row, and bow ammo.
    internal static bool PlayerHasEquippedQuiver(Player player) => FindEquippedQuiver(player) != null;

    internal static bool IsQuiverItem(ItemDrop.ItemData item)
    {
        if (item == null)
        {
            return false;
        }

        if (item.m_shared?.m_name == "$FF_Quiver")
        {
            return true;
        }

        return item.m_dropPrefab != null && ArrowAssemblyRegistry.IsQuiverPrefab(item.m_dropPrefab.name);
    }

    internal static bool IsEquipped(ItemDrop.ItemData item)
    {
        if (!IsQuiverItem(item))
        {
            return false;
        }

        Dictionary<string, string> data = EnsureCustomData(item);
        return data.TryGetValue(EquippedKey, out string value) && value == "1";
    }

    internal static bool ToggleEquip(Player player, ItemDrop.ItemData quiver)
    {
        if (player == null || !IsQuiverItem(quiver))
        {
            return false;
        }

        if (IsEquipped(quiver))
        {
            SetEquipped(quiver, false);
            if (boundQuiver == quiver)
            {
                SaveBound();
                boundQuiver = null;
                LoadBound();
            }

            QuiverHud.NotifyQuiverUnequipped();
            QuiverBackVisual.Refresh(player);
            player.Message(MessageHud.MessageType.Center, "$FF_QuiverUnequipped");
            return true;
        }

        UnequipAllQuivers(player, except: quiver);
        SetEquipped(quiver, true);
        SyncFromPlayer(player);
        QuiverHud.NotifyQuiverEquipped();
        QuiverBackVisual.Refresh(player);
        player.Message(MessageHud.MessageType.Center, "$FF_QuiverEquipped");
        return true;
    }

    /// Only one quiver may be equipped. Contents stay on each quiver item (separate custom data).
    private static void UnequipAllQuivers(Player player, ItemDrop.ItemData except)
    {
        Inventory playerInventory = player?.GetInventory();
        if (playerInventory == null)
        {
            return;
        }

        foreach (ItemDrop.ItemData item in playerInventory.GetAllItems())
        {
            if (item != except && IsQuiverItem(item) && IsEquipped(item))
            {
                SetEquipped(item, false);
            }
        }
    }

    private static void SetEquipped(ItemDrop.ItemData item, bool equipped)
    {
        Dictionary<string, string> data = EnsureCustomData(item);
        data[EquippedKey] = equipped ? "1" : "0";
        item.m_equipped = equipped;
    }

    internal static bool Contains(ItemDrop.ItemData item)
    {
        return inventory != null && item != null && inventory.ContainsItem(item);
    }

    internal static bool CanAccept(ItemDrop.ItemData item)
    {
        if (item?.m_dropPrefab == null)
        {
            return false;
        }

        return ArrowAssemblyRegistry.IsQuiverStorageItem(item.m_dropPrefab.name);
    }

    /// EquipItem requires the item to live in the player inventory. Set this while calling it for quiver items.
    internal static bool AllowPlayerInventoryContainQuiverItem { get; set; }

    internal static bool TryEquipFromQuiver(Humanoid character, ItemDrop.ItemData item, bool triggerEquipEffects)
    {
        if (character == null || item == null || !Contains(item))
        {
            return false;
        }

        AllowPlayerInventoryContainQuiverItem = true;
        try
        {
            return character.EquipItem(item, triggerEquipEffects);
        }
        finally
        {
            AllowPlayerInventoryContainQuiverItem = false;
        }
    }

    internal static void EquipAsAmmo(Humanoid character, ItemDrop.ItemData arrow)
    {
        if (character == null || arrow == null)
        {
            return;
        }

        if (character.GetAmmoItem() == arrow)
        {
            return;
        }

        ItemDrop.ItemData current = character.GetAmmoItem();
        if (current != null)
        {
            character.UnequipItem(current, triggerEquipEffects: false);
        }

        Traverse.Create(character).Field("m_ammoItem").SetValue(arrow);
        arrow.m_equipped = true;
    }

    internal static float GetTotalWeight()
    {
        Player player = Player.m_localPlayer;
        return player != null ? GetCarriedQuiverContentsWeight(player) : 0f;
    }

    /// Contents of every quiver in the backpack (equipped or not). Quiver item weight is vanilla.
    internal static float GetCarriedQuiverContentsWeight(Player player)
    {
        Inventory playerInventory = player?.GetInventory();
        if (playerInventory == null)
        {
            return 0f;
        }

        float total = 0f;
        foreach (ItemDrop.ItemData item in playerInventory.GetAllItems())
        {
            if (!IsQuiverItem(item))
            {
                continue;
            }

            if (item == boundQuiver && inventory != null)
            {
                total += inventory.GetTotalWeight();
            }
            else
            {
                total += ProbeStoredContentsWeight(item);
            }
        }

        return total;
    }

    internal static bool IsTeleportable()
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return inventory == null || inventory.IsTeleportable();
        }

        Inventory playerInventory = player.GetInventory();
        if (playerInventory == null)
        {
            return true;
        }

        foreach (ItemDrop.ItemData item in playerInventory.GetAllItems())
        {
            if (!IsQuiverItem(item))
            {
                continue;
            }

            if (item == boundQuiver && inventory != null)
            {
                if (!inventory.IsTeleportable())
                {
                    return false;
                }
            }
            else if (!ProbeStoredContentsTeleportable(item))
            {
                return false;
            }
        }

        return true;
    }

    internal static ItemDrop.ItemData GetSelectedItem()
    {
        EnsureCreated();
        return inventory.GetItemAt(SelectedSlot, 0);
    }

    internal static bool TryGetSelectedArrow(ItemDrop.ItemData weapon, out ItemDrop.ItemData arrow)
    {
        arrow = GetSelectedItem();
        if (arrow?.m_dropPrefab == null ||
            !ArrowAssemblyRegistry.IsProjectileAmmoPrefab(arrow.m_dropPrefab.name))
        {
            arrow = null;
            return false;
        }

        if (weapon?.m_shared != null &&
            !string.IsNullOrEmpty(weapon.m_shared.m_ammoType) &&
            arrow.m_shared.m_ammoType != weapon.m_shared.m_ammoType)
        {
            arrow = null;
            return false;
        }

        return true;
    }

    internal static void ActivateSlot(Player player, int slot)
    {
        if (player == null || slot < 0 || slot >= ModConstants.QuiverSlotCount)
        {
            return;
        }

        SyncFromPlayer(player);
        if (boundQuiver == null)
        {
            return;
        }

        SelectedSlot = slot;
        SaveBound();

        ItemDrop.ItemData item = inventory.GetItemAt(slot, 0);
        if (item?.m_dropPrefab == null)
        {
            return;
        }

        if (ArrowAssemblyRegistry.IsProjectileAmmoPrefab(item.m_dropPrefab.name))
        {
            if (!TryEquipFromQuiver(player, item, triggerEquipEffects: true))
            {
                EquipAsAmmo(player, item);
            }

            return;
        }

        if (ArrowAssemblyRegistry.IsKnifePrefab(item.m_dropPrefab.name))
        {
            TryEquipFromQuiver(player, item, triggerEquipEffects: true);
        }
    }

    internal static bool TryMoveToPlayer(Player player, ItemDrop.ItemData item)
    {
        if (player == null || item == null || !Contains(item))
        {
            return false;
        }

        UnequipIfUsing(player, item);
        Inventory destination = player.GetInventory();
        if (destination == null || !destination.CanAddItem(item))
        {
            player.Message(MessageHud.MessageType.Center, "$hud_inventoryfull");
            return false;
        }

        destination.MoveItemToThis(inventory, item);
        SaveBound();
        return true;
    }

    internal static bool TryMoveSlotToPlayer(Player player, int slot)
    {
        if (player == null || slot < 0 || slot >= ModConstants.QuiverSlotCount)
        {
            return false;
        }

        SyncFromPlayer(player);
        return TryMoveToPlayer(player, inventory.GetItemAt(slot, 0));
    }

    internal static void UnequipIfUsing(Player player, ItemDrop.ItemData item)
    {
        if (player == null || item == null)
        {
            return;
        }

        if (player.GetAmmoItem() == item || item.m_equipped)
        {
            player.UnequipItem(item, triggerEquipEffects: false);
            item.m_equipped = false;
        }
    }

    internal static void ConsumeArrow(ItemDrop.ItemData arrow, int amount)
    {
        if (inventory == null || arrow == null)
        {
            return;
        }

        inventory.RemoveItem(arrow, amount);
        SaveBound();
    }

    internal static void SaveBound()
    {
        if (boundQuiver == null)
        {
            return;
        }

        EnsureCreated();
        saving = true;
        try
        {
            Dictionary<string, string> data = EnsureCustomData(boundQuiver);
            ZPackage pkg = new ZPackage();
            inventory.Save(pkg);
            data[ContentsKey] = Convert.ToBase64String(pkg.GetArray());
            data[SelectedSlotKey] = SelectedSlot.ToString();
        }
        finally
        {
            saving = false;
        }
    }

    internal static void MigrateLegacyPlayerData(Player player)
    {
        if (player?.m_customData == null)
        {
            return;
        }

        if (!player.m_customData.TryGetValue(ModConstants.QuiverSaveKey, out string data) ||
            string.IsNullOrEmpty(data))
        {
            return;
        }

        ItemDrop.ItemData quiver = FindFirstQuiver(player);
        if (quiver != null)
        {
            Dictionary<string, string> quiverData = EnsureCustomData(quiver);
            if (!quiverData.ContainsKey(ContentsKey))
            {
                quiverData[ContentsKey] = data;
            }
        }

        player.m_customData.Remove(ModConstants.QuiverSaveKey);
        SyncFromPlayer(player);
    }

    private static Inventory weightProbe;

    private static Inventory EnsureWeightProbe()
    {
        if (weightProbe == null)
        {
            weightProbe = new Inventory("FF_QuiverWeightProbe", null, ModConstants.QuiverSlotCount, 1);
        }

        return weightProbe;
    }

    private static float ProbeStoredContentsWeight(ItemDrop.ItemData quiver)
    {
        if (!TryLoadStoredContents(quiver, EnsureWeightProbe()))
        {
            return 0f;
        }

        return weightProbe.GetTotalWeight();
    }

    private static bool ProbeStoredContentsTeleportable(ItemDrop.ItemData quiver)
    {
        if (!TryLoadStoredContents(quiver, EnsureWeightProbe()))
        {
            return true;
        }

        return weightProbe.IsTeleportable();
    }

    private static bool TryLoadStoredContents(ItemDrop.ItemData quiver, Inventory probe)
    {
        if (quiver == null || probe == null)
        {
            return false;
        }

        Dictionary<string, string> custom = EnsureCustomData(quiver);
        if (!custom.TryGetValue(ContentsKey, out string data) || string.IsNullOrEmpty(data))
        {
            probe.RemoveAll();
            return false;
        }

        try
        {
            probe.RemoveAll();
            probe.Load(new ZPackage(Convert.FromBase64String(data)));
            return true;
        }
        catch (Exception ex)
        {
            FletchersForgePlugin.Log?.LogWarning($"Failed to probe quiver contents weight: {ex.Message}");
            probe.RemoveAll();
            return false;
        }
    }

    private static void LoadBound()
    {
        EnsureCreated();
        saving = true;
        try
        {
            inventory.RemoveAll();
            SelectedSlot = 0;
            if (boundQuiver == null)
            {
                return;
            }

            Dictionary<string, string> custom = EnsureCustomData(boundQuiver);
            if (custom.TryGetValue(ContentsKey, out string data) &&
                !string.IsNullOrEmpty(data))
            {
                try
                {
                    inventory.Load(new ZPackage(Convert.FromBase64String(data)));
                }
                catch (Exception ex)
                {
                    FletchersForgePlugin.Log?.LogWarning($"Failed to load quiver contents: {ex.Message}");
                }
            }

            if (custom.TryGetValue(SelectedSlotKey, out string slotText) &&
                int.TryParse(slotText, out int slot))
            {
                SelectedSlot = Mathf.Clamp(slot, 0, ModConstants.QuiverSlotCount - 1);
            }
        }
        finally
        {
            saving = false;
        }
    }

    private static Dictionary<string, string> EnsureCustomData(ItemDrop.ItemData item)
    {
        if (item.m_customData == null)
        {
            item.m_customData = new Dictionary<string, string>();
        }

        return item.m_customData;
    }

    private static void OnInventoryChanged()
    {
        if (!saving)
        {
            SaveBound();
        }
    }
}
