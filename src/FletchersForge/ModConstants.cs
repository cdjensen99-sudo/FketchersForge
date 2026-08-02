namespace FletchersForge;

internal static class ModConstants
{
    public const string ModGuid = "hardwire99.fletchersforge";
    public const string ModName = "Fletchers Forge";
    public const string ModVersion = "0.1.32";

    /// Square ship cargo crate first; karve "chest" is the wooden chest piece (fallback only).
    public const string HeadDropBoxPrimaryPrefab = "CargoCrate";
    public static readonly string[] HeadDropBoxPrefabFallbacks =
    {
        "dvergrprops_crate",
        "shipwreck_karve_chest",
    };
    public const float HeadDropBoxScale = 0.25f;

    public const string FletchersKnife = "FF_FletchersKnife";
    public const string FletchContainer = "FF_FletchContainer";

    internal static readonly int LegacyContainerPrefabHash = FletchContainer.GetStableHashCode();

    public const string ShaftStandard = "FF_ShaftStandard";
    public const string ShaftNeedle = "FF_ShaftNeedle";
    public const string ShaftAsh = "FF_ShaftAsh";

    public const string HeadFire = "FF_HeadFire";
    public const string HeadFlint = "FF_HeadFlint";
    public const string HeadBronze = "FF_HeadBronze";
    public const string HeadIron = "FF_HeadIron";
    public const string HeadSilver = "FF_HeadSilver";
    public const string HeadObsidian = "FF_HeadObsidian";
    public const string HeadPoison = "FF_HeadPoison";
    public const string HeadFrost = "FF_HeadFrost";
    public const string HeadNeedle = "FF_HeadNeedle";
    public const string HeadCarapace = "FF_HeadCarapace";
    public const string HeadCharred = "FF_HeadCharred";

    public const float ShaftWeight = 0.05f;
    public const float HeadWeight = 0.05f;
    public const float KnifeWeight = 0.25f;

    public const int ShaftStackSize = 100;
    public const int HeadStackSize = 200;
    public const int BatchSize = 20;

}
