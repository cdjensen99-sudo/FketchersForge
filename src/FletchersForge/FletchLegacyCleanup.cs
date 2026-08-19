using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FletchersForge;

/// Removes ghost bench objects left in world saves by early builds (0.1.1–0.1.8).
internal static class FletchLegacyCleanup
{
    private static readonly int LegacyContainerPrefabHash = ModConstants.LegacyContainerPrefabHash;
    private static readonly int LegacyUnknownPrefabHash = 555343901;
    private static bool fullPurgeCompleted;

    private static readonly MethodInfo HandleDestroyedZdoMethod =
        AccessTools.Method(typeof(ZDOMan), "HandleDestroyedZDO");

    internal static void RunAfterWorldZdosLoaded()
    {
        fullPurgeCompleted = false;
        Run(forceFullPurge: true);
    }

    internal static void Run(bool forceFullPurge = false)
    {
        SanitizeNullZNetViews();

        if (!forceFullPurge && fullPurgeCompleted)
        {
            return;
        }

        int zdoRemoved = PurgeLegacyZdosFromWorldSave(out int found, out int remaining);
        int sceneRemoved = PurgeRuntimeInstances();
        int objectsRemoved = PurgeNamedObjects();
        int sanitizedViews = SanitizeNullZNetViews();

        if (forceFullPurge || zdoRemoved > 0 || sceneRemoved > 0 || objectsRemoved > 0)
        {
            fullPurgeCompleted = true;
        }

        FletchersForgePlugin.Log?.LogInfo(
            $"Legacy bench cleanup: found {found} ZDO(s), removed {zdoRemoved} from world save, " +
            $"{remaining} remain in memory, {sceneRemoved} scene view(s) cleared, {objectsRemoved} named object(s) destroyed. " +
            $"Tracked hashes: {LegacyContainerPrefabHash}, {LegacyUnknownPrefabHash}. " +
            $"IsServer={ZNet.instance != null && ZNet.instance.IsServer()}.");

        if (found == 0)
        {
            FletchersForgePlugin.Log?.LogInfo(
                "No legacy bench ZDOs in world save — phantom bench cleanup is complete for this world.");
        }

        if (zdoRemoved > 0)
        {
            FletchersForgePlugin.Log?.LogInfo("Run console command 'save' to persist ZDO removal.");
        }
        else if (found > 0)
        {
            FletchersForgePlugin.Log?.LogWarning(
                "Legacy bench ZDO(s) were found but could not be removed. Load as world host and try fletcher.cleanup again.");
        }

        if (remaining > 0)
        {
            FletchersForgePlugin.Log?.LogWarning(
                $"{remaining} legacy bench ZDO(s) still in memory after cleanup.");
        }

        if (sanitizedViews > 0)
        {
            FletchersForgePlugin.Log?.LogInfo(
                $"Removed {sanitizedViews} stale ZNetView entries from ZNetScene.");
        }
    }

    internal static int SanitizeNullZNetViews()
    {
        if (ZNetScene.instance == null)
        {
            return 0;
        }

        Dictionary<ZDO, ZNetView> instances =
            Traverse.Create(ZNetScene.instance).Field<Dictionary<ZDO, ZNetView>>("m_instances").Value;

        if (instances == null)
        {
            return 0;
        }

        List<ZDO> keysToRemove = new List<ZDO>();
        foreach (KeyValuePair<ZDO, ZNetView> pair in instances)
        {
            if (pair.Key == null || !IsLiveZNetView(pair.Value))
            {
                keysToRemove.Add(pair.Key);
            }
        }

        foreach (ZDO key in keysToRemove)
        {
            if (key != null)
            {
                instances.Remove(key);
            }
        }

        return keysToRemove.Count;
    }

    internal static bool IsLiveZNetView(ZNetView view)
    {
        if (view == null)
        {
            return false;
        }

        try
        {
            return view.gameObject != null;
        }
        catch (NullReferenceException)
        {
            return false;
        }
    }

    private static int PurgeLegacyZdosFromWorldSave(out int found, out int remaining)
    {
        found = 0;
        remaining = 0;

        if (ZDOMan.instance == null)
        {
            return 0;
        }

        HashSet<ZDO> toDestroy = new HashSet<ZDO>();
        CollectZdosByPrefabName(ModConstants.FletchContainer, toDestroy);
        CollectZdosByPrefabHash(toDestroy);
        found = toDestroy.Count;

        int removed = 0;
        foreach (ZDO zdo in toDestroy)
        {
            if (ForceDestroyZdo(zdo))
            {
                removed++;
            }
        }

        remaining = CountLegacyZdos();
        return removed;
    }

    private static int CountLegacyZdos()
    {
        HashSet<ZDO> legacy = new HashSet<ZDO>();
        CollectZdosByPrefabName(ModConstants.FletchContainer, legacy);
        CollectZdosByPrefabHash(legacy);
        return legacy.Count;
    }

