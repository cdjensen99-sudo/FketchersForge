using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FletchersForge;

/// HUD row while inventory is closed; matching row under the backpack while it is open.
internal static class QuiverHud
{
    private static readonly MethodInfo OnSelectedItemMethod =
        AccessTools.Method(typeof(InventoryGui), "OnSelectedItem");

    private static readonly MethodInfo OnRightClickItemMethod =
        AccessTools.Method(typeof(InventoryGui), "OnRightClickItem");

    private static readonly HashSet<InventoryGrid> boundGrids = new HashSet<InventoryGrid>();
    private static GameObject hudRoot;
    private static InventoryGrid hudGrid;
    private static HotkeyBar boundHotkeyBar;
    private static GameObject invRoot;
    private static InventoryGrid invGrid;
    private static Image invBkg;
    private static bool updateErrorLogged;
    private static bool hudDragging;
    private static bool invDragging;
    /// Tracks InventoryGui open edge so the inventory row is only (re)built on a fresh open —
    /// creating it mid-session (craft/equip while already open) leaves a non-interactive UI.
    private static bool inventoryWasVisible;

    internal static RectTransform HudRootRect => hudRoot != null ? hudRoot.transform as RectTransform : null;

    internal static void Update()
    {
        Player player = Player.m_localPlayer;
        bool show = player != null &&
                    player.IsOwner() &&
                    !player.IsDead() &&
                    QuiverInventory.PlayerHasEquippedQuiver(player);

        if (!show)
        {
            SetActive(hudRoot, false);
            SetActive(invRoot, false);
            return;
        }

        QuiverInventory.SyncFromPlayer(player);
        HandleSlotHotkeys(player);

        bool inventoryOpen = InventoryGui.IsVisible();
        if (inventoryOpen)
        {
            SetActive(hudRoot, false);
            return;
        }
        else
        {
            if (!EnsureHud())
            {
                return;
            }

            SetActive(invRoot, false);
            SetActive(hudRoot, true);
            if (!hudDragging)
            {
                ApplyHudPosition();
            }
            UpdateGrid(hudGrid, player);
        }
    }

    internal static bool IsSelectModifierHeld()
    {
        KeyCode modifier = ModConfig.QuiverSelectModifier?.Value ?? KeyCode.None;
        return modifier != KeyCode.None && ZInput.GetKey(modifier);
    }

    internal static bool IsCursorMode()
    {
        KeyCode key = ModConfig.QuiverCursorKey?.Value ?? KeyCode.None;
        if (key == KeyCode.None)
        {
            return false;
        }

        if (InventoryGui.IsVisible() ||
            Menu.IsVisible() ||
            Console.IsVisible() ||
            Minimap.IsOpen() ||
            (Chat.instance != null && Chat.instance.HasFocus()))
        {
            return false;
        }

        Player player = Player.m_localPlayer;
        if (player == null || !player.IsOwner() || player.IsDead() || !QuiverInventory.PlayerHasEquippedQuiver(player))
        {
            return false;
        }

        return ZInput.GetKey(key);
    }

    internal static void AfterInventoryGuiUpdate()
    {
        if (!InventoryGui.IsVisible())
        {
            inventoryWasVisible = false;
            SetActive(invRoot, false);
            return;
        }

        Player player = Player.m_localPlayer;
        if (player == null || !player.IsOwner() || player.IsDead() || !QuiverInventory.PlayerHasEquippedQuiver(player))
        {
            // Inventory is open; keep the open-edge tracker so a later equip does not
            // treat this as a fresh open and spawn a mid-session (dead) row.
            inventoryWasVisible = true;
            SetActive(invRoot, false);
            return;
        }

        bool justOpened = !inventoryWasVisible;
        inventoryWasVisible = true;

        // Rebuild only when the inventory panel opens. Unequip/equip must not destroy a
        // working row — recreating while the panel stays open is what made slots dead.
        if (justOpened)
        {
            DestroyInventoryRow();
        }

        if (!EnsureInventoryRow())
        {
            return;
        }

        PlaceInventoryRow(InventoryGui.instance.m_playerGrid);
        SetActive(invRoot, true);
        UpdateGrid(invGrid, player);
        invRoot.transform.SetAsLastSibling();
    }

