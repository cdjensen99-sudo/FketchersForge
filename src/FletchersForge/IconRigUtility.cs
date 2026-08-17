using System;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace FletchersForge;

internal static class IconRigUtility
{
    private static readonly string[] HeadNameTokens =
    {
        "head", "tip", "flint", "iron", "bronze", "silver", "obsidian",
        "fire", "poison", "frost", "needle", "carapace", "charred", "resin", "stone", "metal", "arrowhead",
    };

    private static readonly string[] ShaftNameTokens =
    {
        "shaft", "stick", "wood", "body", "arrowwood",
    };

    private static readonly string[] FeatherNameTokens =
    {
        "feather", "fletch", "vane",
    };

    internal static void ApplyShaftIcon(CustomItem item, string sourceArrow, bool ashTint = false)
    {
        GameObject rig = BuildShaftIconRig(sourceArrow, ashTint);
        ApplyRenderedIcon(item, rig);
    }

    internal static void ApplyHeadIcon(CustomItem item, string sourceArrow)
    {
        GameObject rig = BuildHeadIconRig(sourceArrow);
        ApplyRenderedIcon(item, rig);
    }

    internal static void ApplyRenderedIcon(CustomItem item, GameObject target)
    {
        if (item?.ItemDrop == null || target == null)
        {
            return;
        }

        try
        {
            Sprite sprite = RenderManager.Instance.Render(target, RenderManager.IsometricRotation);
            if (sprite != null)
            {
                item.ItemDrop.m_itemData.m_shared.m_icons = new[] { sprite };
                SyncIconsToObjectDb(item.ItemPrefab.name, item.ItemDrop.m_itemData.m_shared.m_icons);
            }
            else
            {
                FletchersForgePlugin.Log?.LogWarning($"Icon render returned null for {target.name}.");
            }
        }
        catch (Exception ex)
        {
            FletchersForgePlugin.Log?.LogWarning($"Icon render failed for {target.name}: {ex.Message}");
        }
        finally
        {
            if (target != item.ItemDrop.gameObject)
            {
                UnityEngine.Object.Destroy(target);
            }
        }
    }

    internal static bool HasUsableIcons(Sprite[] icons)
    {
        if (icons == null || icons.Length == 0)
        {
            return false;
        }

        foreach (Sprite icon in icons)
        {
            if (icon != null && icon.texture != null)
            {
                return true;
            }
        }

        return false;
    }

    internal static void ApplyHeadIconFromTip(CustomItem item, string sourceArrow)
    {
        if (HeadIconAssets.TryApplyCustomHeadIcon(item))
        {
            return;
        }

        Sprite generated = HeadIconGenerator.CreateForArrow(sourceArrow);
        if (generated != null)
        {
            item.ItemDrop.m_itemData.m_shared.m_icons = new[] { generated };
            SyncIconsToObjectDb(item.ItemPrefab.name, item.ItemDrop.m_itemData.m_shared.m_icons);
            return;
        }

        if (ApplyCroppedArrowTipIcon(item, sourceArrow))
        {
            return;
        }

        GameObject rig = BuildHeadIconRig(sourceArrow);
        if (rig != null)
        {
            ApplyRenderedIcon(item, rig);
            SyncIconsToObjectDb(item.ItemPrefab.name, item.ItemDrop.m_itemData.m_shared.m_icons);
        }
    }

    internal static bool ApplyCroppedArrowTipIcon(CustomItem item, string sourceArrow)
    {
        if (item?.ItemDrop == null)
        {
            return false;
        }

        GameObject prefab = ObjectDB.instance != null
            ? ObjectDB.instance.GetItemPrefab(sourceArrow)
            : PrefabManager.Instance.GetPrefab(sourceArrow);

        ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
        if (drop?.m_itemData?.m_shared?.m_icons == null || drop.m_itemData.m_shared.m_icons.Length == 0)
        {
            FletchersForgePlugin.Log?.LogWarning($"No arrow icon to crop for {sourceArrow}.");
            return false;
        }

        Sprite cropped = CropSpriteToArrowTip(drop.m_itemData.m_shared.m_icons[0], sourceArrow);
        if (cropped == null)
        {
            return false;
        }

        item.ItemDrop.m_itemData.m_shared.m_icons = new[] { cropped };
        SyncIconsToObjectDb(item.ItemPrefab.name, item.ItemDrop.m_itemData.m_shared.m_icons);
        return true;
    }

