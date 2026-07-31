using HarmonyLib;

namespace FletchersForge.Patches;

/// Runs legacy cleanup after the world and ZDOs are loaded (local player spawn).
[HarmonyPatch(typeof(Player), "OnSpawned")]
internal static class PlayerSpawnCleanupPatch
{
    private static bool spawnCleanupDone;

    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if (__instance == null || !__instance.IsOwner() || spawnCleanupDone)
        {
            return;
        }

        spawnCleanupDone = true;
        // Backup if ZDOMan.Load already completed before player spawn.
        FletchLegacyCleanup.Run();
    }
}
