using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FletchersForge;

internal static class FletchBenchButtonUi
{
    private const string TakeAllLabelKey = "$inventory_takeall";
    private const string StackAllLabelKey = "$inventory_stackall";

    internal static void Update(InventoryGui gui, bool benchOpen)
    {
        if (gui == null)
        {
            return;
        }

        bool showVanillaContainerButtons = !benchOpen && gui.IsContainerOpen();
        bool showBenchButtons = benchOpen && InventoryGui.IsVisible();

        if (gui.m_takeAllButton != null)
        {
            gui.m_takeAllButton.gameObject.SetActive(showVanillaContainerButtons || showBenchButtons);
            if (showBenchButtons)
            {
                SetButtonLabel(gui.m_takeAllButton, GetReforgeLabel());
            }
            else if (showVanillaContainerButtons)
            {
                RestoreVanillaLabel(gui.m_takeAllButton, TakeAllLabelKey, "Take all");
            }
        }

        if (gui.m_stackAllButton != null)
        {
            gui.m_stackAllButton.gameObject.SetActive(showVanillaContainerButtons || showBenchButtons);
            if (showBenchButtons)
            {
                SetButtonLabel(gui.m_stackAllButton, GetSplitLabel());
            }
            else if (showVanillaContainerButtons)
            {
                RestoreVanillaLabel(gui.m_stackAllButton, StackAllLabelKey, "Place stacks");
            }
        }
    }

    private static string GetReforgeLabel()
    {
        string label = Localization.instance.Localize("$FF_Reforge");
        return string.IsNullOrEmpty(label) || label.StartsWith("$", System.StringComparison.Ordinal)
            ? "Reforge"
            : label;
    }

    private static string GetSplitLabel()
    {
        string label = Localization.instance.Localize("$FF_Split");
        return string.IsNullOrEmpty(label) || label.StartsWith("$", System.StringComparison.Ordinal)
            ? "Split"
            : label;
    }

    private static void RestoreVanillaLabel(Button button, string locKey, string fallback)
    {
        string label = Localization.instance.Localize(locKey);
        if (string.IsNullOrEmpty(label) || label.StartsWith("$", System.StringComparison.Ordinal))
        {
            label = fallback;
        }

        SetButtonLabel(button, label);
    }

    private static void SetButtonLabel(Button button, string label)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
        }
    }
}
