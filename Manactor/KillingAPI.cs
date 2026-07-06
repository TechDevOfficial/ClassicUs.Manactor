using System;
using UnityEngine;

namespace ClassicUs.Manactor
{
    public sealed class KillOptions
    {
        public bool WillTeleportMurder = true;
        public MurderResultFlags ResultFlags = MurderResultFlags.Succeeded;
    }

    internal static class KillingAPI
    {
        public static void KillPlayer(PlayerControl killer, PlayerControl target, KillOptions options = null)
        {
            if (killer == null || target == null || target.Data == null || target.Data.IsDead) return;
            options ??= new KillOptions();

            if (options.WillTeleportMurder)
            {
                killer.RpcMurderPlayer(target, options.ResultFlags);
                return;
            }

            Vector2 killerPosition = killer.GetTruePosition();
            try
            {
                killer.RpcMurderPlayer(target, options.ResultFlags);
            }
            finally
            {
                try
                {
                    if (killer.NetTransform != null)
                    {
                        killer.NetTransform.SnapTo(killerPosition);
                        killer.NetTransform.RpcSnapTo(killerPosition);
                    }
                    else
                    {
                        killer.transform.position = new Vector3(killerPosition.x, killerPosition.y, killer.transform.position.z);
                    }
                }
                catch (Exception e)
                {
                    ManactorPlugin.Log.LogError("KillPlayer restore position failed: " + e);
                }
            }
        }
    }
}
