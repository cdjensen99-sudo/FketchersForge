using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace FletchersForge;

internal static class ComponentDropVisualUtility
{
    private static GameObject GetBoxPrefab()
    {
        GameObject prefab = PrefabManager.Instance.GetPrefab(ModConstants.HeadDropBoxPrimaryPrefab);
        if (prefab != null)
        {
            return prefab;
        }

        foreach (string prefabName in ModConstants.HeadDropBoxPrefabFallbacks)
        {
            prefab = PrefabManager.Instance.GetPrefab(prefabName);
            if (prefab != null)
            {
                return prefab;
            }
        }

        return null;
    }

    internal static void ApplyHeadDropVisual(CustomItem item, string sourceArrow)
    {
        if (item?.ItemDrop == null)
        {
            return;
        }

        if (TryApplyHeadPouchVisual(item.ItemDrop.gameObject))
        {
            return;
        }

        ApplyShipwreckBoxVisual(item.ItemDrop.gameObject);
    }

    private static bool TryApplyHeadPouchVisual(GameObject dropPrefab)
    {
        GameObject pouchPrefab = AssetBundleLoader.HeadPouchPrefab;
        if (pouchPrefab == null)
        {
            return false;
        }

        RemoveDropVisual(dropPrefab);

        GameObject visualRoot = new GameObject("FF_DropVisual");
        visualRoot.transform.SetParent(dropPrefab.transform, false);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale = Vector3.one * ModConstants.HeadDropPouchScale;

        GameObject pouchInstance = UnityEngine.Object.Instantiate(pouchPrefab, visualRoot.transform);
        pouchInstance.name = "FF_HeadPouchVisual";
        pouchInstance.transform.localPosition = Vector3.zero;
        pouchInstance.transform.localRotation = Quaternion.identity;
        pouchInstance.transform.localScale = Vector3.one;

        StripLodHelpers(pouchInstance);
        DisableVisualColliders(pouchInstance);
        // Do not AlignDropVisualToGround for the pouch — it can bury the mesh under the terrain.
        DisableVanillaDropRenderers(dropPrefab);
        EnsurePouchDropPhysics(dropPrefab);

        FletchersForgePlugin.Log?.LogInfo(
            $"Applied leather pouch drop visual (scale {ModConstants.HeadDropPouchScale:0.##}) for {dropPrefab.name}.");
        return true;
    }

    internal static void ApplyQuiverDropVisual(CustomItem item)
    {
        if (item?.ItemDrop == null)
        {
            return;
        }

        GameObject dropPrefab = item.ItemDrop.gameObject;
        GameObject quiverPrefab = AssetBundleLoader.QuiverPrefab;
        if (quiverPrefab == null)
        {
            FletchersForgePlugin.Log?.LogWarning("FF_Quiver bundle prefab missing; keeping deer hide drop mesh.");
            return;
        }

        RemoveDropVisual(dropPrefab);

        GameObject visualRoot = new GameObject("FF_DropVisual");
        visualRoot.transform.SetParent(dropPrefab.transform, false);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale = Vector3.one * ModConstants.QuiverDropScale;

        GameObject quiverInstance = UnityEngine.Object.Instantiate(quiverPrefab, visualRoot.transform);
        quiverInstance.name = "FF_QuiverVisual";
        quiverInstance.transform.localPosition = Vector3.zero;
        quiverInstance.transform.localRotation = Quaternion.identity;
        quiverInstance.transform.localScale = Vector3.one;

        CustomVisualUtility.PrepareBundledInstance(quiverInstance);
        CustomVisualUtility.ApplyMaterialsFromSource(quiverInstance, AssetBundleLoader.HeadPouchPrefab);
        DisableVanillaDropRenderers(dropPrefab);
        EnsurePouchDropPhysics(dropPrefab);
        FletchersForgePlugin.Log?.LogInfo(
            $"Applied quiver drop visual (scale {ModConstants.QuiverDropScale:0.##}) for {dropPrefab.name}.");
    }

