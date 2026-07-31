using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace FletchersForge;

internal static class ItemRegistrar
{
    private static CustomItem knifeItem;

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

        RegisterKnife();
    }

    internal static void ApplyDeferredIcons()
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

        if (knifeItem != null)
        {
            GameObject rig = UnityEngine.Object.Instantiate(knifeItem.ItemDrop.gameObject);
            rig.name = "IconRig_FF_FletchersKnife";
            IconRigUtility.ApplyRenderedIcon(knifeItem, rig);
            UnityEngine.Object.Destroy(rig);
        }

        FletchersForgePlugin.Log?.LogInfo("Applied deferred item icons.");

        Player local = Player.m_localPlayer;
        if (local != null)
        {
            RefreshInventoryIcons(local);
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
        FletchersKnifeConfigurator.Configure(knifeItem.ItemDrop.m_itemData.m_shared);
        ApplyKnifeVisuals(knifeItem.ItemDrop.gameObject);
        ItemManager.Instance.AddItem(knifeItem);
        FletchersForgePlugin.Log?.LogInfo("Registered Fletcher's knife.");
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
