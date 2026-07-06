using System;
using System.Collections.Generic;
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
        private const float ResumeDelaySeconds = 0.08f;

        private sealed class PausedPlayer
        {
            public byte PlayerId;
            public float ResumeAt;
        }

        private static readonly Dictionary<byte, PausedPlayer> _paused = new();

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

            if (killer.Data != null && killer.NetTransform != null)
                killer.NetTransform.SetPaused(true);

            killer.RpcMurderPlayer(target, options.ResultFlags);

            if (killer.NetTransform != null)
            {
                killer.NetTransform.SnapTo(killerPosition);
                killer.NetTransform.RpcSnapTo(killerPosition);

                if (killer.Data != null)
                {
                    _paused[killer.Data.PlayerId] = new PausedPlayer
                    {
                        PlayerId = killer.Data.PlayerId,
                        ResumeAt = Time.time + ResumeDelaySeconds,
                    };
                }
            }
            else
            {
                killer.transform.position = new Vector3(killerPosition.x, killerPosition.y, killer.transform.position.z);
            }
        }

        public static void Tick()
        {
            if (_paused.Count == 0) return;

            List<byte> expired = null;
            foreach (var kv in _paused)
            {
                var pause = kv.Value;
                if (Time.time < pause.ResumeAt) continue;

                (expired ??= new List<byte>()).Add(kv.Key);
                var player = FindPlayer(pause.PlayerId);
                if (player != null && player.NetTransform != null)
                    player.NetTransform.SetPaused(false);
            }

            if (expired == null) return;
            foreach (var id in expired)
                _paused.Remove(id);
        }

        private static PlayerControl FindPlayer(byte playerId)
        {
            foreach (var p in PlayerControl.AllPlayerControls)
                if (p != null && p.Data != null && p.Data.PlayerId == playerId)
                    return p;
            return null;
        }
    }
}
