using System;

namespace FletchersForge;

/// Last-resort guard so ChestSnap snappoint scans never spam the log on stale world objects.
internal static class ChestSnapMoveNextFinalizerPatch
{
    private static bool logged;

    internal static Exception Finalizer(Exception __exception)
    {
        if (__exception is NullReferenceException)
        {
            if (!logged)
            {
                logged = true;
                FletchersForgePlugin.Log?.LogWarning(
                    "ChestSnap snappoint scan hit a stale world object and was aborted (harmless).");
            }

            return null;
        }

        return __exception;
    }
}
