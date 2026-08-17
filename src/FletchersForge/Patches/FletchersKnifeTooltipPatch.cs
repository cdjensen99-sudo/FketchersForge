using System;
using System.Text;
using HarmonyLib;

namespace FletchersForge.Patches;

/// Vanilla weapon tooltips always add knockback / backstab / block / parry lines.
/// Skip those for the Fletcher's knife so it reads as a field tool.
[HarmonyPatch]
internal static class FletchersKnifeTooltipPatch
{
    private static readonly string[] CombatTooltipTokens =
    {
        "$item_knockback",
        "$item_backstab",
        "$item_blockarmor",
        "$item_blockforce",
        "$item_parrybonus",
        "$item_parryadrenaline",
        "$item_staminause",
        "$item_eitruse",
        "$item_healthuse",
        "$item_damagemultipliertotal",
        "$item_damagemultiplierhp",
    };

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemDrop.ItemData), "AddBlockTooltip")]
    private static bool SkipBlockTooltip(ItemDrop.ItemData item)
    {
        return !FletchersKnifeHelper.IsKnife(item);
    }

    [HarmonyPostfix]
    [HarmonyPatch(
        typeof(ItemDrop.ItemData),
        nameof(ItemDrop.ItemData.GetTooltip),
        new Type[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int) })]
    private static void StripCombatLines(ItemDrop.ItemData item, ref string __result)
    {
        if (string.IsNullOrEmpty(__result) || !FletchersKnifeHelper.IsKnife(item))
        {
            return;
        }

        var kept = new StringBuilder(__result.Length);
        string[] lines = __result.Split(new[] { '\n' }, StringSplitOptions.None);
        bool wrote = false;
        foreach (string line in lines)
        {
            if (IsCombatStatLine(line))
            {
                continue;
            }

            if (wrote)
            {
                kept.Append('\n');
            }

            kept.Append(line);
            wrote = true;
        }

        __result = kept.ToString();
    }

    private static bool IsCombatStatLine(string line)
    {
        for (int i = 0; i < CombatTooltipTokens.Length; i++)
        {
            if (line.IndexOf(CombatTooltipTokens[i], StringComparison.Ordinal) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