    internal static void NotifyQuiverEquipped()
    {
        // Reuse an existing interactive row. Never destroy/recreate mid-open inventory.
        if (InventoryGui.IsVisible())
        {
            AfterInventoryGuiUpdate();
        }
    }

    internal static void NotifyQuiverUnequipped()
    {
        SetActive(hudRoot, false);
        SetActive(invRoot, false);
    }

    private static void DestroyInventoryRow()
    {
        if (invGrid != null)
        {
            boundGrids.Remove(invGrid);
        }

        if (invRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(invRoot);
        }

        invRoot = null;
        invGrid = null;
        invBkg = null;
        invDragging = false;
    }

    internal static void CancelPlayerCombatInput(Player player)
    {
        if (player == null)
        {
            return;
        }

        Traverse playerFields = Traverse.Create(player);
        playerFields.Field("m_attackDrawTime").SetValue(0f);
        ItemDrop.ItemData weapon = player.GetCurrentWeapon();
        string drawState = weapon?.m_shared?.m_attack?.m_drawAnimationState;
        if (string.IsNullOrEmpty(drawState))
        {
            return;
        }

        object zanim = playerFields.Field("m_zanim").GetValue();
        if (zanim != null)
        {
            Traverse.Create(zanim).Method("SetBool", drawState, false).GetValue();
        }
    }

    internal static void BeginHudDrag()
    {
        hudDragging = true;
    }

    internal static void SaveHudPosition(Vector2 anchoredPosition)
    {
        hudDragging = false;
        ModConfig.QuiverHudCustomPosition.Value = true;
        ModConfig.QuiverHudPosX.Value = anchoredPosition.x;
        ModConfig.QuiverHudPosY.Value = anchoredPosition.y;
    }

    internal static void ResetHudPosition()
    {
        hudDragging = false;
        ModConfig.QuiverHudCustomPosition.Value = false;
        ApplyHudPosition();
    }

    internal static void BeginInvDrag()
    {
        invDragging = true;
    }

    internal static void SaveInvPosition(Vector2 anchoredPosition)
    {
        invDragging = false;
        ModConfig.QuiverInvCustomPosition.Value = true;
        ModConfig.QuiverInvPosX.Value = anchoredPosition.x;
        ModConfig.QuiverInvPosY.Value = anchoredPosition.y;
    }

    internal static void ResetInvPosition()
    {
        invDragging = false;
        ModConfig.QuiverInvCustomPosition.Value = false;
        if (InventoryGui.instance?.m_playerGrid != null)
        {
            PlaceInventoryRow(InventoryGui.instance.m_playerGrid);
        }
    }

    private static void UpdateGrid(InventoryGrid grid, Player player)
    {
        if (grid == null)
        {
            return;
        }

        if (grid.CanDropDragOntoItem == null)
        {
            grid.CanDropDragOntoItem = _ => true;
        }

        InventoryGui gui = InventoryGui.instance;
        ItemDrop.ItemData dragItem = gui != null ? InventoryGuiAccess.GetDragItem(gui) : null;
        try
        {
            grid.UpdateInventory(QuiverInventory.Inventory, player, dragItem);
            SetSlotBindingsAndSelection(grid);
            updateErrorLogged = false;
        }
        catch (Exception ex)
        {
            if (!updateErrorLogged)
            {
                updateErrorLogged = true;
                FletchersForgePlugin.Log?.LogError($"Quiver HUD update failed: {ex}");
            }
        }
    }

