using System.Collections;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;

namespace FletchersForge;

internal static class FletchUiService
{
    private static bool benchUiOpen;
    private static string statusMessage = string.Empty;
    private static float statusUntil;
    private static bool benchPanelSaved;
    private static readonly RectState panelState = new RectState();
    private static readonly RectState gridState = new RectState();
    private static readonly RectState gridRootState = new RectState();
    private static readonly RectState takeAllState = new RectState();
    private static readonly RectState stackAllState = new RectState();

    internal static bool IsFletchInventory(Inventory inventory)
    {
        Inventory benchInventory = FletchBenchInventory.Inventory;
        return benchInventory != null && benchInventory == inventory;
    }

    internal static bool IsBenchUiOpen => benchUiOpen;

    internal static bool IsFletchContainerOpen =>
        benchUiOpen &&
        InventoryGui.instance != null &&
        InventoryGui.IsVisible();

    internal static void Open(Player player)
    {
        if (!FletchersKnifeHelper.IsKnifeInHand(player))
        {
            return;
        }

        if (IsFletchContainerOpen)
        {
            return;
        }

        FletchBenchInventory.Initialize();
        Inventory benchInventory = FletchBenchInventory.Inventory;
        if (benchInventory == null)
        {
            FletchersForgePlugin.Log?.LogError("Virtual Fletcher bench is not ready.");
            return;
        }

        if (InventoryGui.instance == null)
        {
            FletchersForgePlugin.Log?.LogError("InventoryGui not available.");
            return;
        }

        InventoryGui gui = InventoryGui.instance;
        FletchBenchInventory.ClearSlots();

        if (!InventoryGui.IsVisible())
        {
            gui.Show(null);
        }

        InventoryGuiAccess.SetHiddenFrames(gui, 0);
        InventoryGuiAccess.SetActiveGroup(gui, 1, playSound: false);
        InventoryGuiAccess.SetFirstContainerUpdate(gui, true);

        benchUiOpen = true;
        ApplyCompactBenchPanel(gui);
        FletchBenchButtonUi.Update(gui, true);

        FletchersForgePlugin.Log?.LogInfo("Opened virtual Fletcher bench UI.");
    }

    internal static void NotifyGuiClosed()
    {
        benchUiOpen = false;
        FletchBenchInventory.ClearSlots();
        RestoreBenchPanel();
    }

    internal static void Close()
    {
        if (!benchUiOpen && !IsFletchContainerOpen)
        {
            return;
        }

        InventoryGui gui = InventoryGui.instance;
        benchUiOpen = false;
        FletchBenchInventory.ClearSlots();
        RestoreBenchPanel();

        if (gui?.m_container != null)
        {
            gui.m_container.gameObject.SetActive(false);
        }

        FletchBenchButtonUi.Update(gui, false);
    }

    internal static void ApplyCompactBenchPanel(InventoryGui gui)
    {
        RectTransform panel = gui?.m_container;
        InventoryGrid grid = gui?.ContainerGrid;
        if (panel == null || grid == null)
        {
            return;
        }

        RectTransform gridRect = grid.transform as RectTransform;
        if (!benchPanelSaved)
        {
            panelState.Capture(panel);
            gridState.Capture(gridRect);
            gridRootState.Capture(grid.m_gridRoot);
            takeAllState.Capture(gui.m_takeAllButton != null ? gui.m_takeAllButton.transform as RectTransform : null);
            stackAllState.Capture(gui.m_stackAllButton != null ? gui.m_stackAllButton.transform as RectTransform : null);
            benchPanelSaved = true;
        }

        panelState.Restore();
        gridState.Restore();
        gridRootState.Restore();
        takeAllState.Restore();
        stackAllState.Restore();

        float space = grid.m_elementSpace > 1f ? grid.m_elementSpace : 70f;
        float targetWidth = space * 6.2f;
        float targetHeight = space * 3.05f;
        Vector2 current = panel.rect.size;
        panel.sizeDelta += new Vector2(targetWidth - current.x, targetHeight - current.y);

        PlaceNativeButtons(gui, space);
        PlaceSlotsBetweenHeaderAndButtons(panel, grid, space);
        ClearPlayerInventoryOverlap(gui, panel);
    }

    private static void PlaceNativeButtons(InventoryGui gui, float space)
    {
        float inset = Mathf.Max(8f, space * 0.12f);
        RectTransform take = gui.m_takeAllButton != null ? gui.m_takeAllButton.transform as RectTransform : null;
        if (take != null)
        {
            take.anchorMin = new Vector2(0f, 0f);
            take.anchorMax = new Vector2(0f, 0f);
            take.pivot = new Vector2(0f, 0f);
            take.anchoredPosition = new Vector2(inset, inset);
        }

        RectTransform split = gui.m_stackAllButton != null ? gui.m_stackAllButton.transform as RectTransform : null;
        if (split != null)
        {
            split.anchorMin = new Vector2(1f, 0f);
            split.anchorMax = new Vector2(1f, 0f);
            split.pivot = new Vector2(1f, 0f);
            split.anchoredPosition = new Vector2(-inset, inset);
        }
    }

