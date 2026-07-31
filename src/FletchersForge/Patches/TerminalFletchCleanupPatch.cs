using HarmonyLib;

namespace FletchersForge.Patches;

[HarmonyPatch(typeof(Terminal), "Awake")]
internal static class TerminalAwakeFletchCleanupPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        new Terminal.ConsoleCommand(
            "fletcher.cleanup",
            "Remove phantom fletcher bench objects from the world save.",
            (Terminal.ConsoleEventArgs args) =>
            {
                FletchLegacyCleanup.Run(forceFullPurge: true);
                HeadIconAssets.ClearCache();
                ItemRegistrar.ApplyDeferredIcons();
                args.Context.AddString("Fletcher legacy cleanup finished. Icons refreshed. Type 'save' to persist.");
                return true;
            });
    }
}
