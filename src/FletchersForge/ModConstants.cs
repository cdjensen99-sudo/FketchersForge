namespace FletchersForge;

internal static class ModConstants
{
    public const string ModGuid = "hardwire99.fletchersforge";
    public const string ModName = "Fletchers Forge";
    public const string ModVersion = "0.2.10";

    public const string AssetBundleName = "fletchersforge";
    public const string HeadPouchPrefabName = "FF_HeadPouch";
    public const string KnifeVisualPrefabName = "FF_FletchersKnife";
    public const string QuiverPrefabName = "FF_Quiver";
    /// Extra scale vs the vanilla attach/mesh. Icon uses the same mesh.
    public const float KnifeVisualMeshScale = 0.85f;
    /// Slide grip from handle-bounds center toward the pommel (0.5 = pommel end).
    public const float KnifeVisualHandleShift = 0.4f;
    /// Minimum world-drop box so a thin dagger still hits terrain.
    public const float KnifeDropColliderMinSize = 0.1f;
    public const float KnifeVisualLocalEulerX = 0f;
    public const float KnifeVisualLocalEulerY = 0f;
    public const float KnifeVisualLocalEulerZ = 0f;
    public const string KnifeVisualMaterialTemplate = "KnifeCopper";
    /// Scale for the leather pouch world-drop visual.
    public const float HeadDropPouchScale = 2.5f;
    public const float HeadDropPouchColliderRadius = 0.22f;

    /// Fallback when the pouch AssetBundle is missing — square ship cargo crate.
    public const string HeadDropBoxPrimaryPrefab = "CargoCrate";
    public static readonly string[] HeadDropBoxPrefabFallbacks =
    {
        "dvergrprops_crate",
        "shipwreck_karve_chest",
    };
    public const float HeadDropBoxScale = 0.25f;

    public const string FletchersKnife = "FF_FletchersKnife";
    public const string Quiver = "FF_Quiver";
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
    public const float QuiverWeight = 1f;
    public const float QuiverDropScale = 1.375f;
    public const int QuiverSlotCount = 8;
    public const string QuiverSaveKey = "FF_QuiverInventory";

    // Back cosmetic (hardcoded from in-game tuning). FBX needs ~0.01 base scale.
    public const float QuiverBackBaseScale = 0.011f; // ~10% larger than the 0.01 tuned look
    public const float QuiverBackPosX = 0f;
    public const float QuiverBackPosY = 0f;
    public const float QuiverBackPosZ = 0.001f;
    // Tip top toward the body (90 flared the opening out; 80 made it worse).
    public const float QuiverBackEulerX = 105f;
    public const float QuiverBackEulerY = 180f;
    public const float QuiverBackEulerZ = 135f;

    public const int ShaftStackSize = 100;
    public const int HeadStackSize = 200;
    public const int BatchSize = 20;

}
