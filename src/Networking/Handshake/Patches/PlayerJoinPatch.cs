using System.Collections.Generic;
using HarmonyLib;
using InnerNet;
using VentLib.Networking.RPC;
using VentLib.Utilities;
using VentLib.Version;

namespace VentLib.Networking.Handshake.Patches;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
internal static class PlayerJoinPatch
{
    private const uint VersionCheck = (uint)VentCall.VersionCheck;
    internal static readonly HashSet<int> WaitSet = new();

    internal static void Postfix(
        AmongUsClient __instance,
        [HarmonyArgument(0)] ClientData client)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        VersionControl vc = VersionControl.Instance;
        if (!vc.Handshake) return;

        ModRPC rpc = Vents.FindRPC(
            VersionCheck,
            AccessTools.Method(
                typeof(VersionCheck),
                nameof(Handshake.VersionCheck.RequestVersion)
            )
        )!;

        WaitSet.Add(client.Id);

        Async.Schedule(() =>
        {
            rpc.Send([client.Id]);

            // Diagnostic: do not invoke NoVersion timeout fallback.
            // This prevents the host from locally processing a failed handshake.
        }, NetUtils.DeriveDelay(1.5f));
    }
}