    private static bool EnsureHud()
    {
        if (hudGrid != null)
        {
            BindGrid(hudGrid, activateWhenClosed: true);
            return true;
        }

        InventoryGui gui = InventoryGui.instance;
        Hud hud = Hud.instance;
        if (gui?.m_playerGrid == null || hud == null)
        {
            return false;
        }

        HotkeyBar hotkeyBar = hud.GetComponentInChildren<HotkeyBar>(true);
        Transform parent = hotkeyBar != null ? hotkeyBar.transform.parent : hud.transform;
        if (parent == null)
        {
            return false;
        }

        boundHotkeyBar = hotkeyBar;
        float space = hotkeyBar != null ? hotkeyBar.m_elementSpace : gui.m_playerGrid.m_elementSpace;
        hudRoot = CreateGridRoot("FF_QuiverHud", parent, gui.m_playerGrid.m_elementPrefab, space, out hudGrid);
        Canvas hudCanvas = hudRoot.AddComponent<Canvas>();
        hudCanvas.overrideSorting = true;
        hudCanvas.sortingOrder = 300;
        hudRoot.AddComponent<GraphicRaycaster>();
        AddMoveHandle(hudRoot.transform as RectTransform, forInventory: false);
        BindGrid(hudGrid, activateWhenClosed: true);
        FletchersForgePlugin.Log?.LogInfo("Created Fletcher's quiver HUD slots.");
        return true;
    }

    private static bool EnsureInventoryRow()
    {
        if (invGrid != null)
        {
            BindGrid(invGrid, activateWhenClosed: false);
            return true;
        }

        InventoryGui gui = InventoryGui.instance;
        if (gui?.m_playerGrid == null || gui.m_player == null)
        {
            return false;
        }

        InventoryGrid playerGrid = gui.m_playerGrid;
        Transform parent = gui.m_player != null ? gui.m_player : playerGrid.transform;

        invRoot = CreateGridRoot(
            "FF_QuiverInventoryRow",
            parent,
            playerGrid.m_elementPrefab,
            playerGrid.m_elementSpace,
            out invGrid);
        // Stay on the inventory canvas — a nested Canvas+raycaster created mid-session
        // often never receives clicks (dead grips/slots until a full restart).
        AddMoveHandle(invRoot.transform as RectTransform, forInventory: true);
        PlaceInventoryRow(playerGrid);
        BindGrid(invGrid, activateWhenClosed: false);
        FletchersForgePlugin.Log?.LogInfo("Created Fletcher's quiver inventory slots.");
        return true;
    }

    private static GameObject CreateGridRoot(
        string name,
        Transform parent,
        GameObject elementPrefab,
        float elementSpace,
        out InventoryGrid grid)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(0f, 0f);
        rootRect.pivot = new Vector2(0f, 0f);
        rootRect.sizeDelta = new Vector2(ModConstants.QuiverSlotCount * elementSpace, elementSpace);
        rootRect.anchoredPosition = Vector2.zero;

        GameObject gridRootObject = new GameObject("GridRoot", typeof(RectTransform));
        gridRootObject.transform.SetParent(root.transform, false);
        RectTransform gridRoot = gridRootObject.GetComponent<RectTransform>();
        gridRoot.anchorMin = Vector2.zero;
        gridRoot.anchorMax = Vector2.zero;
        gridRoot.pivot = Vector2.zero;
        gridRoot.anchoredPosition = Vector2.zero;
        gridRoot.sizeDelta = rootRect.sizeDelta;