    private static void StripLodHelpers(GameObject pouchRoot)
    {
        LODGroup lodGroup = pouchRoot.GetComponent<LODGroup>();
        if (lodGroup != null)
        {
            UnityEngine.Object.Destroy(lodGroup);
        }

        var toDestroy = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in pouchRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child == null || child == pouchRoot.transform)
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
            UnityEngine.Object.Destroy(go);
        }
    }

    private static void DisableVisualColliders(GameObject visualRoot)
    {
        foreach (Collider collider in visualRoot.GetComponentsInChildren<Collider>(true))
        {
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }

    private static void EnsurePouchDropPhysics(GameObject dropPrefab)
    {
        // Spheres roll forever on Valheim terrain; use a box so the pouch settles like scrap/ore.
        foreach (Collider collider in dropPrefab.GetComponentsInChildren<Collider>(true))
        {
            // Keep only root colliders we control; visual colliders are already disabled.
            if (collider.transform != dropPrefab.transform)
            {
                collider.enabled = false;
            }
        }

        SphereCollider sphere = dropPrefab.GetComponent<SphereCollider>();
        if (sphere != null)
        {
            UnityEngine.Object.Destroy(sphere);
        }

        BoxCollider box = dropPrefab.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = dropPrefab.AddComponent<BoxCollider>();
        }

        float size = ModConstants.HeadDropPouchColliderRadius * ModConstants.HeadDropPouchScale * 2f;
        box.enabled = true;
        box.isTrigger = false;
        box.size = new Vector3(size, size * 0.85f, size);
        box.center = new Vector3(0f, box.size.y * 0.5f, 0f);

        Rigidbody rigidbody = dropPrefab.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = dropPrefab.AddComponent<Rigidbody>();
        }

        rigidbody.isKinematic = false;
        rigidbody.useGravity = true;
        rigidbody.mass = 2f;
        rigidbody.linearDamping = 1.5f;
        rigidbody.angularDamping = 8f;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // Stop endless tumble; allow a short slide then settle.
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    internal static void ApplyShaftDropVisual(CustomItem item, string sourceArrow, bool ashTint = false)
    {
        if (item?.ItemDrop == null)
        {
            return;
        }

        ApplyArrowPartDropVisual(item.ItemDrop.gameObject, sourceArrow, isHead: false, ashTint);
    }

    private static void ApplyShipwreckBoxVisual(GameObject dropPrefab)
    {
        GameObject boxPrefab = GetBoxPrefab();
        if (boxPrefab == null)
        {
            FletchersForgePlugin.Log?.LogWarning("No box prefab found for head drop visual; keeping iron scrap mesh.");
            return;
        }

        RemoveDropVisual(dropPrefab);

        GameObject visualRoot = new GameObject("FF_DropVisual");
        visualRoot.transform.SetParent(dropPrefab.transform, false);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale = Vector3.one * ModConstants.HeadDropBoxScale;

        bool copied = CopyMeshHierarchyFromRoot(boxPrefab.transform, visualRoot.transform);
        if (!copied)
        {
            UnityEngine.Object.Destroy(visualRoot);
            EnableVanillaDropRenderers(dropPrefab);
            FletchersForgePlugin.Log?.LogWarning(
                $"No box mesh copied for {dropPrefab.name}; keeping iron scrap mesh.");
            return;
        }

        AlignDropVisualToGround(visualRoot.transform);

        DisableVanillaDropRenderers(dropPrefab);
        EnsureDropPhysics(dropPrefab);
        FletchersForgePlugin.Log?.LogInfo(
            $"Applied box drop visual ({ModConstants.HeadDropBoxScale:P0}) for {dropPrefab.name} from '{boxPrefab.name}'.");
    }

    private static void ApplyArrowPartDropVisual(
        GameObject dropPrefab,
        string sourceArrow,
        bool isHead,
        bool ashTint = false)
    {
        GameObject rig = isHead
            ? IconRigUtility.BuildHeadIconRig(sourceArrow, 1f)
            : IconRigUtility.BuildShaftIconRig(sourceArrow, ashTint);

        if (rig == null)
        {
            FletchersForgePlugin.Log?.LogWarning($"Could not build drop visual for {dropPrefab.name} from {sourceArrow}.");
            EnableVanillaDropRenderers(dropPrefab);
            return;
        }

        bool copied = TryCopyMeshVisual(dropPrefab, rig, isHead);
        UnityEngine.Object.Destroy(rig);

        if (!copied)
        {
            EnableVanillaDropRenderers(dropPrefab);
            FletchersForgePlugin.Log?.LogWarning($"No mesh copied for drop visual on {dropPrefab.name}.");
            return;
        }

        DisableVanillaDropRenderers(dropPrefab);
        EnsureDropPhysics(dropPrefab);
        FletchersForgePlugin.Log?.LogInfo($"Applied drop visual for {dropPrefab.name}.");
    }

    private static void RemoveDropVisual(GameObject dropPrefab)
    {
        Transform existing = dropPrefab.transform.Find("FF_DropVisual");
        if (existing != null)
        {
            UnityEngine.Object.Destroy(existing.gameObject);
        }
    }

    private static bool TryCopyMeshVisual(GameObject dropPrefab, GameObject rig, bool isHead)
    {
        RemoveDropVisual(dropPrefab);

        GameObject visualRoot = new GameObject("FF_DropVisual");
        visualRoot.transform.SetParent(dropPrefab.transform, false);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        visualRoot.transform.localScale = Vector3.one * (isHead ? 0.28f : 0.22f);

        bool copiedAny = false;
        foreach (Renderer renderer in rig.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
            {
                continue;
            }

            if (CopyRendererMesh(renderer, visualRoot.transform))
            {
                copiedAny = true;
            }
        }

        if (!copiedAny)
        {
            UnityEngine.Object.Destroy(visualRoot);
        }

        return copiedAny;
    }

    private static bool CopyMeshHierarchyFromRoot(Transform sourceRoot, Transform destRoot)
    {
        bool copiedAny = false;
        foreach (Renderer renderer in sourceRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
            {
                continue;
            }

            if (CopyRendererMeshRelative(renderer, sourceRoot, destRoot))
            {
                copiedAny = true;
            }
        }

        return copiedAny;
    }

    private static bool CopyRendererMeshRelative(Renderer source, Transform sourceRoot, Transform destRoot)
    {
        Mesh mesh = null;
        if (source is MeshRenderer)
        {
            MeshFilter meshFilter = source.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                mesh = meshFilter.sharedMesh;
            }
        }
        else if (source is SkinnedMeshRenderer skinnedMeshRenderer)
        {
            mesh = skinnedMeshRenderer.sharedMesh;
        }

        if (mesh == null)
        {
            return false;
        }

        GameObject part = new GameObject(source.name);
        part.transform.SetParent(destRoot, false);
        part.transform.localPosition = sourceRoot.InverseTransformPoint(source.transform.position);
        part.transform.localRotation = Quaternion.Inverse(sourceRoot.rotation) * source.transform.rotation;

        Vector3 sourceScale = source.transform.lossyScale;
        Vector3 rootScale = sourceRoot.lossyScale;
        part.transform.localScale = new Vector3(
            SafeDivide(sourceScale.x, rootScale.x),
            SafeDivide(sourceScale.y, rootScale.y),
            SafeDivide(sourceScale.z, rootScale.z));

        MeshFilter newFilter = part.AddComponent<MeshFilter>();
        newFilter.sharedMesh = mesh;

        MeshRenderer newRenderer = part.AddComponent<MeshRenderer>();
        newRenderer.sharedMaterials = source.sharedMaterials;

        return true;
    }

    private static bool CopyRendererMesh(Renderer source, Transform parent)
    {
        Mesh mesh = null;
        if (source is MeshRenderer)
        {
            MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
            if (sourceFilter != null)
            {
                mesh = sourceFilter.sharedMesh;
            }
        }
        else if (source is SkinnedMeshRenderer skinnedMeshRenderer)
        {
            mesh = skinnedMeshRenderer.sharedMesh;
        }

        if (mesh == null)
        {
            return false;
        }

        GameObject part = new GameObject(source.name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = source.transform.localPosition;
        part.transform.localRotation = source.transform.localRotation;
        part.transform.localScale = source.transform.localScale;

        MeshFilter meshFilter = part.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = part.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = source.sharedMaterials;

        return true;
    }

    private static void DisableVanillaDropRenderers(GameObject dropPrefab)
    {
        Transform dropVisual = dropPrefab.transform.Find("FF_DropVisual");
        foreach (Renderer renderer in dropPrefab.GetComponentsInChildren<Renderer>(true))
        {
            if (dropVisual != null && renderer.transform.IsChildOf(dropVisual))
            {
                continue;
            }

            renderer.enabled = false;
        }
    }

    private static void EnableVanillaDropRenderers(GameObject dropPrefab)
    {
        Transform dropVisual = dropPrefab.transform.Find("FF_DropVisual");
        foreach (Renderer renderer in dropPrefab.GetComponentsInChildren<Renderer>(true))
        {
            if (dropVisual != null && renderer.transform.IsChildOf(dropVisual))
            {
                continue;
            }

            renderer.enabled = true;
        }
    }

    private static void EnsureDropPhysics(GameObject dropPrefab)
    {
        foreach (Collider collider in dropPrefab.GetComponentsInChildren<Collider>(true))
        {
            if (collider != null)
            {
                collider.enabled = true;
            }
        }

        Rigidbody rigidbody = dropPrefab.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.isKinematic = false;
            rigidbody.useGravity = true;
        }

        if (dropPrefab.GetComponent<Collider>() == null)
        {
            SphereCollider sphere = dropPrefab.AddComponent<SphereCollider>();
            sphere.radius = 0.18f * ModConstants.HeadDropBoxScale;
            sphere.center = Vector3.zero;
        }
    }

    private static void AlignDropVisualToGround(Transform visualRoot)
    {
        float minLocalY = float.MaxValue;
        foreach (Renderer renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
        {
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            Bounds meshBounds = meshFilter.sharedMesh.bounds;
            Vector3 scaledExtents = Vector3.Scale(meshBounds.extents, renderer.transform.localScale);
            Vector3 localCenter = renderer.transform.localPosition + Vector3.Scale(meshBounds.center, renderer.transform.localScale);
            float bottomY = localCenter.y - scaledExtents.y;
            minLocalY = Mathf.Min(minLocalY, bottomY);
        }

        if (minLocalY < float.MaxValue && !Mathf.Approximately(minLocalY, 0f))
        {
            visualRoot.localPosition = new Vector3(0f, -minLocalY, 0f);
        }
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Approximately(divisor, 0f) ? value : value / divisor;
    }
}
