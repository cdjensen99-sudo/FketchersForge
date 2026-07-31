namespace FletchersForge;

/// Sanitizes ZNetScene once before ChestSnap scans container instances.
internal static class ChestSnapCompatPatch
{
    private static bool sanitizedThisSession;

    internal static void Prefix()
    {
        if (sanitizedThisSession)
        {
            return;
        }

        sanitizedThisSession = true;
        int removed = FletchLegacyCleanup.SanitizeNullZNetViews();
        if (removed > 0)
        {
            FletchersForgePlugin.Log?.LogInfo(
                $"ChestSnap compat: removed {removed} stale ZNetView entries before snappoint scan.");
        }
    }
}
