using Jotunn.Managers;
using UnityEngine;

namespace FletchersForge;

internal static class CustomVisualUtility
{
    internal static GameObject ApplyBundledVisual(
        GameObject itemPrefab,
        GameObject bundledPrefab,
        string visualRootName,
        Vector3 localPosition,
        Vector3 localEulerAngles,
        Vector3 localScale)
    {
        if (itemPrefab == null || bundledPrefab == null)
        {
            return null;
        }

        RemoveVisualRoot(itemPrefab, visualRootName);

        Transform parent = itemPrefab.transform.Find("attach") ?? itemPrefab.transform;
        GameObject visualRoot = new GameObject(visualRootName);
        visualRoot.transform.SetParent(parent, false);
        visualRoot.transform.localPosition = localPosition;
        visualRoot.transform.localRotation = Quaternion.Euler(localEulerAngles);
        visualRoot.transform.localScale = localScale;

        GameObject instance = Object.Instantiate(bundledPrefab, visualRoot.transform);
        instance.name = visualRootName + "_Mesh";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        PrepareBundledInstance(instance);

        return visualRoot;
    }

    internal static void PrepareBundledInstance(GameObject instance)
    {
        StripLodGroup(instance);
        DisableColliders(instance);
        StripRigidbodies(instance);
    }

    internal static void RemoveVisualRootFromItem(GameObject itemPrefab, string visualRootName)
    {
        RemoveVisualRoot(itemPrefab, visualRootName);
    }

    internal static bool HasEnabledRenderers(GameObject root)
    {
        if (root == null)
        {
            return false;
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                continue;
            }

            foreach (Material material in materials)
            {
                if (material != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// Asset Store meshes ship URP materials; swap to a vanilla item material so they render in Valheim.
    internal static void ApplyTemplateMaterials(GameObject visualRoot, string templatePrefabName)
    {
        if (visualRoot == null)
        {
            return;
        }

        GameObject templatePrefab = PrefabManager.Instance.GetPrefab(templatePrefabName);
        if (templatePrefab == null)
        {
            FletchersForgePlugin.Log?.LogWarning($"Template prefab '{templatePrefabName}' not found for material remap.");
            return;
        }

        Transform attach = templatePrefab.transform.Find("attach");
        Renderer templateRenderer = null;
        if (attach != null)
        {
            templateRenderer = attach.Find("mesh")?.GetComponent<Renderer>();
        }

        if (templateRenderer == null)
        {
            templateRenderer = PrefabPathUtility.FindRendererChild(templatePrefab.transform)?.GetComponent<Renderer>();
        }

        if (templateRenderer == null || templateRenderer.sharedMaterials == null || templateRenderer.sharedMaterials.Length == 0)
        {
            FletchersForgePlugin.Log?.LogWarning($"No renderer materials on template '{templatePrefabName}'.");
            return;
        }

        Material[] templateMaterials = templateRenderer.sharedMaterials;
        foreach (Renderer renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
            {
                continue;
            }

            var remapped = new Material[System.Math.Max(1, renderer.sharedMaterials.Length)];
            for (int i = 0; i < remapped.Length; i++)
            {
                remapped[i] = templateMaterials[System.Math.Min(i, templateMaterials.Length - 1)];
            }

            renderer.sharedMaterials = remapped;
            renderer.enabled = true;
        }
    }

    /// Copy materials from a source prefab (e.g. the leather pouch) onto another mesh.
    internal static void ApplyMaterialsFromSource(GameObject visualRoot, GameObject sourcePrefab)
    {
        if (visualRoot == null || sourcePrefab == null)
        {
            return;
        }

        Material sourceMaterial = null;
        foreach (Renderer sourceRenderer in sourcePrefab.GetComponentsInChildren<Renderer>(true))
        {
            if (sourceRenderer?.sharedMaterials == null)
            {
                continue;
            }

            foreach (Material material in sourceRenderer.sharedMaterials)
            {
                if (material != null)
                {
                    sourceMaterial = material;
                    break;
                }
            }

            if (sourceMaterial != null)
            {
                break;
            }
        }

        if (sourceMaterial == null)
        {
            FletchersForgePlugin.Log?.LogWarning($"No materials found on '{sourcePrefab.name}' to copy.");
            return;
        }

        foreach (Renderer renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
            {
                continue;
            }

            var remapped = new Material[System.Math.Max(1, renderer.sharedMaterials.Length)];
            for (int i = 0; i < remapped.Length; i++)
            {
                remapped[i] = sourceMaterial;
            }

            renderer.sharedMaterials = remapped;
            renderer.enabled = true;
        }
    }

    private static void RemoveVisualRoot(GameObject itemPrefab, string visualRootName)
    {
        Transform existing = itemPrefab.transform.Find(visualRootName);
        if (existing != null)
        {
            Object.Destroy(existing.gameObject);
        }

        Transform attach = itemPrefab.transform.Find("attach");
        if (attach != null)
        {
            Transform nested = attach.Find(visualRootName);
            if (nested != null)
            {
                Object.Destroy(nested.gameObject);
            }
        }
    }

    private static void StripLodGroup(GameObject root)
    {
        LODGroup lodGroup = root.GetComponent<LODGroup>();
        if (lodGroup != null)
        {
            Object.Destroy(lodGroup);
        }

        var toDestroy = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == null || child == root.transform)
            {
                continue;
            }

            string name = child.name;
            if (name.IndexOf("LOD1", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("LOD2", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("LOD3", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                toDestroy.Add(child.gameObject);
            }
        }

        foreach (GameObject go in toDestroy)
        {
            Object.Destroy(go);
        }
    }

    private static void DisableColliders(GameObject root)
    {
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
        {
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }

    private static void StripRigidbodies(GameObject root)
    {
        foreach (Rigidbody rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
        {
            if (rigidbody != null)
            {
                Object.Destroy(rigidbody);
            }
        }
    }
}
