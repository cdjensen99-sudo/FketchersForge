using System.Collections.Generic;
using UnityEngine;

namespace FletchersForge;

/// Cosmetic quiver on the back while equipped.
/// Owner writes FF_QuiverBack on the player ZDO; every client attaches a mesh when that flag is set.
internal static class QuiverBackVisual
{
    private const string InstanceName = "FF_QuiverBackCosmetic";
    private const string MeshName = "FF_QuiverBackMesh";
    private const string PreferredJointName = "BackShield_attach";
    private const string ZdoKey = "FF_QuiverBack";

    private static readonly int ZdoHash = ZdoKey.GetStableHashCode();
    private static readonly Dictionary<Player, Attached> Attachments = new Dictionary<Player, Attached>();
    private static readonly List<Player> ScratchPlayers = new List<Player>();
    private static readonly List<Player> ToRemove = new List<Player>();
    private static bool missingPrefabLogged;

    private sealed class Attached
    {
        public GameObject Root;
        public Transform Mesh;
        public Transform Joint;
    }

    /// Owner: publish equipped-quiver state so remote clients can render the back mesh.
    internal static void SyncOwnerZdo(Player player)
    {
        if (player == null || !player.IsOwner())
        {
            return;
        }

        ZNetView nview = player.GetComponent<ZNetView>();
        ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
        if (zdo == null)
        {
            return;
        }

        bool want = QuiverInventory.PlayerHasEquippedQuiver(player);
        if (zdo.GetBool(ZdoHash, false) != want)
        {
            zdo.Set(ZdoHash, want);
        }
    }

    internal static void UpdateAll()
    {
        Player local = Player.m_localPlayer;
        if (local != null && local.IsOwner())
        {
            SyncOwnerZdo(local);
        }

        ScratchPlayers.Clear();
        ScratchPlayers.AddRange(Player.GetAllPlayers());

        foreach (Player player in ScratchPlayers)
        {
            UpdatePlayer(player);
        }

        ToRemove.Clear();
        foreach (KeyValuePair<Player, Attached> pair in Attachments)
        {
            if (pair.Key == null || !ScratchPlayers.Contains(pair.Key))
            {
                ToRemove.Add(pair.Key);
            }
        }

        foreach (Player gone in ToRemove)
        {
            ClearPlayer(gone);
        }
    }

    internal static void Refresh(Player player)
    {
        if (player == null)
        {
            return;
        }

        SyncOwnerZdo(player);
        ClearPlayer(player);
        UpdatePlayer(player);
    }

    private static void UpdatePlayer(Player player)
    {
        if (player == null || player.IsDead())
        {
            ClearPlayer(player);
            return;
        }

        if (!ShouldShow(player))
        {
            ClearPlayer(player);
            return;
        }

        if (!Attachments.TryGetValue(player, out Attached attached) ||
            attached == null ||
            attached.Root == null ||
            attached.Joint == null ||
            attached.Mesh == null ||
            attached.Root.transform.parent != attached.Joint)
        {
            Attach(player);
            return;
        }

        ApplyLocalPose(attached.Root);
    }

    private static bool ShouldShow(Player player)
    {
        ZNetView nview = player.GetComponent<ZNetView>();
        ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
        if (zdo == null || !zdo.GetBool(ZdoHash, false))
        {
            return false;
        }

        // Local config only hides your own back mesh; others still see you.
        if (player == Player.m_localPlayer &&
            (ModConfig.ShowQuiverOnBack == null || !ModConfig.ShowQuiverOnBack.Value))
        {
            return false;
        }

        return true;
    }

    private static void Attach(Player player)
    {
        ClearPlayer(player);

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
        Transform joint = ResolveBackJoint(player, vis);
        if (joint == null)
        {
            return;
        }

        GameObject root = new GameObject(InstanceName);
        root.transform.SetParent(joint, false);

        GameObject mesh = Object.Instantiate(meshPrefab, root.transform);
        mesh.name = MeshName;
        Transform meshTransform = mesh.transform;
        meshTransform.localPosition = Vector3.zero;
        meshTransform.localRotation = Quaternion.identity;
        meshTransform.localScale = Vector3.one;
        CustomVisualUtility.PrepareBundledInstance(mesh);
        CustomVisualUtility.ApplyMaterialsFromSource(mesh, AssetBundleLoader.HeadPouchPrefab);

        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one * ModConstants.QuiverBackBaseScale;

        CenterChildOnLocalOrigin(meshTransform);
        ApplyLocalPose(root);

        Attachments[player] = new Attached
        {
            Root = root,
            Mesh = meshTransform,
            Joint = joint,
        };
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

    private static void ApplyLocalPose(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        root.transform.localPosition = new Vector3(
            ModConstants.QuiverBackPosX,
            ModConstants.QuiverBackPosY,
            ModConstants.QuiverBackPosZ);
        root.transform.localRotation = Quaternion.Euler(
            ModConstants.QuiverBackEulerX,
            ModConstants.QuiverBackEulerY,
            ModConstants.QuiverBackEulerZ);
        root.transform.localScale = Vector3.one * ModConstants.QuiverBackBaseScale;
    }

    private static void ClearPlayer(Player player)
    {
        if (player == null)
        {
            return;
        }

        if (!Attachments.TryGetValue(player, out Attached attached))
        {
            return;
        }

        Attachments.Remove(player);
        if (attached?.Root != null)
        {
            Object.Destroy(attached.Root);
        }
    }
}