    private static void CollectZdosByPrefabName(string prefabName, HashSet<ZDO> output)
    {
        if (ZDOMan.instance == null || string.IsNullOrEmpty(prefabName))
        {
            return;
        }

        List<ZDO> batch = new List<ZDO>();
        int index = 0;
        while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefabName, batch, ref index))
        {
            foreach (ZDO zdo in batch)
            {
                if (zdo != null)
                {
                    output.Add(zdo);
                }
            }

            batch.Clear();
        }

        foreach (ZDO zdo in batch)
        {
            if (zdo != null)
            {
                output.Add(zdo);
            }
        }
    }

    private static void CollectZdosByPrefabHash(HashSet<ZDO> output)
    {
        foreach (ZDO zdo in EnumerateAllZdos())
        {
            if (zdo == null)
            {
                continue;
            }

            int prefab = zdo.GetPrefab();
            if (prefab == LegacyContainerPrefabHash || prefab == LegacyUnknownPrefabHash)
            {
                output.Add(zdo);
            }
        }
    }

    private static IEnumerable<ZDO> EnumerateAllZdos()
    {
        if (ZDOMan.instance == null)
        {
            return new List<ZDO>();
        }

        HashSet<ZDO> seen = new HashSet<ZDO>();
        List<ZDO> all = new List<ZDO>();
        Traverse zdoMan = Traverse.Create(ZDOMan.instance);

        object objectsById =
            zdoMan.Field("m_objectsByID").GetValue() ?? zdoMan.Field("m_objectsById").GetValue();

        if (objectsById is IDictionary idTable)
        {
            foreach (object value in idTable.Values)
            {
                AddUniqueZdo(all, seen, value as ZDO);
            }
        }
        else
        {
            FletchersForgePlugin.Log?.LogWarning("Legacy cleanup could not read ZDOMan m_objectsByID.");
        }

        object outsideSector = zdoMan.Field("m_objectsByOutsideSector").GetValue();
        if (outsideSector is IDictionary outsideBuckets)
        {
            foreach (object value in outsideBuckets.Values)
            {
                CollectZdosFromBucket(all, seen, value);
            }
        }

        // Valheim stores sector buckets as List<ZDO>[] (not a 2D array).
        object sectorBuckets = zdoMan.Field("m_objectsBySector").GetValue();
        if (sectorBuckets is List<ZDO>[] sectorList)
        {
            foreach (List<ZDO> bucket in sectorList)
            {
                CollectZdosFromBucket(all, seen, bucket);
            }
        }

        return all;
    }

    private static void CollectZdosFromBucket(List<ZDO> all, HashSet<ZDO> seen, object bucket)
    {
        if (bucket is List<ZDO> zdoList)
        {
            foreach (ZDO zdo in zdoList)
            {
                AddUniqueZdo(all, seen, zdo);
            }
        }
    }

    private static void AddUniqueZdo(List<ZDO> all, HashSet<ZDO> seen, ZDO zdo)
    {
        if (zdo == null || seen.Contains(zdo))
        {
            return;
        }

        seen.Add(zdo);
        all.Add(zdo);
    }

    private static int PurgeRuntimeInstances()
    {
        if (ZNetScene.instance == null)
        {
            return 0;
        }

        Dictionary<ZDO, ZNetView> instances =
            Traverse.Create(ZNetScene.instance).Field<Dictionary<ZDO, ZNetView>>("m_instances").Value;

        if (instances == null)
        {
            return 0;
        }

        int removed = 0;
        List<ZDO> keysToRemove = new List<ZDO>();

        foreach (KeyValuePair<ZDO, ZNetView> pair in instances)
        {
            ZDO zdo = pair.Key;
            ZNetView view = pair.Value;

            if (view == null || view.gameObject == null)
            {
                if (zdo != null)
                {
                    keysToRemove.Add(zdo);
                }

                removed++;
                continue;
            }

            if (zdo != null &&
                (zdo.GetPrefab() == LegacyContainerPrefabHash || zdo.GetPrefab() == LegacyUnknownPrefabHash))
            {
                UnityEngine.Object.Destroy(view.gameObject);
                keysToRemove.Add(zdo);
                removed++;
            }
        }

        foreach (ZDO key in keysToRemove)
        {
            instances.Remove(key);
        }

        return removed;
    }

    private static int PurgeNamedObjects()
    {
        int removed = 0;
        foreach (GameObject objectInScene in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (objectInScene == null)
            {
                continue;
            }

            string name = objectInScene.name;
            if (name == ModConstants.FletchContainer || name == "FF_FletchWorkbenchRuntime")
            {
                UnityEngine.Object.Destroy(objectInScene);
                removed++;
            }
        }

        return removed;
    }

    private static bool ForceDestroyZdo(ZDO zdo)
    {
        if (zdo == null || ZDOMan.instance == null)
        {
            return false;
        }

        if (ZNetScene.instance != null)
        {
            GameObject instance = ZNetScene.instance.FindInstance(zdo.m_uid);
            if (instance != null)
            {
                UnityEngine.Object.Destroy(instance);
            }

            Dictionary<ZDO, ZNetView> instances =
                Traverse.Create(ZNetScene.instance).Field<Dictionary<ZDO, ZNetView>>("m_instances").Value;

            if (instances != null)
            {
                instances.Remove(zdo);
            }
        }

        // DestroyZDO only queues removal when zdo.IsOwner() — legacy phantom ZDOs are not owned and were silently skipped.
        if (ZNet.instance != null && ZNet.instance.IsServer() && HandleDestroyedZdoMethod != null)
        {
            HandleDestroyedZdoMethod.Invoke(ZDOMan.instance, new object[] { zdo.m_uid });
            return ZDOMan.instance.GetZDO(zdo.m_uid) == null;
        }

        if (zdo.IsOwner())
        {
            ZDOMan.instance.DestroyZDO(zdo);
            return true;
        }

        FletchersForgePlugin.Log?.LogWarning(
            $"Could not remove legacy bench ZDO {zdo.m_uid} (not owner; load world as host).");
        return false;
    }
}
