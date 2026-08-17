using Jotunn.Utils;
using UnityEngine;

namespace FletchersForge;

/// Loads the Unity AssetBundle that ships inside FletchersForge.dll.
internal static class AssetBundleLoader
{
    private static AssetBundle bundle;
    private static GameObject headPouchPrefab;
    private static GameObject knifePrefab;
    private static GameObject quiverPrefab;
    private static bool loadAttempted;

    internal static GameObject HeadPouchPrefab
    {
        get
        {
            EnsureLoaded();
            return headPouchPrefab;
        }
    }

    internal static GameObject KnifePrefab
    {
        get
        {
            EnsureLoaded();
            return knifePrefab;
        }
    }

    internal static GameObject QuiverPrefab
    {
        get
        {
            EnsureLoaded();
            return quiverPrefab;
        }
    }

    internal static void EnsureLoaded()
    {
        if (loadAttempted)
        {
            return;
        }

        loadAttempted = true;

        try
        {
            bundle = AssetUtils.LoadAssetBundleFromResources(
                ModConstants.AssetBundleName,
                typeof(FletchersForgePlugin).Assembly);

            if (bundle == null)
            {
                FletchersForgePlugin.Log?.LogWarning(
                    $"AssetBundle '{ModConstants.AssetBundleName}' not found in embedded resources.");
                return;
            }

            headPouchPrefab = LoadPrefab(
                ModConstants.HeadPouchPrefabName,
                "Assets/CustomItems/FF_HeadPouch.prefab");
            knifePrefab = LoadPrefab(
                ModConstants.KnifeVisualPrefabName,
                "Assets/CustomItems/FF_FletchersKnife.prefab");
            quiverPrefab = LoadPrefab(
                ModConstants.QuiverPrefabName,
                "Assets/CustomItems/FF_Quiver.prefab");

            FletchersForgePlugin.Log?.LogInfo(
                $"Loaded AssetBundle '{ModConstants.AssetBundleName}' " +
                $"(pouch={(headPouchPrefab != null)}, knife={(knifePrefab != null)}, quiver={(quiverPrefab != null)}).");

            if (headPouchPrefab == null || knifePrefab == null || quiverPrefab == null)
            {
                FletchersForgePlugin.Log?.LogWarning(
                    $"AssetBundle assets: {string.Join(", ", bundle.GetAllAssetNames())}");
            }
        }
        catch (System.Exception ex)
        {
            FletchersForgePlugin.Log?.LogError($"Failed to load AssetBundle '{ModConstants.AssetBundleName}': {ex}");
        }
    }

    private static GameObject LoadPrefab(string shortName, string assetPath)
    {
        if (bundle == null)
        {
            return null;
        }

        GameObject prefab = bundle.LoadAsset<GameObject>(shortName);
        if (prefab == null && !string.IsNullOrEmpty(assetPath))
        {
            prefab = bundle.LoadAsset<GameObject>(assetPath);
        }

        return prefab;
    }
}
