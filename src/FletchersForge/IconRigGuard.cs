namespace FletchersForge;

/// Suppresses ZNetView.Awake while building temporary icon-render rigs.
internal static class IconRigGuard
{
    internal static int Depth;

    internal static bool IsActive => Depth > 0;

    internal static void Enter()
    {
        Depth++;
    }

    internal static void Leave()
    {
        if (Depth > 0)
        {
            Depth--;
        }
    }
}
