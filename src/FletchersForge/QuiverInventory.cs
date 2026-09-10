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
    /// True while Inventory.Load is reconstituting the bag — do not strip Fletcher-equip from saved quivers.
    internal static bool SuppressIncomingEquipStrip;

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

    /// When an equipped quiver leaves the player bag (chest/ground), pack ammo onto it first.
    internal static void PackIfEquippedQuiverLeavingPlayerBag(Inventory source, ItemDrop.ItemData item)
    {
        Player player = Player.m_localPlayer;
        if (player == null || source == null || item == null)
        {
            return;
        }

        if (source != player.GetInventory() || !IsQuiverItem(item) || !IsEquipped(item))
        {
            return;
        }

        PackAndRelease(player, item);
        QuiverHud.NotifyQuiverUnequipped();
        QuiverBackVisual.Refresh(player);
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

    /// Death only: temporarily expose ammo as real bag cells so tombstone/Take All see them.
    /// Does not keep bag rows while playing — that breaks AzuEPI equipment/quick UI.
    internal static void PrepareEquippedQuiverForDeath(Player player)
    {
        if (player == null)
        {
            return;
        }

        Inventory bag = player.GetInventory();
        if (bag == null)
        {
            return;
        }

        ItemDrop.ItemData equipped = FindEquippedQuiver(player);
        if (equipped != null)
        {
            if (boundQuiver != equipped)
            {
                boundQuiver = equipped;
                LoadBound();
            }

            QuiverTombstoneDump.RememberEquippedQuiver(equipped);
            QuiverBagBridge.EnsureRows(player);
            QuiverBagBridge.PushUiToBag(player);
            TagReservedAsOwned(player);
            Dictionary<string, string> data = EnsureCustomData(equipped);
            data.Remove(ContentsKey);
        }

        bool wasBound = boundQuiver != null;
        foreach (ItemDrop.ItemData item in bag.GetAllItems())
        {
            if (IsQuiverItem(item) && (IsEquipped(item) || item.m_equipped))
            {
                SetEquipped(item, false);
            }
        }

        if (wasBound)
        {
            boundQuiver = null;
            saving = true;
            try
            {
                inventory?.RemoveAll();
            }
            finally
            {
                saving = false;
            }
        }

        QuiverHud.NotifyQuiverUnequipped();
        QuiverBackVisual.Refresh(player);
    }

    internal static void ReleaseRowsAfterGrave(Player player)
    {
        QuiverBagBridge.ReleaseRows(player);
    }

    /// Incoming quiver (grave, chest, world) must not arrive Fletcher-equipped.
    /// Skipped during Inventory.Load so a saved FF_QuiverEquipped=1 survives login.
    internal static void StripIncomingIfNewToPlayerBag(Inventory dest, ItemDrop.ItemData item)
    {
        if (SuppressIncomingEquipStrip)
        {
            return;
        }

        Player player = Player.m_localPlayer;
        if (player == null || dest == null || item == null || dest != player.GetInventory())
        {
            return;
        }

        if (!IsQuiverItem(item) || dest.ContainsItem(item))
        {
            return;
        }

        SetEquipped(item, false);
    }

    /// Only one quiver may be equipped. Contents stay on each quiver item when unequipped.
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
                if (boundQuiver == item)
                {
                    SaveBound();
                    boundQuiver = null;
                    LoadBound();
                }
            }
        }
    }

    private static void PackAndRelease(Player player, ItemDrop.ItemData quiver)
    {
        if (player == null || quiver == null)
        {
            return;
        }

        if (boundQuiver == quiver || IsEquipped(quiver))
        {
            if (QuiverBagBridge.ExtraRowsActive > 0)
            {
                QuiverBagBridge.PullBagToUi(player);
            }

            if (boundQuiver != quiver)
            {
                boundQuiver = quiver;
            }

            SaveBound();
            if (QuiverBagBridge.ExtraRowsActive > 0)
            {
                QuiverBagBridge.ClearReservedCells(player.GetInventory());
                QuiverBagBridge.ReleaseRows(player);
            }

            saving = true;
            try
            {
                if (boundQuiver == quiver)
                {
                    // keep contents saved; clear live UI binding
                }
            }
            finally
            {
                saving = false;
            }

            boundQuiver = null;
            LoadBound();
        }

        SetEquipped(quiver, false);
        QuiverBagBridge.ReleaseRows(player);
    }

    private static void SetEquipped(ItemDrop.ItemData item, bool equipped)
    {
        Dictionary<string, string> data = EnsureCustomData(item);
        data[EquippedKey] = equipped ? "1" : "0";
        // Fletcher equip lives in custom data only. Persisting m_equipped=true makes vanilla
        // EquipInventoryItems() call EquipItem on login, fail (we block it), and clear m_equipped.
        if (!equipped)
        {
            item.m_equipped = false;
        }
    }

    /// Clear vanilla m_equipped on quivers before character save so login does not treat them as gear.
    internal static void ClearVanillaEquippedFlags(Player player)
    {
        Inventory bag = player?.GetInventory();
        if (bag == null)
        {
            return;
        }

        foreach (ItemDrop.ItemData item in bag.GetAllItems())
        {
            if (IsQuiverItem(item))
            {
                item.m_equipped = false;
            }
        }
    }

    internal static void SetEquippedPublic(ItemDrop.ItemData item, bool equipped)
    {
        if (!IsQuiverItem(item))
        {
            return;
        }

        SetEquipped(item, equipped);
    }

    /// After tombstone loot: Fletcher-equip and move tagged ammo back into the quiver UI / packed data.
    internal static void EquipOnly(Player player, ItemDrop.ItemData quiver)
    {
        if (player == null || !IsQuiverItem(quiver))
        {
            return;
        }

        UnequipAllQuivers(player, except: quiver);
        SetEquipped(quiver, true);
        boundQuiver = quiver;
        LoadBound();
        ClaimTaggedAmmoIntoUi(player, quiver);
        SaveBound();
        QuiverHud.NotifyQuiverEquipped();
        QuiverBackVisual.Refresh(player);
    }

    private static void TagReservedAsOwned(Player player)
    {
        if (player == null || QuiverBagBridge.ExtraRowsActive <= 0)
        {
            return;
        }

        ItemDrop.ItemData owner = boundQuiver ?? FindEquippedQuiver(player);
        if (owner == null)
        {
            return;
        }

        string id = QuiverTombstoneDump.EnsureQuiverId(owner);
        Inventory bag = player.GetInventory();
        if (bag == null)
        {
            return;
        }

        foreach (ItemDrop.ItemData item in bag.GetAllItems())
        {
            if (item != null && QuiverBagBridge.IsReservedCell(item.m_gridPos))
            {
                EnsureCustomData(item)[QuiverTombstoneDump.DumpIdKey] = id;
            }
        }
    }

    private static void ClaimTaggedAmmoIntoUi(Player player, ItemDrop.ItemData quiver)
    {
        Inventory bag = player?.GetInventory();
        if (bag == null || quiver == null || inventory == null)
        {
            return;
        }

        string id = QuiverTombstoneDump.EnsureQuiverId(quiver);
        List<ItemDrop.ItemData> tagged = new List<ItemDrop.ItemData>();
        foreach (ItemDrop.ItemData item in bag.GetAllItems())
        {
            if (item?.m_customData != null &&
                item.m_customData.TryGetValue(QuiverTombstoneDump.DumpIdKey, out string dumpId) &&
                dumpId == id)
            {
                tagged.Add(item);
            }
        }

        foreach (ItemDrop.ItemData item in tagged)
        {
            bag.RemoveItem(item);
            item.m_customData?.Remove(QuiverTombstoneDump.DumpIdKey);
            if (!inventory.AddItem(item))
            {
                bag.AddItem(item);
                FletchersForgePlugin.Log?.LogWarning(
                    $"Restore: quiver full; left '{item.m_shared?.m_name}' in backpack.");
            }
        }
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
            return inventory == null || inventory.IsTeleportable(false);
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
                if (!inventory.IsTeleportable(false))
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

    /// Matching ammo for a bow/crossbow only. Melee and staffs have no ammo type and must not
    /// consume quiver stacks. Empty selected slot falls through to the next matching slot.
    internal static bool TryGetSelectedArrow(ItemDrop.ItemData weapon, out ItemDrop.ItemData arrow)
    {
        arrow = null;
        if (weapon?.m_shared == null || string.IsNullOrWhiteSpace(weapon.m_shared.m_ammoType))
        {
            return false;
        }

        EnsureCreated();
        string ammoType = weapon.m_shared.m_ammoType;
        if (TryGetSlotAmmo(SelectedSlot, ammoType, out arrow))
        {
            return true;
        }

        for (int i = 0; i < ModConstants.QuiverSlotCount; i++)
        {
            if (i == SelectedSlot)
            {
                continue;
            }

            if (TryGetSlotAmmo(i, ammoType, out arrow))
            {
                SelectedSlot = i;
                SaveBound();
                return true;
            }
        }

        return false;
    }

    private static bool TryGetSlotAmmo(int slot, string ammoType, out ItemDrop.ItemData ammo)
    {
        ammo = inventory != null ? inventory.GetItemAt(slot, 0) : null;
        if (ammo?.m_dropPrefab == null || ammo.m_shared == null || ammo.m_stack <= 0)
        {
            ammo = null;
            return false;
        }

        if (!ArrowAssemblyRegistry.IsProjectileAmmoPrefab(ammo.m_dropPrefab.name) ||
            ammo.m_shared.m_ammoType != ammoType)
        {
            ammo = null;
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

        return weightProbe.IsTeleportable(false);
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
        if (saving || QuiverBagBridge.IsSyncing)
        {
            return;
        }

        SaveBound();
    }
}
