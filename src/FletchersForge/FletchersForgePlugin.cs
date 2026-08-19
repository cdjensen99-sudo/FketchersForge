using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn;
using Jotunn.Managers;
using UnityEngine;

namespace FletchersForge;

[BepInPlugin(ModConstants.ModGuid, ModConstants.ModName, ModConstants.ModVersion)]
[BepInDependency(Main.ModGuid)]
public sealed class FletchersForgePlugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;

    private Harmony harmony;

    private void Awake()
    {
        Log = Logger;
        ModConfig.Bind(Config);

        if (!ModConfig.Enabled.Value)
        {
            Log.LogInfo($"{ModConstants.ModName} is disabled in config.");
            return;
        }

        harmony = new Harmony(ModConstants.ModGuid);
        harmony.PatchAll(typeof(FletchersForgePlugin).Assembly);

        LocalizationRegistrar.Initialize();
        gameObject.AddComponent<FletchUiBehaviour>();
        PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
        ItemManager.OnItemsRegisteredFejd += OnItemsRegisteredFejd;
        ItemManager.OnItemsRegistered += OnItemsRegisteredWorld;

        Log.LogInfo($"{ModConstants.ModName} {ModConstants.ModVersion} loaded.");
        Log.LogInfo($"Legacy bench prefab hash: {ModConstants.LegacyContainerPrefabHash}");
    }

    private void OnDestroy()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        ItemManager.OnItemsRegisteredFejd -= OnItemsRegisteredFejd;
        ItemManager.OnItemsRegistered -= OnItemsRegisteredWorld;
        harmony?.UnpatchSelf();
    }

    private static void OnVanillaPrefabsAvailable()
    {
        AssetBundleLoader.EnsureLoaded();
        ItemRegistrar.RegisterAll();
        RecipeRegistrar.RegisterAll();
        PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
    }

    private static void OnItemsRegisteredFejd()
    {
        // Fejd/ObjectDB copy runs before ZDOMan is ready for cloned item prefabs.
        ItemRegistrar.ApplyEmbeddedHeadIconsOnly();
    }

    private static void OnItemsRegisteredWorld()
    {
        ItemRegistrar.ApplyDeferredIcons();
    }
}