    private static void PlaceSlotsBetweenHeaderAndButtons(RectTransform panel, InventoryGrid grid, float space)
    {
        IList elements = Traverse.Create(grid).Field("m_elements").GetValue() as IList;
        if (elements == null || elements.Count == 0)
        {
            return;
        }

        Vector3[] slotCorners = new Vector3[4];
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        int found = 0;
        foreach (object element in elements)
        {
            GameObject go = Traverse.Create(element).Field("m_go").GetValue<GameObject>();
            RectTransform slot = go != null ? go.transform as RectTransform : null;
            if (slot == null)
            {
                continue;
            }

            slot.GetWorldCorners(slotCorners);
            minX = Mathf.Min(minX, slotCorners[0].x);
            maxX = Mathf.Max(maxX, slotCorners[2].x);
            minY = Mathf.Min(minY, slotCorners[0].y);
            maxY = Mathf.Max(maxY, slotCorners[1].y);
            found++;
        }

        if (found == 0)
        {
            return;
        }

        Vector3[] panelCorners = new Vector3[4];
        panel.GetWorldCorners(panelCorners);
        float scaleY = Mathf.Abs(panel.lossyScale.y);
        float header = space * scaleY * 0.55f;
        float footer = space * scaleY * 0.95f;
        float desiredCenterX = (panelCorners[0].x + panelCorners[3].x) * 0.5f;
        float desiredCenterY = ((panelCorners[1].y - header) + (panelCorners[0].y + footer)) * 0.5f;
        Vector3 delta = new Vector3(
            desiredCenterX - ((minX + maxX) * 0.5f),
            desiredCenterY - ((minY + maxY) * 0.5f),
            0f);

        Transform mover = grid.m_gridRoot != null ? grid.m_gridRoot : grid.transform;
        mover.position += delta;
    }

    private static void ClearPlayerInventoryOverlap(InventoryGui gui, RectTransform panel)
    {
        RectTransform player = gui.m_player;
        if (player == null || panel == null)
        {
            return;
        }

        Vector3[] playerCorners = new Vector3[4];
        Vector3[] panelCorners = new Vector3[4];
        player.GetWorldCorners(playerCorners);
        panel.GetWorldCorners(panelCorners);
        float overlap = panelCorners[1].y - (playerCorners[0].y - 12f);
        if (overlap > 0.5f)
        {
            panel.position += new Vector3(0f, -overlap, 0f);
        }
    }

    private static void RestoreBenchPanel()
    {
        if (!benchPanelSaved)
        {
            return;
        }

        panelState.Restore();
        gridState.Restore();
        gridRootState.Restore();
        takeAllState.Restore();
        stackAllState.Restore();
        benchPanelSaved = false;
    }

    private sealed class RectState
    {
        private RectTransform rt;
        private Vector2 anchored;
        private Vector2 size;
        private Vector2 anchorMin;
        private Vector2 anchorMax;
        private Vector2 pivot;
        private Vector3 scale;
        private bool saved;

        public void Capture(RectTransform target)
        {
            rt = target;
            saved = target != null;
            if (!saved)
            {
                return;
            }

            anchored = target.anchoredPosition;
            size = target.sizeDelta;
            anchorMin = target.anchorMin;
            anchorMax = target.anchorMax;
            pivot = target.pivot;
            scale = target.localScale;
        }

        public void Restore()
        {
            if (!saved || rt == null)
            {
                return;
            }

            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchored;
            rt.localScale = scale;
        }
    }

    internal static void TryReforge(Player player)
    {
        if (!IsFletchContainerOpen || FletchBenchInventory.Inventory == null)
        {
            return;
        }

        FletchOperations.TryReforge(player, FletchBenchInventory.Inventory, out string message);
        SetStatus(message);
    }

    internal static void TrySplit(Player player)
    {
        if (!IsFletchContainerOpen || FletchBenchInventory.Inventory == null)
        {
            return;
        }

        FletchOperations.TrySplit(player, FletchBenchInventory.Inventory, out string message);
        SetStatus(message);
    }

    internal static void DrawBenchOverlay()
    {
        if (!IsFletchContainerOpen || !TryGetContainerScreenRect(out Rect box))
        {
            return;
        }

        if (Time.time < statusUntil && !string.IsNullOrEmpty(statusMessage))
        {
            const float statusHeight = 20f;
            Rect statusRect = new Rect(box.x + 12f, box.y + box.height - 52f, box.width - 24f, statusHeight);
            GUI.Label(statusRect, statusMessage);
        }
    }

    internal static void SetStatus(string message)
    {
        statusMessage = message;
        statusUntil = Time.time + 3f;
    }

    private static bool TryGetContainerScreenRect(out Rect rect)
    {
        rect = default;
        InventoryGui gui = InventoryGui.instance;
        if (gui?.m_container == null)
        {
            return false;
        }

        RectTransform container = gui.m_container;
        Vector3[] corners = new Vector3[4];
        container.GetWorldCorners(corners);

        Canvas canvas = container.GetComponentInParent<Canvas>();
        Camera uiCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);

        float x = bottomLeft.x;
        float y = Screen.height - topRight.y;
        float width = topRight.x - bottomLeft.x;
        float height = topRight.y - bottomLeft.y;
        rect = new Rect(x, y, width, height);
        return width > 0f && height > 0f;
    }
}
