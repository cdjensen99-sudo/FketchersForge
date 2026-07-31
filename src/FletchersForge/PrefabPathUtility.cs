using UnityEngine;

namespace FletchersForge;

internal static class PrefabPathUtility
{
    internal static Transform FindFirstChild(Transform root, params string[] relativePaths)
    {
        if (root == null)
        {
            return null;
        }

        foreach (string path in relativePaths)
        {
            Transform match = root.Find(path);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    internal static Transform FindRendererChild(Transform root, bool skipInactive = false)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root)
            {
                continue;
            }

            if (skipInactive && !child.gameObject.activeSelf)
            {
                continue;
            }

            if (child.GetComponent<Renderer>() != null)
            {
                return child;
            }
        }

        return null;
    }
}
