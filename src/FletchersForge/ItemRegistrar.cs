using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace FletchersForge;

internal static class ItemRegistrar
{
    private static CustomItem knifeItem;
    private static CustomItem quiverItem;

    internal static void RegisterAll()
    {
        ArrowAssemblyRegistry.Initialize();

        RegisterShaft(
            ModConstants.ShaftStandard,
            "$FF_ShaftStandard",
            "$FF_ShaftStandard_desc",
            ModConstants.ShaftWeight);

        RegisterShaft(
            ModConstants.ShaftNeedle,
            "$FF_ShaftNeedle",
            "$FF_ShaftNeedle_desc",
            ModConstants.ShaftWeight,
            "ArrowNeedle");

        RegisterShaft(
            ModConstants.ShaftAsh,
            "$FF_ShaftAsh",
            "$FF_ShaftAsh_desc",
            ModConstants.ShaftWeight,
            ashTint: true);

        RegisterHead(ModConstants.HeadFire, "$FF_HeadFire", "ArrowFire");
        RegisterHead(ModConstants.HeadFlint, "$FF_HeadFlint", "ArrowFlint");
        RegisterHead(ModConstants.HeadBronze, "$FF_HeadBronze", "ArrowBronze");
        RegisterHead(ModConstants.HeadIron, "$FF_HeadIron", "ArrowIron");
        RegisterHead(ModConstants.HeadSilver, "$FF_HeadSilver", "ArrowSilver");
        RegisterHead(ModConstants.HeadObsidian, "$FF_HeadObsidian", "ArrowObsidian");
        RegisterHead(ModConstants.HeadPoison, "$FF_HeadPoison", "ArrowPoison");
        RegisterHead(ModConstants.HeadFrost, "$FF_HeadFrost", "ArrowFrost");
        RegisterHead(ModConstants.HeadNeedle, "$FF_HeadNeedle", "ArrowNeedle");
        RegisterHead(ModConstants.HeadCarapace, "$FF_HeadCarapace", "ArrowCarapace");
        RegisterHead(ModConstants.HeadCharred, "$FF_HeadCharred", "ArrowCharred");

        try
        {
            RegisterKnife();
        }
        catch (System.Exception ex)
        {
            FletchersForgePlugin.Log?.LogError($"Failed to register Fletcher's knife: {ex}");
        }

        try
        {
            RegisterQuiver();
        }
        catch (System.Exception ex)
        {
            FletchersForgePlugin.Log?.LogError($"Failed to register Fletcher's quiver: {ex}");
        }
    }

    internal static void ApplyEmbeddedHeadIconsOnly()
    {
        HeadIconAssets.ClearCache();

        ApplyHeadIcon(ModConstants.HeadFire, "ArrowFire");
        ApplyHeadIcon(ModConstants.HeadFlint, "ArrowFlint");
        ApplyHeadIcon(ModConstants.HeadBronze, "ArrowBronze");
        ApplyHeadIcon(ModConstants.HeadIron, "ArrowIron");
        ApplyHeadIcon(ModConstants.HeadSilver, "ArrowSilver");
        ApplyHeadIcon(ModConstants.HeadObsidian, "ArrowObsidian");
        ApplyHeadIcon(ModConstants.HeadPoison, "ArrowPoison");
        ApplyHeadIcon(ModConstants.HeadFrost, "ArrowFrost");
        ApplyHeadIcon(ModConstants.HeadNeedle, "ArrowNeedle");
        ApplyHeadIcon(ModConstants.HeadCarapace, "ArrowCarapace");
        ApplyHeadIcon(ModConstants.HeadCharred, "ArrowCharred");
    }

