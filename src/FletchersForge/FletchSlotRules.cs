namespace FletchersForge;

internal static class FletchSlotRules
{
    internal static bool CanAccept(int gridX, int gridY, string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
        {
            return true;
        }

        if (gridX == 0 && gridY == 0)
        {
            return ArrowAssemblyRegistry.IsShaftPrefab(prefabName) ||
                   ArrowAssemblyRegistry.IsArrowPrefab(prefabName);
        }

        if (gridX == 1 && gridY == 0)
        {
            return ArrowAssemblyRegistry.IsHeadPrefab(prefabName);
        }

        return false;
    }
}
