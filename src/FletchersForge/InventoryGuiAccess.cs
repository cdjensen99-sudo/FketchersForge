using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FletchersForge;

internal static class InventoryGuiAccess
{
    // Valheim 1.0 has two SetActiveGroup overloads; unbound GetMethod throws AmbiguousMatchException
    // and poisons this type's static initializer (floods the log from QuiverHud/Fletch UI).
    private static readonly MethodInfo SetActiveGroupMethod =
        AccessTools.Method(typeof(InventoryGui), "SetActiveGroup", new Type[] { typeof(int), typeof(bool) })
        ?? typeof(InventoryGui).GetMethod(
            "SetActiveGroup",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            new Type[] { typeof(int), typeof(bool) },
            null);

    internal static void SetActiveGroup(InventoryGui gui, int index, bool playSound = false)
    {
        if (gui == null || SetActiveGroupMethod == null)
        {
            return;
        }

        SetActiveGroupMethod.Invoke(gui, new object[] { index, playSound });
    }

    internal static void SetAnimatorVisible(InventoryGui gui, bool visible)
    {
        object animator = Traverse.Create(gui).Field("m_animator").GetValue();
        if (animator != null)
        {
            Traverse.Create(animator).Method("SetBool", "visible", visible).GetValue();
        }
    }

    internal static void SetHiddenFrames(InventoryGui gui, int frames)
    {
        Traverse.Create(gui).Field("m_hiddenFrames").SetValue(frames);
    }

    internal static void SetContainerName(InventoryGui gui, string localizedName)
    {
        object containerName = Traverse.Create(gui).Field("m_containerName").GetValue();
        if (containerName != null)
        {
            Traverse.Create(containerName).Property("text").SetValue(localizedName);
        }
    }

    internal static ItemDrop.ItemData GetDragItem(InventoryGui gui)
    {
        return Traverse.Create(gui).Field<ItemDrop.ItemData>("m_dragItem").Value;
    }

    internal static bool GetFirstContainerUpdate(InventoryGui gui)
    {
        return Traverse.Create(gui).Field<bool>("m_firstContainerUpdate").Value;
    }

    internal static void SetFirstContainerUpdate(InventoryGui gui, bool value)
    {
        Traverse.Create(gui).Field("m_firstContainerUpdate").SetValue(value);
    }

    internal static void ResetContainerHold(InventoryGui gui)
    {
        Traverse.Create(gui).Field("m_containerHoldTime").SetValue(0f);
        Traverse.Create(gui).Field("m_containerHoldState").SetValue(0);
    }
}
