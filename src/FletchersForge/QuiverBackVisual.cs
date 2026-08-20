using UnityEngine;

namespace FletchersForge;

/// Cosmetic quiver on the back while equipped.
/// Parents a centered FF_Quiver mesh to BackShield_attach / VisEquipment.m_backShield.
internal static class QuiverBackVisual
{
    private const string InstanceName = "FF_QuiverBackCosmetic";
    private const string MeshName = "FF_QuiverBackMesh";
    private const string PreferredJointName = "BackShield_attach";

    private static GameObject instance;
    private static Transform meshTransform;
    private static Transform joint;
    private static bool missingJointLogged;
    private static bool missingPrefabLogged;

    internal static void UpdateLocal()
    {
        Player player = Player.m_localPlayer;
        if (player == null || !player.IsOwner() || player.IsDead())
        {
            Clear();
            return;
        }

        bool want = ModConfig.ShowQuiverOnBack != null &&
                    ModConfig.ShowQuiverOnBack.Value &&
                    QuiverInventory.PlayerHasEquippedQuiver(player);

        if (!want)
        {
            Clear();
            return;
        }

        if (instance == null || joint == null || meshTransform == null)
        {
            Attach(player);
            return;
        }

        ApplyLocalPose();
    }

    internal static void Refresh(Player player)
    {
        if (player == null || player != Player.m_localPlayer)
        {
            return;
        }

        Clear();
        UpdateLocal();
    }

    private static void Attach(Player player)
    {
        Clear();

        GameObject meshPrefab = AssetBundleLoader.QuiverPrefab;
        if (meshPrefab == null)
        {
            if (!missingPrefabLogged)
            {
                missingPrefabLogged = true;
                FletchersForgePlugin.Log?.LogWarning("Quiver back cosmetic: FF_Quiver AssetBundle mesh missing.");
            }

            return;
        }

        VisEquipment vis = player.GetComponent<VisEquipment>();
        joint = ResolveBackJoint(player, vis);
        if (joint == null)
        {
            if (!missingJointLogged)
            {
                missingJointLogged = true;
                FletchersForgePlugin.Log?.LogWarning("Quiver back cosmetic: no back joint yet.");
            }

            return;
        }

        instance = new GameObject(InstanceName);
        instance.transform.SetParent(joint, false);

        GameObject mesh = Object.Instantiate(meshPrefab, instance.transform);
        mesh.name = MeshName;
        meshTransform = mesh.transform;
        meshTransform.localPosition = Vector3.zero;
        meshTransform.localRotation = Quaternion.identity;
        meshTransform.localScale = Vector3.one;
        CustomVisualUtility.PrepareBundledInstance(mesh);
        CustomVisualUtility.ApplyMaterialsFromSource(mesh, AssetBundleLoader.HeadPouchPrefab);

        // Scale first, then center geometry on the joint, then apply hardcoded pose.
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * ModConstants.QuiverBackBaseScale;

        CenterChildOnLocalOrigin(meshTransform);
        ApplyLocalPose();

        FletchersForgePlugin.Log?.LogInfo(
            $"Quiver back cosmetic on joint '{joint.name}' (scale={ModConstants.QuiverBackBaseScale}).");
    }

    private static Transform ResolveBackJoint(Player player, VisEquipment vis)
    {
        Transform named = FindChildRecursive(player != null ? player.transform : null, PreferredJointName);
        if (named != null)
        {
            return named;
        }

        if (vis == null)
        {
            return null;
        }

        if (vis.m_backShield != null)
        {
            return vis.m_backShield;
        }

        if (vis.m_backMelee != null)
        {
            return vis.m_backMelee;
        }

        if (vis.m_backBow != null)
        {
            return vis.m_backBow;
        }

        if (vis.m_backTwohandedMelee != null)
        {
            return vis.m_backTwohandedMelee;
        }

        return vis.m_backTool;
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void CenterChildOnLocalOrigin(Transform child)
    {
        if (child == null)
        {
            return;
        }

        Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        bool has = false;
        Bounds bounds = default;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!has)
            {
                bounds = renderer.bounds;
                has = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!has)
        {
            return;
        }

        Transform parent = child.parent;
        if (parent == null)
        {
            return;
        }

        Vector3 localCenter = parent.InverseTransformPoint(bounds.center);
        child.localPosition -= localCenter;
    }

    private static void ApplyLocalPose()
    {
        if (instance == null)
        {
            return;
        }

        instance.transform.localPosition = new Vector3(
            ModConstants.QuiverBackPosX,
            ModConstants.QuiverBackPosY,
            ModConstants.QuiverBackPosZ);
        instance.transform.localRotation = Quaternion.Euler(
            ModConstants.QuiverBackEulerX,
            ModConstants.QuiverBackEulerY,
            ModConstants.QuiverBackEulerZ);
        instance.transform.localScale = Vector3.one * ModConstants.QuiverBackBaseScale;
    }

    private static void Clear()
    {
        if (instance != null)
        {
            Object.Destroy(instance);
            instance = null;
        }

        meshTransform = null;
        joint = null;
    }
}
