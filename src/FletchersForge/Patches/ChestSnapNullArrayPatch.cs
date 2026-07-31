using System.Linq;
using UnityEngine;

namespace FletchersForge;

/// Filters null GameObjects from ChestSnap's scene object arrays (SnappointHelper.cs:48).
internal static class ChestSnapNullArrayPatch
{
    internal static void Postfix(ref GameObject[] __result)
    {
        if (__result == null || __result.Length == 0)
        {
            return;
        }

        GameObject[] filtered = __result.Where(gameObject => gameObject != null).ToArray();
        if (filtered.Length != __result.Length)
        {
            __result = filtered;
        }
    }
}
