using HarmonyLib;

namespace FletchersForge.Patches;

/// Icon rigs clone networked item prefabs; skip ZNet registration during Fejd/ObjectDB setup.
[HarmonyPatch(typeof(ZNetView), "Awake")]
internal static class ZNetViewAwakeIconRigPatch
{
    [HarmonyPrefix]
    private static bool Prefix()
    {
        return !IconRigGuard.IsActive;
    }
}
