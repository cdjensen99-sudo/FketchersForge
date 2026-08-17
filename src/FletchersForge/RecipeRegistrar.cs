using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace FletchersForge;

internal static class RecipeRegistrar
{
    internal static void RegisterAll()
    {
        RegisterShaftRecipe(
            ModConstants.ShaftStandard,
            1,
            new RequirementConfig("Wood", 8),
            new RequirementConfig("Feathers", 2));

        RegisterShaftRecipe(
            ModConstants.ShaftNeedle,
            4,
            new RequirementConfig("Feathers", 2));

        RegisterShaftRecipe(
            ModConstants.ShaftAsh,
            3,
            new RequirementConfig("Blackwood", 8),
            new RequirementConfig("Feathers", 2));

        RegisterHeadRecipe(ModConstants.HeadFire, CraftingStations.Workbench, 2, new RequirementConfig("Resin", 8));
        RegisterHeadRecipe(ModConstants.HeadFlint, CraftingStations.Workbench, 2, new RequirementConfig("Flint", 2));
        RegisterHeadRecipe(ModConstants.HeadBronze, CraftingStations.Forge, 1, new RequirementConfig("Bronze", 1));
        RegisterHeadRecipe(ModConstants.HeadIron, CraftingStations.Forge, 2, new RequirementConfig("Iron", 1));
        RegisterHeadRecipe(ModConstants.HeadSilver, CraftingStations.Forge, 3, new RequirementConfig("Silver", 1));
        RegisterHeadRecipe(ModConstants.HeadObsidian, CraftingStations.Workbench, 3, new RequirementConfig("Obsidian", 4));
        RegisterHeadRecipe(
            ModConstants.HeadPoison,
            CraftingStations.Workbench,
            3,
            new RequirementConfig("Obsidian", 4),
            new RequirementConfig("Ooze", 2));
        RegisterHeadRecipe(
            ModConstants.HeadFrost,
            CraftingStations.Workbench,
            4,
            new RequirementConfig("Obsidian", 4),
            new RequirementConfig("FreezeGland", 1));
        RegisterHeadRecipe(ModConstants.HeadNeedle, CraftingStations.Workbench, 4, new RequirementConfig("Needle", 4));
        RegisterHeadRecipe(ModConstants.HeadCarapace, CraftingStations.BlackForge, 1, new RequirementConfig("Carapace", 4));
        RegisterHeadRecipe(ModConstants.HeadCharred, CraftingStations.BlackForge, 3, new RequirementConfig("CharredBone", 4));

        RegisterKnifeRecipe();
        RegisterQuiverRecipe();
    }

    private static void RegisterShaftRecipe(string item, int minLevel, params RequirementConfig[] requirements)
    {
        var config = new RecipeConfig
        {
            Name = $"Recipe_{item}",
            Item = item,
            Amount = ModConstants.BatchSize,
            CraftingStation = CraftingStations.Workbench,
            MinStationLevel = minLevel,
            Requirements = requirements,
        };

        ItemManager.Instance.AddRecipe(new CustomRecipe(config));
    }

    private static void RegisterHeadRecipe(string item, string station, int minLevel, params RequirementConfig[] requirements)
    {
        var config = new RecipeConfig
        {
            Name = $"Recipe_{item}",
            Item = item,
            Amount = ModConstants.BatchSize,
            CraftingStation = station,
            MinStationLevel = minLevel,
            Requirements = requirements,
        };

        ItemManager.Instance.AddRecipe(new CustomRecipe(config));
    }

    private static void RegisterKnifeRecipe()
    {
        var config = new RecipeConfig
        {
            Name = "Recipe_FF_FletchersKnife",
            Item = ModConstants.FletchersKnife,
            Amount = 1,
            CraftingStation = CraftingStations.Forge,
            MinStationLevel = 1,
            Requirements = new[]
            {
                new RequirementConfig("FineWood", 1),
                new RequirementConfig("Copper", 1),
                new RequirementConfig("LeatherScraps", 1),
            },
        };

        ItemManager.Instance.AddRecipe(new CustomRecipe(config));
    }

    private static void RegisterQuiverRecipe()
    {
        var config = new RecipeConfig
        {
            Name = "Recipe_FF_Quiver",
            Item = ModConstants.Quiver,
            Amount = 1,
            CraftingStation = CraftingStations.Forge,
            MinStationLevel = 1,
            Requirements = new[]
            {
                new RequirementConfig("DeerHide", 4),
                new RequirementConfig("Bronze", 2),
                new RequirementConfig("FineWood", 4),
            },
        };

        ItemManager.Instance.AddRecipe(new CustomRecipe(config));
    }
}
