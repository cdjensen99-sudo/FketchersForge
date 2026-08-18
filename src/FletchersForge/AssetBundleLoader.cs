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
    private static bool bundleLoadAttempted;
    private static bool knifeVisualLoadAttempted;
    private static bool quiverVisualLoadAttempted;

    internal static GameObject HeadPouchPrefab
    {
        get
        {
            EnsureBundleLoaded();
            return headPouchPrefab;
        }
    }

    internal static GameObject KnifePrefab
    {
        get
        {
            EnsureKnifeVisualLoaded();
            return knifePrefab;
        }
    }

    internal static GameObject QuiverPrefab
    {
        get
        {
            EnsureQuiverVisualLoaded();
            return quiverPrefab;
        }
    }

    /// Pouch only. Knife and quiver visuals must load after those items are cloned.
    internal static void EnsureLoaded()
    {
        EnsureBundleLoaded();
    }

    private static void EnsureBundleLoaded()
    {
        if (bundleLoadAttempted)
        {
            return;
        }

        bundleLoadAttempted = true;

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

            FletchersForgePlugin.Log?.LogInfo(
                $"Loaded AssetBundle '{ModConstants.AssetBundleName}' (pouch={(headPouchPrefab != null)}).");

            if (headPouchPrefab == null)
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

    private static void EnsureKnifeVisualLoaded()
    {
        EnsureBundleLoaded();
        if (knifeVisualLoadAttempted)
        {
            return;
        }

        knifeVisualLoadAttempted = true;
        knifePrefab = LoadPrefabCopy(
            ModConstants.KnifeVisualPrefabName,
            "Assets/CustomItems/FF_FletchersKnife.prefab",
            "FF_FletchersKnifeVisual");
        FletchersForgePlugin.Log?.LogInfo($"Knife visual loaded: {knifePrefab != null}.");
    }

    private static void EnsureQuiverVisualLoaded()
    {
        EnsureBundleLoaded();
        if (quiverVisualLoadAttempted)
        {
            return;
        }

        quiverVisualLoadAttempted = true;
        quiverPrefab = LoadPrefabCopy(
            ModConstants.QuiverPrefabName,
            "Assets/CustomItems/FF_Quiver.prefab",
            "FF_QuiverVisual");
        FletchersForgePlugin.Log?.LogInfo($"Quiver visual loaded: {quiverPrefab != null}.");
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

    private static GameObject LoadPrefabCopy(string shortName, string assetPath, string instanceName)
    {
        GameObject asset = LoadPrefab(shortName, assetPath);
        if (asset == null)
        {
            return null;
        }

        asset.name = instanceName;
        GameObject copy = Object.Instantiate(asset);
        copy.name = instanceName;
        copy.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(copy);
        return copy;
    }
}
