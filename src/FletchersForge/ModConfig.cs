using BepInEx.Configuration;
using UnityEngine;

namespace FletchersForge;

internal static class ModConfig
{
    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<bool> UseEmbeddedHeadIcons;
    internal static ConfigEntry<bool> AllowExternalHeadIconOverrides;
    internal static ConfigEntry<KeyCode> ReforgeKey;
    internal static ConfigEntry<KeyCode> SplitKey;

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
    }
}
