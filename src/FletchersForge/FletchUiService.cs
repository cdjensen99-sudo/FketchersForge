using Jotunn.Managers;
using UnityEngine;

namespace FletchersForge;

internal static class FletchUiService
{
    private static bool benchUiOpen;
    private static string statusMessage = string.Empty;
    private static float statusUntil;

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
        FletchBenchButtonUi.Update(gui, true);

        FletchersForgePlugin.Log?.LogInfo("Opened virtual Fletcher bench UI.");
    }

    internal static void NotifyGuiClosed()
    {
        benchUiOpen = false;
        FletchBenchInventory.ClearSlots();
        QuiverHud.RestoreBenchContainerPosition();
    }

    internal static void Close()
    {
        if (!benchUiOpen && !IsFletchContainerOpen)
        {
            return;
        }

        benchUiOpen = false;
        FletchBenchInventory.ClearSlots();
        QuiverHud.RestoreBenchContainerPosition();

        if (InventoryGui.instance != null && InventoryGui.IsVisible())
        {
            InventoryGui.instance.Hide();
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
