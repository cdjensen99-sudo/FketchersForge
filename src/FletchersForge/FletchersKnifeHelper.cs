using System.Reflection;

namespace FletchersForge;

internal static class FletchersKnifeHelper
{
    private static readonly MethodInfo GetRightItemMethod =
        typeof(Humanoid).GetMethod("GetRightItem", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo GetLeftItemMethod =
        typeof(Humanoid).GetMethod("GetLeftItem", BindingFlags.Instance | BindingFlags.NonPublic);

    internal static bool IsKnifeInHand(Player player)
    {
        if (player == null)
        {
            return false;
        }

        return IsKnife(GetHandItem(player, GetRightItemMethod)) ||
               IsKnife(GetHandItem(player, GetLeftItemMethod));
    }

    internal static bool IsKnifeEquipped(Player player)
    {
        if (player == null)
        {
            return false;
        }

        if (IsKnife(GetHandItem(player, GetRightItemMethod)) || IsKnife(GetHandItem(player, GetLeftItemMethod)))
        {
            return true;
        }

        if (player.GetInventory() == null)
        {
            return false;
        }

        foreach (ItemDrop.ItemData equipped in player.GetInventory().GetEquippedItems())
        {
            if (IsKnife(equipped))
            {
                return true;
            }
        }

        return false;
    }

    private static ItemDrop.ItemData GetHandItem(Player player, MethodInfo method)
    {
        if (method == null)
        {
            return null;
        }

        return method.Invoke(player, null) as ItemDrop.ItemData;
    }

    private static bool IsKnife(ItemDrop.ItemData item)
    {
        if (item?.m_dropPrefab == null)
        {
            return false;
        }

        return ArrowAssemblyRegistry.IsKnifePrefab(item.m_dropPrefab.name);
    }
}
