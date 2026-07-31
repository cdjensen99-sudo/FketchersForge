using System;
using System.Collections.Generic;

namespace FletchersForge;

internal static class ArrowAssemblyRegistry
{
    internal sealed class ArrowPair
    {
        public string ShaftPrefab;
        public string HeadPrefab;
        public string ArrowPrefab;
    }

    private static readonly Dictionary<string, ArrowPair> ArrowToParts = new();
    private static readonly Dictionary<string, string> ShaftHeadToArrow = new();

    internal static void Initialize()
    {
        Register("ArrowWood", ModConstants.ShaftStandard, null, "ArrowWood");
        Register("ArrowFire", ModConstants.ShaftStandard, ModConstants.HeadFire, "ArrowFire");
        Register("ArrowFlint", ModConstants.ShaftStandard, ModConstants.HeadFlint, "ArrowFlint");
        Register("ArrowBronze", ModConstants.ShaftStandard, ModConstants.HeadBronze, "ArrowBronze");
        Register("ArrowIron", ModConstants.ShaftStandard, ModConstants.HeadIron, "ArrowIron");
        Register("ArrowSilver", ModConstants.ShaftStandard, ModConstants.HeadSilver, "ArrowSilver");
        Register("ArrowObsidian", ModConstants.ShaftStandard, ModConstants.HeadObsidian, "ArrowObsidian");
        Register("ArrowPoison", ModConstants.ShaftStandard, ModConstants.HeadPoison, "ArrowPoison");
        Register("ArrowFrost", ModConstants.ShaftStandard, ModConstants.HeadFrost, "ArrowFrost");
        Register("ArrowNeedle", ModConstants.ShaftNeedle, ModConstants.HeadNeedle, "ArrowNeedle");
        Register("ArrowCarapace", ModConstants.ShaftStandard, ModConstants.HeadCarapace, "ArrowCarapace");
        Register("ArrowCharred", ModConstants.ShaftAsh, ModConstants.HeadCharred, "ArrowCharred");
    }

    private static void Register(string arrow, string shaft, string head, string arrowPrefab)
    {
        ArrowToParts[arrow] = new ArrowPair { ShaftPrefab = shaft, HeadPrefab = head, ArrowPrefab = arrowPrefab };
        ShaftHeadToArrow[Key(shaft, head)] = arrowPrefab;
    }

    internal static bool TryGetParts(string arrowPrefabName, out string shaftPrefab, out string headPrefab)
    {
        shaftPrefab = null;
        headPrefab = null;

        if (!ArrowToParts.TryGetValue(arrowPrefabName, out ArrowPair pair))
        {
            return false;
        }

        shaftPrefab = pair.ShaftPrefab;
        headPrefab = pair.HeadPrefab;
        return true;
    }

    internal static bool TryGetArrow(string shaftPrefab, string headPrefab, out string arrowPrefab)
    {
        if (ShaftHeadToArrow.TryGetValue(Key(shaftPrefab, headPrefab), out arrowPrefab))
        {
            return true;
        }

        arrowPrefab = null;
        return false;
    }

    internal static bool IsArrowPrefab(string prefabName) => ArrowToParts.ContainsKey(prefabName);

    internal static bool IsShaftPrefab(string prefabName) =>
        prefabName == ModConstants.ShaftStandard ||
        prefabName == ModConstants.ShaftNeedle ||
        prefabName == ModConstants.ShaftAsh;

    internal static bool IsHeadPrefab(string prefabName) =>
        prefabName.StartsWith("FF_Head", StringComparison.Ordinal);

    internal static bool IsKnifePrefab(string prefabName) =>
        prefabName == ModConstants.FletchersKnife;

    private static string Key(string shaft, string head) => $"{shaft}|{head ?? string.Empty}";
}
