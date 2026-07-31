using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FletchersForge;

/// Applies ChestSnap compatibility patches manually (auto TargetMethod lookup fails on optional params).
internal static class ChestSnapHarmony
{
    internal static void Apply(Harmony harmony)
    {
        Type helper = AccessTools.TypeByName("ChestSnap.Helpers.SnappointHelper");
        if (helper == null)
        {
            FletchersForgePlugin.Log?.LogWarning("ChestSnap compat: SnappointHelper type not found.");
            return;
        }

        int applied = 0;

        MethodInfo recreate = AccessTools.Method(helper, "RecreateSnappoints");
        if (recreate != null)
        {
            harmony.Patch(
                recreate,
                prefix: new HarmonyMethod(typeof(ChestSnapCompatPatch), nameof(ChestSnapCompatPatch.Prefix)));
            applied++;
            FletchersForgePlugin.Log?.LogInfo("ChestSnap compat: patched RecreateSnappoints.");
        }

        MethodInfo routine = AccessTools.Method(helper, "RecreateSnappointsRoutine");
        if (routine != null)
        {
            harmony.Patch(
                routine,
                prefix: new HarmonyMethod(typeof(ChestSnapCompatPatch), nameof(ChestSnapCompatPatch.Prefix)));
            applied++;
            FletchersForgePlugin.Log?.LogInfo("ChestSnap compat: patched RecreateSnappointsRoutine.");
        }

        Type compilerClass = helper.GetNestedTypes(BindingFlags.NonPublic)
            .FirstOrDefault(t => t.Name == "<>c");

        if (compilerClass != null)
        {
            MethodInfo nullViewLambda = compilerClass
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == "<RecreateSnappointsRoutine>b__2_9");

            if (nullViewLambda != null)
            {
                harmony.Patch(
                    nullViewLambda,
                    prefix: new HarmonyMethod(typeof(ChestSnapNullViewPatch), nameof(ChestSnapNullViewPatch.Prefix)));
                applied++;
                FletchersForgePlugin.Log?.LogInfo("ChestSnap compat: patched null-view lambda.");
            }

            MethodInfo objectArrayLambda = compilerClass
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == "<RecreateSnappointsRoutine>b__2_8");

            if (objectArrayLambda != null)
            {
                harmony.Patch(
                    objectArrayLambda,
                    postfix: new HarmonyMethod(typeof(ChestSnapNullArrayPatch), nameof(ChestSnapNullArrayPatch.Postfix)));
                applied++;
                FletchersForgePlugin.Log?.LogInfo("ChestSnap compat: patched GameObject[] lambda.");
            }
        }

        Type stateMachine = helper.GetNestedTypes(BindingFlags.NonPublic)
            .FirstOrDefault(t => t.Name.StartsWith("<RecreateSnappointsRoutine>d", StringComparison.Ordinal));

        if (stateMachine != null)
        {
            MethodInfo moveNext = AccessTools.Method(stateMachine, "MoveNext");
            if (moveNext != null)
            {
                harmony.Patch(
                    moveNext,
                    finalizer: new HarmonyMethod(typeof(ChestSnapMoveNextFinalizerPatch), nameof(ChestSnapMoveNextFinalizerPatch.Finalizer)));
                applied++;
                FletchersForgePlugin.Log?.LogInfo("ChestSnap compat: patched MoveNext finalizer.");
            }
        }

        FletchersForgePlugin.Log?.LogInfo($"ChestSnap compat: {applied} patch(es) applied.");
    }
}
