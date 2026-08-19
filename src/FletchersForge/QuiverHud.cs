using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Jotunn.Managers;
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

    internal static RectTransform HudRootRect => hudRoot != null ? hudRoot.transform as RectTransform : null;

    internal static void Update()
    {
        Player player = Player.m_localPlayer;
        bool show = player != null &&
                    player.IsOwner() &&
                    !player.IsDead() &&
                    QuiverInventory.PlayerHasQuiver(player);

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
        if (player == null || !player.IsOwner() || player.IsDead() || !QuiverInventory.PlayerHasQuiver(player))
        {
            return false;
        }

        return ZInput.GetKey(key);
    }

    internal static void AfterInventoryGuiUpdate()
    {
        if (!InventoryGui.IsVisible())
        {
            SetActive(invRoot, false);
            return;
        }

        Player player = Player.m_localPlayer;
        if (player == null || !player.IsOwner() || player.IsDead() || !QuiverInventory.PlayerHasQuiver(player))
        {
            SetActive(invRoot, false);
            return;
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
        InventoryGui gui = InventoryGui.instance;
        ItemDrop.ItemData dragItem = gui != null ? InventoryGuiAccess.GetDragItem(gui) : null;
        try
        {
            grid.UpdateInventory(QuiverInventory.Inventory, player, dragItem);
            SetSlotBindingsAndSelection(grid);
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
        Canvas invCanvas = invRoot.AddComponent<Canvas>();
        invCanvas.overrideSorting = true;
        invCanvas.sortingOrder = 250;
        invRoot.AddComponent<GraphicRaycaster>();
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

        if (!TryGetBackpackDockSlot(playerGrid, out RectTransform slot))
        {
            return false;
        }

        RectTransform invRect = invRoot.transform as RectTransform;
        if (invRect == null)
        {
            return false;
        }

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

        float space = playerGrid.m_elementSpace;
        invRect.anchorMin = new Vector2(0.5f, 0.5f);
        invRect.anchorMax = new Vector2(0.5f, 0.5f);
        invRect.pivot = new Vector2(0f, 1f);
        invRect.sizeDelta = new Vector2(ModConstants.QuiverSlotCount * space, space);

        if (!invDragging)
        {
            if (ModConfig.QuiverInvCustomPosition != null && ModConfig.QuiverInvCustomPosition.Value)
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
        if (invRect == null)
        {
            return;
        }

        Image source = FindPlayerWoodpanel(InventoryGui.instance);
        Sprite sprite = source?.sprite;
        if (sprite == null || sprite.name.IndexOf("woodpanel", StringComparison.OrdinalIgnoreCase) < 0)
        {
            sprite = GUIManager.Instance?.GetSprite("woodpanel_playerinventory")
                     ?? GUIManager.Instance?.GetSprite("inv_bkg");
        }

        if (sprite == null)
        {
            return;
        }

        if (invBkg == null)
        {
            GameObject go = new GameObject("FF_QuiverInvBkg", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(invRoot.transform, false);
            invBkg = go.GetComponent<Image>();
            invBkg.raycastTarget = false;
        }

        invBkg.transform.SetAsFirstSibling();
        invBkg.sprite = sprite;
        invBkg.type = Image.Type.Sliced;
        invBkg.fillCenter = true;
        invBkg.color = Color.white;
        invBkg.pixelsPerUnitMultiplier = source != null ? source.pixelsPerUnitMultiplier : 1f;
        Material litpanel = source != null ? source.material : PrefabManager.Cache.GetPrefab<Material>("litpanel");
        if (litpanel != null)
        {
            invBkg.material = litpanel;
        }

        float pad = space * 0.16f;

        RectTransform bkgRect = invBkg.rectTransform;
        bkgRect.anchorMin = new Vector2(0f, 1f);
        bkgRect.anchorMax = new Vector2(0f, 1f);
        bkgRect.pivot = new Vector2(0f, 1f);
        bkgRect.anchoredPosition = new Vector2(-pad, pad);
        bkgRect.sizeDelta = new Vector2(
            invRect.sizeDelta.x + (pad * 2f),
            invRect.sizeDelta.y + (pad * 2f));
    }

    private static Image FindPlayerWoodpanel(InventoryGui gui)
    {
        if (gui?.m_player == null)
        {
            return null;
        }

        Image best = null;
        int bestScore = -1;
        foreach (Image image in gui.m_player.GetComponentsInChildren<Image>(true))
        {
            if (image?.sprite == null || image.GetComponentInParent<InventoryGrid>() != null)
            {
                continue;
            }

            string name = image.sprite.name;
            int score = 0;
            if (name.IndexOf("woodpanel_playerinventory", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score = 3;
            }
            else if (name.IndexOf("woodpanel", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score = 2;
            }
            else if (name.IndexOf("inv_bkg", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score = 1;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = image;
            }
        }

        return bestScore > 0 ? best : null;
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
