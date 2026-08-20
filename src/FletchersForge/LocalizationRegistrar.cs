using Jotunn.Managers;

namespace FletchersForge;

internal static class LocalizationRegistrar
{
    internal static void Initialize()
    {
        var loc = LocalizationManager.Instance.GetLocalization();

        loc.AddTranslation("English", "FF_ModName", "Fletchers Forge");
        loc.AddTranslation("English", "FF_FletchersKnife", "Fletcher's knife");
        loc.AddTranslation("English", "FF_FletchersKnife_desc",
            "Field tool for the Fletcher's bench. Hold it ready to open the bench, then reforge or split arrows. Not meant for combat — no damage and does not wear out from use.");
        loc.AddTranslation("English", "FF_ShaftStandard", "Arrow shaft");
        loc.AddTranslation("English", "FF_ShaftStandard_desc", "A wooden arrow shaft with fletching.");
        loc.AddTranslation("English", "FF_ShaftNeedle", "Needle arrow shaft");
        loc.AddTranslation("English", "FF_ShaftNeedle_desc", "A feathered shaft for needle arrows.");
        loc.AddTranslation("English", "FF_ShaftAsh", "Ashwood arrow shaft");
        loc.AddTranslation("English", "FF_ShaftAsh_desc", "A dark ashwood shaft with fletching.");
        loc.AddTranslation("English", "FF_HeadFire", "Fire arrowhead");
        loc.AddTranslation("English", "FF_HeadFlint", "Flint arrowhead");
        loc.AddTranslation("English", "FF_HeadBronze", "Bronze arrowhead");
        loc.AddTranslation("English", "FF_HeadIron", "Iron arrowhead");
        loc.AddTranslation("English", "FF_HeadSilver", "Silver arrowhead");
        loc.AddTranslation("English", "FF_HeadObsidian", "Obsidian arrowhead");
        loc.AddTranslation("English", "FF_HeadPoison", "Poison arrowhead");
        loc.AddTranslation("English", "FF_HeadFrost", "Frost arrowhead");
        loc.AddTranslation("English", "FF_HeadNeedle", "Needle arrowhead");
        loc.AddTranslation("English", "FF_HeadCarapace", "Carapace arrowhead");
        loc.AddTranslation("English", "FF_HeadCharred", "Charred arrowhead");
        loc.AddTranslation("English", "FF_Quiver", "Fletcher's quiver");
        loc.AddTranslation("English", "FF_Quiver_desc",
            "Right-click in inventory to equip or unequip. While equipped: eight fletcher slots under the backpack, HUD when inventory is closed, and a cosmetic quiver on your back. Hold ~ to free the mouse and click a HUD slot, or hold [ with 1–8.");
        loc.AddTranslation("English", "FF_QuiverEquipped", "Equipped Fletcher's quiver");
        loc.AddTranslation("English", "FF_QuiverUnequipped", "Unequipped Fletcher's quiver");
        loc.AddTranslation("English", "FF_FletchContainer", "Fletcher's bench");
        loc.AddTranslation("English", "FF_Reforge", "Reforge");
        loc.AddTranslation("English", "FF_Split", "Split");
        loc.AddTranslation("English", "FF_Preview", "Result");
        loc.AddTranslation("English", "FF_OpenBenchHint", "[{0}] Fletcher's bench");
    }
}
