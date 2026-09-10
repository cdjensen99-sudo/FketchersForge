using System.IO;
using HarmonyLib;

namespace FletchersForge.Patches;

/// <summary>
/// Runs legacy bench cleanup after world ZDOs are loaded into ZDOMan
/// (not during early item registration).
/// Valheim 1.0 worlds are folders with chunk files (<c>_main.0.db2</c>, etc.) and load via
/// <see cref="ZDOMan.LoadChunks"/>; pre-chunked saves still use
/// <see cref="ZDOMan.Load(BinaryReader, Version.World)"/>.
/// </summary>
[HarmonyPatch]
internal static class ZDOManLoadCleanupPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.Load), typeof(BinaryReader), typeof(Version.World))]
    private static void AfterLoad()
    {
        FletchLegacyCleanup.RunAfterWorldZdosLoaded();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.LoadChunks), typeof(string), typeof(FileHelpers.FileSource), typeof(Version.World))]
    private static void AfterLoadChunks()
    {
        FletchLegacyCleanup.RunAfterWorldZdosLoaded();
    }
}