    internal static void ApplyDeferredIcons()
    {
        try
        {
            HeadIconAssets.ClearCache();

            if (ModConfig.AllowExternalHeadIconOverrides.Value)
            {
                FletchersForgePlugin.Log?.LogInfo($"External head icon overrides enabled: {HeadIconAssets.GetIconsFolderPath()}");
            }

            ApplyWoodShaftIcon(ModConstants.ShaftStandard);
            ApplyWoodShaftIcon(ModConstants.ShaftAsh, ashTint: true);
            ApplyShaftIcon(ModConstants.ShaftNeedle, "ArrowNeedle");

            ApplyHeadIcon(ModConstants.HeadFire, "ArrowFire");
            ApplyHeadIcon(ModConstants.HeadFlint, "ArrowFlint");
            ApplyHeadIcon(ModConstants.HeadBronze, "ArrowBronze");
            ApplyHeadIcon(ModConstants.HeadIron, "ArrowIron");
            ApplyHeadIcon(ModConstants.HeadSilver, "ArrowSilver");
            ApplyHeadIcon(ModConstants.HeadObsidian, "ArrowObsidian");
            ApplyHeadIcon(ModConstants.HeadPoison, "ArrowPoison");
            ApplyHeadIcon(ModConstants.HeadFrost, "ArrowFrost");
            ApplyHeadIcon(ModConstants.HeadNeedle, "ArrowNeedle");
            ApplyHeadIcon(ModConstants.HeadCarapace, "ArrowCarapace");
            ApplyHeadIcon(ModConstants.HeadCharred, "ArrowCharred");

            ApplyDeferredRenderedIcon(knifeItem, "IconRig_FF_FletchersKnife");
            ApplyQuiverIcon(quiverItem);

            FletchersForgePlugin.Log?.LogInfo("Applied deferred item icons.");

            Player local = Player.m_localPlayer;
            if (local != null)
            {
                RefreshInventoryIcons(local);
            }
        }
        catch (System.Exception ex)
        {
            FletchersForgePlugin.Log?.LogError($"Deferred icon setup failed: {ex}");
        }
    }

