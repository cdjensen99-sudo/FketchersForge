using BepInEx.Configuration;
using UnityEngine;

namespace FletchersForge;

internal static class ModConfig
{
    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<bool> UseEmbeddedHeadIcons;
    internal static ConfigEntry<bool> AllowExternalHeadIconOverrides;
    internal static ConfigEntry<bool> UseBundledKnifeVisual;
    internal static ConfigEntry<KeyCode> ReforgeKey;
    internal static ConfigEntry<KeyCode> SplitKey;
    internal static ConfigEntry<KeyCode> QuiverCursorKey;
    internal static ConfigEntry<KeyCode> QuiverSelectModifier;
    internal static ConfigEntry<KeyCode>[] QuiverSlotKeys;
    internal static ConfigEntry<float> QuiverHudOffsetY;
    internal static ConfigEntry<bool> QuiverHudCustomPosition;
    internal static ConfigEntry<float> QuiverHudPosX;
    internal static ConfigEntry<float> QuiverHudPosY;
    internal static ConfigEntry<bool> QuiverInvCustomPosition;
    internal static ConfigEntry<float> QuiverInvPosX;
    internal static ConfigEntry<float> QuiverInvPosY;

    internal static void Bind(ConfigFile config)
    {
        Enabled = config.Bind(
            "General",
            "Enabled",
            true,
            "Enable Fletchers Forge. When false, the mod does not register items, recipes, or UI.");

        UseEmbeddedHeadIcons = config.Bind(
            "Icons",
            "UseEmbeddedHeadIcons",
            true,
            "Load arrowhead inventory icons bundled inside FletchersForge.dll.");

        AllowExternalHeadIconOverrides = config.Bind(
            "Icons",
            "AllowExternalHeadIconOverrides",
            false,
            "When true, PNG files in the plugin Icons folder override embedded icons.");

        UseBundledKnifeVisual = config.Bind(
            "Visuals",
            "UseBundledKnifeVisual",
            true,
            "Use the FF_FletchersKnife AssetBundle mesh on the knife. Falls back to kitbash if the bundle fails.");

        ReforgeKey = config.Bind(
            "Controls",
            "Reforge",
            KeyCode.None,
            "While the Fletcher's bench is open: reforge / assemble / rehead. None disables the hotkey (use the button).");

        SplitKey = config.Bind(
            "Controls",
            "Split",
            KeyCode.None,
            "While the Fletcher's bench is open: split arrows. None disables the hotkey (use the button).");

        QuiverCursorKey = config.Bind(
            "Quiver",
            "CursorKey",
            KeyCode.BackQuote,
            "Hold to unlock the mouse: drag the quiver HUD and click a slot to select ammo. Default is the ~ ` key (left of 1).");

        QuiverSelectModifier = config.Bind(
            "Quiver",
            "SelectModifier",
            KeyCode.LeftBracket,
            "Hold with a slot key to select quiver ammo. Default is [ . Set to None to disable keyboard select (click still works).");

        QuiverSlotKeys = new ConfigEntry<KeyCode>[ModConstants.QuiverSlotCount];
        for (int i = 0; i < ModConstants.QuiverSlotCount; i++)
        {
            QuiverSlotKeys[i] = config.Bind(
                "Quiver",
                "Slot" + (i + 1),
                KeyCode.Alpha1 + i,
                "Pressed with SelectModifier to choose quiver slot " + (i + 1) + ".");
        }

        QuiverHudOffsetY = config.Bind(
            "Quiver",
            "HudOffsetY",
            0f,
            "Extra vertical shift for the default HUD placement. Negative moves it down. Ignored after you drag the bar.");

        QuiverHudCustomPosition = config.Bind(
            "Quiver",
            "HudCustomPosition",
            false,
            "True after you drag the quiver HUD. Right-click the left grip (while holding the cursor key) to restore the default spot.");

        QuiverHudPosX = config.Bind(
            "Quiver",
            "HudPosX",
            0f,
            "Saved HUD X after dragging the left grip.");

        QuiverHudPosY = config.Bind(
            "Quiver",
            "HudPosY",
            0f,
            "Saved HUD Y after dragging the left grip.");

        QuiverInvCustomPosition = config.Bind(
            "Quiver",
            "InvCustomPosition",
            false,
            "True after you drag the open-inventory quiver row. Right-click the left grip to restore the default spot under the backpack.");

        QuiverInvPosX = config.Bind(
            "Quiver",
            "InvPosX",
            0f,
            "Saved inventory-row X after dragging the left grip.");

        QuiverInvPosY = config.Bind(
            "Quiver",
            "InvPosY",
            0f,
            "Saved inventory-row Y after dragging the left grip.");
    }

    internal static string FormatKey(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.None:
                return string.Empty;
            case KeyCode.LeftBracket:
                return "[";
            case KeyCode.RightBracket:
                return "]";
            case KeyCode.LeftCurlyBracket:
                return "{";
            case KeyCode.RightCurlyBracket:
                return "}";
            case KeyCode.BackQuote:
                return "~";
            case KeyCode.LeftControl:
            case KeyCode.RightControl:
                return "Ctrl";
            case KeyCode.LeftAlt:
            case KeyCode.RightAlt:
                return "Alt";
            case KeyCode.LeftShift:
            case KeyCode.RightShift:
                return "Shift";
            default:
                string name = key.ToString();
                if (name.StartsWith("Alpha"))
                {
                    return name.Substring("Alpha".Length);
                }

                if (name.StartsWith("Keypad"))
                {
                    return name.Substring("Keypad".Length);
                }

                return name;
        }
    }

    internal static string SlotBindingLabel(int slotIndex)
    {
        KeyCode modifier = QuiverSelectModifier.Value;
        KeyCode slot = QuiverSlotKeys[slotIndex].Value;
        string slotText = FormatKey(slot);
        if (modifier == KeyCode.None)
        {
            return slotText;
        }

        return FormatKey(modifier) + "+" + slotText;
    }
}
