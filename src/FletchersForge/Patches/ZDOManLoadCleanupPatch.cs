using HarmonyLib;

namespace FletchersForge.Patches;

/// Runs legacy bench cleanup after world ZDOs are loaded into ZDOMan (not during early item registration).
[HarmonyPatch(typeof(ZDOMan), "Load")]
internal static class ZDOManLoadCleanupPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        FletchLegacyCleanup.RunAfterWorldZdosLoaded();
    }
}