    private static void RefreshInventoryIcons(Player player)
    {
        Inventory inventory = player.GetInventory();
        if (inventory == null)
        {
            return;
        }

        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (item?.m_dropPrefab == null || item.m_shared == null)
            {
                continue;
            }

            string prefabName = item.m_dropPrefab.name;
            if (!prefabName.StartsWith("FF_", System.StringComparison.Ordinal))
            {
                continue;
            }

            GameObject prefab = ObjectDB.instance.GetItemPrefab(prefabName);
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (drop?.m_itemData?.m_shared?.m_icons != null && IconRigUtility.HasUsableIcons(drop.m_itemData.m_shared.m_icons))
            {
                item.m_shared.m_icons = drop.m_itemData.m_shared.m_icons;
            }
        }
    }

    private static void RegisterShaft(
        string prefabName,
        string locName,
        string locDesc,
        float weight,
        string iconSourceArrow = "ArrowWood",
        bool ashTint = false)
    {
        var config = new ItemConfig
        {
            Name = locName,
            Description = locDesc,
        };

        var item = new CustomItem(prefabName, "IronScrap", config);
        ItemManager.Instance.AddItem(item);
        ApplyMaterialStats(item, weight, ModConstants.ShaftStackSize);
        ComponentDropVisualUtility.ApplyShaftDropVisual(item, iconSourceArrow, ashTint);
        FletchersForgePlugin.Log?.LogInfo($"Registered shaft {prefabName}.");
    }

    private static void RegisterHead(string prefabName, string locName, string iconSourceArrow)
    {
        var config = new ItemConfig
        {
            Name = locName,
            Description = locName,
        };

        var item = new CustomItem(prefabName, "IronScrap", config);
        ItemManager.Instance.AddItem(item);
        ApplyMaterialStats(item, ModConstants.HeadWeight, ModConstants.HeadStackSize);
        ComponentDropVisualUtility.ApplyHeadDropVisual(item, iconSourceArrow);
        FletchersForgePlugin.Log?.LogInfo($"Registered head {prefabName}.");
    }

    private static void RegisterKnife()
    {
        var config = new ItemConfig
        {
            Name = "$FF_FletchersKnife",
            Description = "$FF_FletchersKnife_desc",
        };

        knifeItem = new CustomItem(ModConstants.FletchersKnife, "KnifeBlackMetal", config);
        if (knifeItem?.ItemDrop?.m_itemData?.m_shared == null)
        {
            FletchersForgePlugin.Log?.LogError(
                "Failed to clone Fletcher's knife (prefab name already in use). Knife item was not registered.");
            knifeItem = null;
            return;
        }

        FletchersKnifeConfigurator.Configure(knifeItem.ItemDrop.m_itemData.m_shared);
        ApplyKnifeVisuals(knifeItem.ItemDrop.gameObject);
        ItemManager.Instance.AddItem(knifeItem);
        FletchersForgePlugin.Log?.LogInfo("Registered Fletcher's knife.");
    }

    private static void RegisterQuiver()
    {
        var config = new ItemConfig
        {
            Name = "$FF_Quiver",
            Description = "$FF_Quiver_desc",
        };

        quiverItem = new CustomItem(ModConstants.Quiver, "DeerHide", config);
        if (quiverItem?.ItemDrop?.m_itemData?.m_shared == null)
        {
            FletchersForgePlugin.Log?.LogError(
                "Failed to clone Fletcher's quiver (prefab name already in use). Quiver item was not registered.");
            quiverItem = null;
            return;
        }

        ApplyMaterialStats(quiverItem, ModConstants.QuiverWeight, 1);
        ComponentDropVisualUtility.ApplyQuiverDropVisual(quiverItem);
        ItemManager.Instance.AddItem(quiverItem);
        FletchersForgePlugin.Log?.LogInfo("Registered Fletcher's quiver.");
    }

    private static void ApplyDeferredRenderedIcon(CustomItem item, string rigName)
    {
        if (item?.ItemDrop == null)
        {
            return;
        }

        GameObject rig;
        IconRigGuard.Enter();
        try
        {
            rig = UnityEngine.Object.Instantiate(item.ItemDrop.gameObject);
            rig.name = rigName;
        }
        finally
        {
            IconRigGuard.Leave();
        }

        IconRigUtility.ApplyRenderedIcon(item, rig);
    }

    private static void ApplyQuiverIcon(CustomItem item)
    {
        if (item?.ItemDrop == null)
        {
            return;
        }

        GameObject quiverPrefab = AssetBundleLoader.QuiverPrefab;
        if (quiverPrefab == null)
        {
            FletchersForgePlugin.Log?.LogWarning("FF_Quiver bundle prefab missing; quiver icon skipped.");
            return;
        }

        GameObject rig;
        IconRigGuard.Enter();
        try
        {
            rig = UnityEngine.Object.Instantiate(quiverPrefab);
            rig.name = "IconRig_FF_Quiver";
        }
        finally
        {
            IconRigGuard.Leave();
        }

        CustomVisualUtility.PrepareBundledInstance(rig);
        CustomVisualUtility.ApplyMaterialsFromSource(rig, AssetBundleLoader.HeadPouchPrefab);
        IconRigUtility.ApplyRenderedIcon(item, rig);
    }

    private static void ApplyWoodShaftIcon(string prefabName, bool ashTint = false)
    {
        CustomItem item = ItemManager.Instance.GetItem(prefabName);
        if (item == null)
        {
            return;
        }

        GameObject rig = IconRigUtility.BuildShaftIconRig("ArrowWood", ashTint);
        if (rig == null)
        {
            return;
        }

        IconRigUtility.ApplyRenderedIcon(item, rig);
        UnityEngine.Object.Destroy(rig);
        IconRigUtility.SyncIconsToObjectDb(prefabName, item.ItemDrop.m_itemData.m_shared.m_icons);
    }

    private static void ApplyShaftIcon(string prefabName, string sourceArrow)
    {
        CustomItem item = ItemManager.Instance.GetItem(prefabName);
        if (item == null)
        {
            return;
        }

        GameObject rig = IconRigUtility.BuildShaftIconRig(sourceArrow, ashTint: false);
        if (rig == null)
        {
            return;
        }

        IconRigUtility.ApplyRenderedIcon(item, rig);
        UnityEngine.Object.Destroy(rig);
        IconRigUtility.SyncIconsToObjectDb(prefabName, item.ItemDrop.m_itemData.m_shared.m_icons);
    }

    private static void ApplyHeadIcon(string prefabName, string sourceArrow)
    {
        CustomItem item = ItemManager.Instance.GetItem(prefabName);
        if (item == null)
        {
            return;
        }

        IconRigUtility.ApplyHeadIconFromTip(item, sourceArrow);
    }

    private static void ApplyMaterialStats(CustomItem item, float weight, int stackSize)
    {
        var shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
        shared.m_weight = weight;
        shared.m_maxStackSize = stackSize;
        shared.m_teleportable = true;
        shared.m_value = 0;
    }

    private static void ApplyKnifeVisuals(GameObject knifePrefab)
    {
        CustomVisualUtility.RemoveVisualRootFromItem(knifePrefab, "FF_KnifeVisual");

        GameObject bundledKnife = AssetBundleLoader.KnifePrefab;
        if (bundledKnife == null || !TryApplyBundledKnifeVisual(knifePrefab, bundledKnife))
        {
            if (bundledKnife == null)
            {
                FletchersForgePlugin.Log?.LogWarning(
                    "FF_FletchersKnife bundle prefab missing; using kitbash blade.");
            }

            ApplyKnifeKitbashVisuals(knifePrefab);
        }

        EnsureKnifeDropPhysics(knifePrefab);
    }

    private static bool TryApplyBundledKnifeVisual(GameObject knifePrefab, GameObject bundledKnife)
    {
        const string visualName = "FF_KnifeVisual";
        Transform attach = knifePrefab.transform.Find("attach");
        if (attach == null)
        {
            FletchersForgePlugin.Log?.LogWarning("Fletcher's knife has no attach point; cannot apply bundle mesh.");
            return false;
        }

        RemoveKnifeVisualChildren(attach);

        GameObject visual = UnityEngine.Object.Instantiate(bundledKnife, attach);
        visual.name = visualName;
        Transform vanillaMesh = FindVanillaKnifeMesh(attach);
        MatchVanillaKnifeBladeAxis(visual.transform, vanillaMesh);

        CustomVisualUtility.PrepareBundledInstance(visual);
        AlignHandleToAttach(visual);

        MeshFilter meshFilter = visual.GetComponentInChildren<MeshFilter>(true);
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            UnityEngine.Object.Destroy(visual);
            FletchersForgePlugin.Log?.LogWarning(
                "Fletcher's knife AssetBundle visual has no mesh; falling back to kitbash.");
            return false;
        }

        HideVanillaAttachChildren(attach, visualName);
        FletchersForgePlugin.Log?.LogInfo(
            $"Applied Fletcher's knife AssetBundle visual (scale {ModConstants.KnifeVisualMeshScale:0.###}).");
        return true;
    }

    private static Transform FindVanillaKnifeMesh(Transform attach)
    {
        Transform vanillaMesh = attach.Find("mesh");
        if (vanillaMesh != null)
        {
            return vanillaMesh;
        }

        return PrefabPathUtility.FindRendererChild(attach);
    }

    /// Point our longest mesh axis the same way as KnifeBlackMetal's attach/mesh.
    private static void MatchVanillaKnifeBladeAxis(Transform visual, Transform vanillaMesh)
    {
        visual.localPosition = vanillaMesh != null ? vanillaMesh.localPosition : Vector3.zero;
        visual.localRotation = vanillaMesh != null ? vanillaMesh.localRotation : Quaternion.identity;
        visual.localScale = Vector3.one * ModConstants.KnifeVisualMeshScale;

        if (vanillaMesh == null)
        {
            return;
        }

        Mesh vanillaMeshData = vanillaMesh.GetComponent<MeshFilter>()?.sharedMesh;
        MeshFilter ourFilter = visual.GetComponentInChildren<MeshFilter>(true);
        if (vanillaMeshData == null || ourFilter?.sharedMesh == null)
        {
            return;
        }

        Vector3 vanillaDir = vanillaMesh.TransformDirection(LocalLongestAxis(vanillaMeshData)).normalized;
        Vector3 ourDir = ourFilter.transform.TransformDirection(LocalLongestAxis(ourFilter.sharedMesh)).normalized;
        if (vanillaDir.sqrMagnitude < 0.01f || ourDir.sqrMagnitude < 0.01f)
        {
            return;
        }

        visual.rotation = Quaternion.FromToRotation(ourDir, vanillaDir) * visual.rotation;
    }

    private static Vector3 LocalLongestAxis(Mesh mesh)
    {
        Vector3 size = mesh.bounds.size;
        if (size.x >= size.y && size.x >= size.z)
        {
            return Vector3.right;
        }

        if (size.y >= size.z)
        {
            return Vector3.up;
        }

        return Vector3.forward;
    }

    private static void AlignHandleToAttach(GameObject visual)
    {
        Transform attach = visual.transform.parent;
        if (attach == null)
        {
            return;
        }

        Transform handle = FindNamedChild(visual.transform, "FF_Handle");
        Transform blade = FindNamedChild(visual.transform, "FF_Blade");
        MeshFilter handleFilter = handle != null ? handle.GetComponent<MeshFilter>() : null;
        if (handleFilter == null || handleFilter.sharedMesh == null)
        {
            return;
        }

        Vector3 handleCenter = handle.TransformPoint(handleFilter.sharedMesh.bounds.center);
        Vector3 toPommel = handle.forward;
        MeshFilter bladeFilter = blade != null ? blade.GetComponent<MeshFilter>() : null;
        if (bladeFilter != null && bladeFilter.sharedMesh != null)
        {
            Vector3 bladeCenter = blade.TransformPoint(bladeFilter.sharedMesh.bounds.center);
            Vector3 towardHandle = handleCenter - bladeCenter;
            if (towardHandle.sqrMagnitude > 0.000001f)
            {
                toPommel = towardHandle.normalized;
            }
        }

        Vector3 handleSize = Vector3.Scale(handleFilter.sharedMesh.bounds.size, handle.lossyScale);
        float handleLength = Mathf.Max(handleSize.x, Mathf.Max(handleSize.y, handleSize.z));
        Vector3 gripPoint = handleCenter + toPommel * (handleLength * ModConstants.KnifeVisualHandleShift);

        Vector3 gripInAttach = attach.InverseTransformPoint(gripPoint);
        visual.transform.localPosition -= gripInAttach;
    }

    private static Transform FindNamedChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    private static void RemoveKnifeVisualChildren(Transform attach)
    {
        Transform existingVisual = attach.Find("FF_KnifeVisual");
        if (existingVisual != null)
        {
            UnityEngine.Object.Destroy(existingVisual.gameObject);
        }

        Transform kitbashBlade = attach.Find("FF_AbyssalBlade");
        if (kitbashBlade != null)
        {
            UnityEngine.Object.Destroy(kitbashBlade.gameObject);
        }
    }

    /// Vanilla knives often put the MeshCollider on attach/mesh. Hiding that mesh
    /// removes ground collision, so the thrown item falls through the world.
    private static void EnsureKnifeDropPhysics(GameObject knifePrefab)
    {
        foreach (Collider collider in knifePrefab.GetComponentsInChildren<Collider>(true))
        {
            if (collider != null && collider.transform != knifePrefab.transform)
            {
                collider.enabled = false;
            }
        }

        foreach (Collider collider in knifePrefab.GetComponents<Collider>())
        {
            if (collider != null && collider is not BoxCollider)
            {
                UnityEngine.Object.Destroy(collider);
            }
        }

        BoxCollider box = knifePrefab.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = knifePrefab.AddComponent<BoxCollider>();
        }

        Bounds localBounds = GetKnifeVisualLocalBounds(knifePrefab);
        Vector3 size = localBounds.size;
        size.x = Mathf.Max(size.x, ModConstants.KnifeDropColliderMinSize);
        size.y = Mathf.Max(size.y, ModConstants.KnifeDropColliderMinSize);
        size.z = Mathf.Max(size.z, ModConstants.KnifeDropColliderMinSize);

        Vector3 center = localBounds.center;
        float bottom = center.y - (size.y * 0.5f);
        if (bottom < 0.02f)
        {
            center.y += 0.02f - bottom;
        }

        box.enabled = true;
        box.isTrigger = false;
        box.size = size;
        box.center = center;

        Rigidbody rigidbody = knifePrefab.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = knifePrefab.AddComponent<Rigidbody>();
        }

        rigidbody.isKinematic = false;
        rigidbody.useGravity = true;
        rigidbody.mass = 1.5f;
        rigidbody.linearDamping = 1f;
        rigidbody.angularDamping = 3f;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private static Bounds GetKnifeVisualLocalBounds(GameObject knifePrefab)
    {
        var bounds = new Bounds(Vector3.zero, Vector3.one * ModConstants.KnifeDropColliderMinSize);
        bool hasBounds = false;

        foreach (MeshFilter filter in knifePrefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter?.sharedMesh == null || !IsKnifeDropVisualMesh(filter.transform))
            {
                continue;
            }

            EncapsulateMeshInPrefabSpace(knifePrefab.transform, filter, ref bounds, ref hasBounds);
        }

        return bounds;
    }

    private static bool IsKnifeDropVisualMesh(Transform meshTransform)
    {
        Transform current = meshTransform;
        while (current != null)
        {
            if (current.name == "FF_KnifeVisual" || current.name == "FF_AbyssalBlade")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static void EncapsulateMeshInPrefabSpace(
        Transform prefab,
        MeshFilter filter,
        ref Bounds bounds,
        ref bool hasBounds)
    {
        Bounds meshBounds = filter.sharedMesh.bounds;
        Vector3 min = meshBounds.min;
        Vector3 max = meshBounds.max;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new Vector3(
                (i & 1) == 0 ? min.x : max.x,
                (i & 2) == 0 ? min.y : max.y,
                (i & 4) == 0 ? min.z : max.z);
            Vector3 prefabLocal = prefab.InverseTransformPoint(filter.transform.TransformPoint(corner));
            if (!hasBounds)
            {
                bounds = new Bounds(prefabLocal, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(prefabLocal);
            }
        }
    }

    private static void HideVanillaAttachChildren(Transform attach, string keepChildName)
    {
        foreach (Transform child in attach)
        {
            if (child.name == keepChildName)
            {
                continue;
            }

            child.gameObject.SetActive(false);
        }
    }

    private static void ApplyKnifeKitbashVisuals(GameObject knifePrefab)
    {
        RestoreKnifeRenderers(knifePrefab);

        Transform attach = knifePrefab.transform.Find("attach");
        if (attach == null)
        {
            FletchersForgePlugin.Log?.LogWarning("Fletcher's knife has no attach point; kitbash skipped.");
            return;
        }

        HideBlackmetalBlade(attach);

        GameObject chitinPrefab = PrefabManager.Instance.GetPrefab("KnifeChitin");
        if (chitinPrefab == null)
        {
            FletchersForgePlugin.Log?.LogWarning("KnifeChitin prefab not found; kitbash skipped.");
            return;
        }

        Transform bladeSource = PrefabPathUtility.FindFirstChild(
            chitinPrefab.transform,
            "attach_skin",
            "attach",
            "attach/mesh");

        if (bladeSource == null)
        {
            bladeSource = PrefabPathUtility.FindRendererChild(chitinPrefab.transform);
        }

        if (bladeSource == null)
        {
            FletchersForgePlugin.Log?.LogWarning("Could not find Abyssal razor blade mesh for kitbash.");
            return;
        }

        GameObject blade = UnityEngine.Object.Instantiate(bladeSource.gameObject, attach);
        blade.name = "FF_AbyssalBlade";
        blade.transform.localPosition = Vector3.zero;
        blade.transform.localRotation = Quaternion.identity;
        blade.transform.localScale = Vector3.one * 0.75f;
        TintBladeCopper(blade.transform);
        FletchersForgePlugin.Log?.LogInfo("Applied Fletcher's knife blade kitbash.");
    }

    private static void RestoreKnifeRenderers(GameObject knifePrefab)
    {
        Transform attach = knifePrefab.transform.Find("attach");
        if (attach != null)
        {
            foreach (Transform child in attach)
            {
                child.gameObject.SetActive(true);
            }
        }

        foreach (Renderer renderer in knifePrefab.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
    }

    private static void HideBlackmetalBlade(Transform attach)
    {
        Transform blade = attach.Find("mesh");
        if (blade != null && blade.name != "FF_AbyssalBlade")
        {
            blade.gameObject.SetActive(false);
            return;
        }

        foreach (Transform child in attach)
        {
            if (child.name == "FF_AbyssalBlade" || child.name.ToLowerInvariant().Contains("handle"))
            {
                continue;
            }

            if (child.GetComponent<Renderer>() != null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private static void TintKnifeVisualCopper(Transform visualRoot)
    {
        Color copper = new Color(0.72f, 0.45f, 0.20f, 1f);
        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", copper);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", copper);
            }
        }
    }

    private static void TintBladeCopper(Transform bladeRoot)
    {
        Color copper = new Color(0.72f, 0.45f, 0.20f, 1f);
        Renderer[] renderers = bladeRoot.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", copper);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", copper);
            }
        }
    }
}