    internal static Sprite CropSpriteToArrowTip(Sprite source, string sourceArrow)
    {
        if (source == null || source.texture == null)
        {
            return null;
        }

        Rect rect = source.rect;
        bool needle = sourceArrow != null && sourceArrow.IndexOf("Needle", System.StringComparison.OrdinalIgnoreCase) >= 0;

        float tipStartX = needle ? 0.52f : 0.42f;
        float tipStartY = needle ? 0.22f : 0.30f;
        float tipWidth = needle ? 0.46f : 0.56f;
        float tipHeight = needle ? 0.72f : 0.62f;

        Rect crop = new Rect(
            rect.x + rect.width * tipStartX,
            rect.y + rect.height * tipStartY,
            rect.width * tipWidth,
            rect.height * tipHeight);

        return Sprite.Create(
            source.texture,
            crop,
            new Vector2(0.5f, 0.5f),
            source.pixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
    }

    internal static void SyncIconsToObjectDb(string prefabName, Sprite[] icons)
    {
        if (ObjectDB.instance == null || icons == null || icons.Length == 0)
        {
            return;
        }

        GameObject objectDbPrefab = ObjectDB.instance.GetItemPrefab(prefabName);
        ItemDrop drop = objectDbPrefab != null ? objectDbPrefab.GetComponent<ItemDrop>() : null;
        if (drop?.m_itemData?.m_shared != null)
        {
            drop.m_itemData.m_shared.m_icons = icons;
        }
    }

    internal static GameObject BuildShaftIconRig(string sourceArrow, bool ashTint)
    {
        GameObject prefab = PrefabManager.Instance.GetPrefab(sourceArrow);
        if (prefab == null)
        {
            return null;
        }

        GameObject rig;
        IconRigGuard.Enter();
        try
        {
            rig = UnityEngine.Object.Instantiate(prefab);
            rig.name = $"IconRig_Shaft_{sourceArrow}";
            PrepareIconRig(rig);
        }
        finally
        {
            IconRigGuard.Leave();
        }

        foreach (Transform child in rig.GetComponentsInChildren<Transform>(true))
        {
            if (child == rig.transform)
            {
                continue;
            }

            if (IsHeadPart(child.name))
            {
                child.gameObject.SetActive(false);
            }
        }

        if (ashTint)
        {
            TintRenderers(rig, new Color(0.38f, 0.38f, 0.42f, 1f));
        }

        return rig;
    }

    internal static GameObject BuildHeadIconRig(string sourceArrow, float headScale = 1.5f)
    {
        GameObject prefab = PrefabManager.Instance.GetPrefab(sourceArrow);
        if (prefab == null)
        {
            return null;
        }

        GameObject rig;
        IconRigGuard.Enter();
        try
        {
            rig = UnityEngine.Object.Instantiate(prefab);
            rig.name = $"IconRig_Head_{sourceArrow}";
            PrepareIconRig(rig);
        }
        finally
        {
            IconRigGuard.Leave();
        }

        foreach (Transform child in rig.GetComponentsInChildren<Transform>(true))
        {
            if (child == rig.transform)
            {
                continue;
            }

            if (IsShaftPart(child.name) || IsFeatherPart(child.name))
            {
                child.gameObject.SetActive(false);
            }
        }

        Renderer[] renderers = rig.GetComponentsInChildren<Renderer>(true);
        List<Renderer> activeRenderers = new List<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && renderer.gameObject.activeInHierarchy)
            {
                activeRenderers.Add(renderer);
            }
        }

        if (activeRenderers.Count > 1)
        {
            IsolateTipRenderers(activeRenderers);
        }

        activeRenderers.Clear();
        foreach (Renderer renderer in rig.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null && renderer.gameObject.activeInHierarchy)
            {
                activeRenderers.Add(renderer);
            }
        }

        if (activeRenderers.Count == 0)
        {
            UnityEngine.Object.Destroy(rig);
            return null;
        }

        foreach (Renderer renderer in rig.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            renderer.transform.localScale *= headScale;
        }

        return rig;
    }

    private static void IsolateTipRenderers(List<Renderer> renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Count; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 axis = GetPrimaryAxis(bounds.size);
        float minProjection = float.MaxValue;
        float maxProjection = float.MinValue;

        foreach (Renderer renderer in renderers)
        {
            float projection = Vector3.Dot(renderer.bounds.center - bounds.center, axis);
            minProjection = Mathf.Min(minProjection, projection);
            maxProjection = Mathf.Max(maxProjection, projection);
        }

        float keepThreshold = minProjection + (maxProjection - minProjection) * 0.45f;
        foreach (Renderer renderer in renderers)
        {
            float projection = Vector3.Dot(renderer.bounds.center - bounds.center, axis);
            if (projection < keepThreshold)
            {
                renderer.gameObject.SetActive(false);
            }
        }
    }

    private static Vector3 GetPrimaryAxis(Vector3 size)
    {
        if (size.y >= size.x && size.y >= size.z)
        {
            return Vector3.up;
        }

        if (size.z >= size.x && size.z >= size.y)
        {
            return Vector3.forward;
        }

        return Vector3.right;
    }

    private static void PrepareIconRig(GameObject rig)
    {
        foreach (Collider collider in rig.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        foreach (Rigidbody rigidbody in rig.GetComponentsInChildren<Rigidbody>(true))
        {
            rigidbody.isKinematic = true;
        }

        foreach (ZNetView view in rig.GetComponentsInChildren<ZNetView>(true))
        {
            view.enabled = false;
        }
    }

    private static void TintRenderers(GameObject rig, Color color)
    {
        foreach (Renderer renderer in rig.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            Material material = renderer.material;
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
        }
    }

    private static bool IsHeadPart(string name)
    {
        string lower = name.ToLowerInvariant();
        foreach (string token in HeadNameTokens)
        {
            if (lower.Contains(token))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsShaftPart(string name)
    {
        string lower = name.ToLowerInvariant();
        foreach (string token in ShaftNameTokens)
        {
            if (lower.Contains(token))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFeatherPart(string name)
    {
        string lower = name.ToLowerInvariant();
        foreach (string token in FeatherNameTokens)
        {
            if (lower.Contains(token))
            {
                return true;
            }
        }

        return false;
    }
}
