using System.Collections.Generic;
using UnityEngine;

namespace FletchersForge;

/// Null-safe replacement for ChestSnap's ZNetView -> GameObject lambda (SnappointHelper.cs:48).
internal static class ChestSnapNullViewPatch
{
    internal static bool Prefix(KeyValuePair<ZDO, ZNetView> g, ref GameObject __result)
    {
        if (!FletchLegacyCleanup.IsLiveZNetView(g.Value))
        {
            __result = null;
            return false;
        }

        return true;
    }
}