        grid = root.AddComponent<InventoryGrid>();
        grid.m_elementPrefab = elementPrefab;
        grid.m_gridRoot = gridRoot;
        grid.m_elementSpace = elementSpace;
        UIGroupHandler uiGroup = root.AddComponent<UIGroupHandler>();
        Traverse.Create(uiGroup).Field("m_active").SetValue(false);
        Traverse.Create(uiGroup).Field("m_userActive").SetValue(false);
        grid.m_uiGroup = uiGroup;
        return root;
    }

    private static bool PlaceInventoryRow(InventoryGrid playerGrid)
    {
        if (invRoot == null || playerGrid == null)
        {
            return false;
        }

        RectTransform invRect = invRoot.transform as RectTransform;
        if (invRect == null)
        {
            return false;
        }

        bool hasCustom = ModConfig.QuiverInvCustomPosition != null && ModConfig.QuiverInvCustomPosition.Value;
        bool hasDock = TryGetBackpackDockSlot(playerGrid, out RectTransform slot);
        if (!hasDock && !hasCustom)
        {
            return false;
        }

        if (hasDock)
        {
            Transform dock = slot.parent != null ? slot.parent.parent : playerGrid.transform;
            if (dock == null)
            {
                dock = playerGrid.transform;
            }

            for (Transform current = dock; current != null; current = current.parent)
            {
                if (current.GetComponent<RectMask2D>() != null || current.GetComponent<Mask>() != null)
                {
                    dock = current.parent != null ? current.parent : current;
                }
            }

            if (invRoot.transform.parent != dock)
            {
                invRoot.transform.SetParent(dock, false);
            }
        }

        float space = playerGrid.m_elementSpace;
        invRect.anchorMin = new Vector2(0.5f, 0.5f);
        invRect.anchorMax = new Vector2(0.5f, 0.5f);
        invRect.pivot = new Vector2(0f, 1f);
        invRect.sizeDelta = new Vector2(ModConstants.QuiverSlotCount * space, space);

        if (!invDragging)
        {
            if (hasCustom)
            {
                invRect.anchoredPosition = new Vector2(
                    ModConfig.QuiverInvPosX.Value,
                    ModConfig.QuiverInvPosY.Value);
            }
            else
            {
                Vector3[] corners = new Vector3[4];
                slot.GetWorldCorners(corners);
                invRect.position = corners[0] + slot.TransformVector(new Vector3(0f, -space * 0.25f, 0f));
            }
        }

        PlaceInventoryBackground(invRect, space);
        Transform handle = invRoot.transform.Find("MoveHandle");
        if (handle != null)
        {
            handle.SetAsLastSibling();
        }

        return true;
    }

    private static void PlaceInventoryBackground(RectTransform invRect, float space)
    {
        if (invRect == null || invRoot == null)
        {
            return;
        }

        // The woodpanel_* sprite renders as a solid yellow silhouette on this custom row in 1.0
        // (texture never samples correctly outside the vanilla inventory Image setup).
        // Use a plain dark plate so slots keep their normal inventory look.
        DestroyLegacyYellowChrome();

        if (invBkg == null)
        {
            GameObject go = new GameObject("FF_QuiverInvBkg", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(invRoot.transform, false);
            invBkg = go.GetComponent<Image>();
            invBkg.raycastTarget = false;
            invBkg.sprite = null;
            invBkg.material = null;
            // Match the dark recessed inventory tray, not the wood border texture.
            invBkg.color = new Color(0.14f, 0.11f, 0.09f, 0.94f);
        }

        invBkg.transform.SetAsFirstSibling();
        invBkg.enabled = true;

        float padX = space * 0.12f;
        float padY = space * 0.10f;
        RectTransform bkgRect = invBkg.rectTransform;
        bkgRect.localScale = Vector3.one;
        bkgRect.anchorMin = new Vector2(0f, 1f);
        bkgRect.anchorMax = new Vector2(0f, 1f);
        bkgRect.pivot = new Vector2(0f, 1f);
        bkgRect.anchoredPosition = new Vector2(-padX, padY);
        bkgRect.sizeDelta = new Vector2(
            invRect.sizeDelta.x + (padX * 2f),
            invRect.sizeDelta.y + (padY * 2f));
    }

    private static void DestroyLegacyYellowChrome()
    {
        if (invRoot == null)
        {
            return;
        }

        for (int i = invRoot.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = invRoot.transform.GetChild(i);
            if (child == null || !child.name.StartsWith("FF_QuiverInvBkg", StringComparison.Ordinal))
            {
                continue;
            }

            // Keep the current plain plate; remove clones / old woodpanel Images.
            if (invBkg != null && child.gameObject == invBkg.gameObject)
            {
                continue;
            }

            UnityEngine.Object.Destroy(child.gameObject);
        }

        // If the kept plate is still a woodpanel Image, replace it.
        if (invBkg != null &&
            invBkg.sprite != null &&
            invBkg.sprite.name.IndexOf("woodpanel", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            UnityEngine.Object.Destroy(invBkg.gameObject);
            invBkg = null;
        }
    }

    /// Bottom-left cell of the 8-column backpack (row 0, col 0 is the hotbar). Ignores extra slots that sit to the right.
    private static bool TryGetBackpackDockSlot(InventoryGrid playerGrid, out RectTransform slot)
    {
        slot = null;
        IList elements = Traverse.Create(playerGrid).Field("m_elements").GetValue() as IList;
        if (elements == null || elements.Count == 0)
        {
            return false;
        }

        RectTransform origin = null;
        foreach (object element in elements)
        {
            if (!TryReadElement(element, out Vector2i pos, out RectTransform rect))
            {
                continue;
            }

            if (pos.x == 0 && pos.y == 0)
            {
                origin = rect;
                break;
            }
        }

        if (origin == null)
        {
            return false;
        }

        float originX = origin.position.x;
        float cell = playerGrid.m_elementSpace * Mathf.Max(0.01f, Mathf.Abs(origin.lossyScale.x));
        int maxY = -1;
        foreach (object element in elements)
        {
            if (!TryReadElement(element, out Vector2i pos, out RectTransform rect))
            {
                continue;
            }

            if (pos.x != 0 || pos.y < 0)
            {
                continue;
            }

            if (Mathf.Abs(rect.position.x - originX) > cell * 1.5f)
            {
                continue;
            }

            if (pos.y >= maxY)
            {
                maxY = pos.y;
                slot = rect;
            }
        }

        return slot != null;
    }

    private static bool TryReadElement(object element, out Vector2i pos, out RectTransform rect)
    {
        pos = new Vector2i(-1, -1);
        rect = null;
        if (element == null)
        {
            return false;
        }

        // Valheim 1.0: InventoryElement is a MonoBehaviour on the slot object.
        if (element is InventoryElement invElement)
        {
            pos = invElement.Position;
            rect = invElement.transform as RectTransform;
            return rect != null;
        }

        // Pre-1.0 element shape (m_pos + m_go).
        Traverse fields = Traverse.Create(element);
        pos = fields.Field("m_pos").GetValue<Vector2i>();
        GameObject go = fields.Field("m_go").GetValue<GameObject>();
        rect = go != null ? go.transform as RectTransform : null;
        return rect != null;
    }

    private static void ApplyHudPosition()
    {
        RectTransform hudRect = HudRootRect;
        if (hudRect == null)
        {
            return;
        }

        if (ModConfig.QuiverHudCustomPosition.Value)
        {
            hudRect.anchoredPosition = new Vector2(ModConfig.QuiverHudPosX.Value, ModConfig.QuiverHudPosY.Value);
            return;
        }

        if (boundHotkeyBar == null)
        {
            return;
        }

        hudRect.anchorMin = new Vector2(0f, 0f);
        hudRect.anchorMax = new Vector2(0f, 0f);
        hudRect.pivot = new Vector2(0f, 0f);
        hudRect.sizeDelta = new Vector2(
            ModConstants.QuiverSlotCount * boundHotkeyBar.m_elementSpace,
            boundHotkeyBar.m_elementSpace);
        hudRect.position = boundHotkeyBar.transform.position;
        Vector3 local = hudRect.localPosition;
        local.y += GetHudOffsetY(boundHotkeyBar);
        hudRect.localPosition = local;
    }

    private static float GetHudOffsetY(HotkeyBar hotkeyBar)
    {
        float slotHeight = hotkeyBar != null ? hotkeyBar.m_elementSpace : 70f;
        return (-2f * slotHeight) + ModConfig.QuiverHudOffsetY.Value;
    }

    private static void AddMoveHandle(RectTransform rootRect, bool forInventory)
    {
        GameObject handle = new GameObject("MoveHandle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(rootRect, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0f);
        handleRect.anchorMax = new Vector2(0f, 1f);
        handleRect.pivot = new Vector2(1f, 0.5f);
        handleRect.sizeDelta = new Vector2(18f, 0f);
        handleRect.anchoredPosition = new Vector2(-4f, 0f);

        Image image = handle.GetComponent<Image>();
        image.color = new Color(0.15f, 0.12f, 0.1f, 0.65f);
        image.raycastTarget = true;
        QuiverHudMover mover = handle.AddComponent<QuiverHudMover>();
        mover.ForInventory = forInventory;
    }

    private static void BindGrid(InventoryGrid grid, bool activateWhenClosed)
    {
        InventoryGui gui = InventoryGui.instance;
        if (grid == null || gui == null || boundGrids.Contains(grid))
        {
            return;
        }

        boundGrids.Add(grid);

        // Valheim 1.0 UpdateGui always invokes this; vanilla grids get it in InventoryGui.Awake.
        // Leaving it null NREs after the first occupied slot and leaves the rest in broken prefab visuals.
        if (grid.CanDropDragOntoItem == null)
        {
            grid.CanDropDragOntoItem = _ => true;
        }

        grid.m_onSelected += (InventoryGrid selectedGrid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod) =>
        {
            InventoryGui current = InventoryGui.instance;
            if (activateWhenClosed && !InventoryGui.IsVisible())
            {
                if (Player.m_localPlayer == null)
                {
                    return;
                }

                if (mod == InventoryGrid.Modifier.Move || mod == InventoryGrid.Modifier.Split)
                {
                    QuiverInventory.TryMoveSlotToPlayer(Player.m_localPlayer, pos.x);
                    return;
                }

                QuiverInventory.ActivateSlot(Player.m_localPlayer, pos.x);
                return;
            }

            if (mod == InventoryGrid.Modifier.Move && item != null)
            {
                QuiverInventory.TryMoveToPlayer(Player.m_localPlayer, item);
                return;
            }

            OnSelectedItemMethod?.Invoke(current, new object[] { selectedGrid, item, pos, mod });
        };
        grid.m_onRightClick += (InventoryGrid selectedGrid, ItemDrop.ItemData item, Vector2i pos) =>
        {
            if (activateWhenClosed && !InventoryGui.IsVisible())
            {
                QuiverInventory.TryMoveSlotToPlayer(Player.m_localPlayer, pos.x);
                return;
            }

            OnRightClickItemMethod?.Invoke(InventoryGui.instance, new object[] { selectedGrid, item, pos });
        };
    }

    private static void HandleSlotHotkeys(Player player)
    {
        if (Chat.instance != null && Chat.instance.HasFocus())
        {
            return;
        }

        if (Console.IsVisible() || Menu.IsVisible() || Minimap.IsOpen())
        {
            return;
        }

        if (!IsSelectModifierHeld())
        {
            return;
        }

        for (int i = 0; i < ModConstants.QuiverSlotCount; i++)
        {
            if (WasSlotKeyPressed(i))
            {
                QuiverInventory.ActivateSlot(player, i);
                break;
            }
        }
    }

    private static bool WasSlotKeyPressed(int slotIndex)
    {
        KeyCode key = ModConfig.QuiverSlotKeys[slotIndex].Value;
        if (key == KeyCode.None)
        {
            return false;
        }

        if (ZInput.GetKeyDown(key))
        {
            return true;
        }

        if (key >= KeyCode.Alpha1 && key <= KeyCode.Alpha8)
        {
            return ZInput.GetKeyDown(KeyCode.Keypad1 + (key - KeyCode.Alpha1));
        }

        return false;
    }

    private static void SetSlotBindingsAndSelection(InventoryGrid grid)
    {
        if (grid?.m_gridRoot == null)
        {
            return;
        }

        int index = 0;
        foreach (Transform child in grid.m_gridRoot)
        {
            if (child == null)
            {
                continue;
            }

            TMP_Text binding = child.Find("binding")?.GetComponent<TMP_Text>();
            if (binding != null)
            {
                binding.enabled = true;
                binding.textWrappingMode = TextWrappingModes.NoWrap;
                binding.overflowMode = TextOverflowModes.Overflow;
                binding.fontSize = Mathf.Min(binding.fontSize, 12f);
                binding.text = ModConfig.SlotBindingLabel(index);
            }

            Transform selected = child.Find("selected");
            if (selected != null)
            {
                selected.gameObject.SetActive(index == QuiverInventory.SelectedSlot);
            }

            index++;
        }
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
        {
            go.SetActive(active);
        }
    }
}
