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
        private const float PauseDurationSeconds = 0.5f;

        private sealed class PausedPlayer
        {
            public byte PlayerId;
            public Vector2 Position;
            public float Until;
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
            {
                killer.NetTransform.SetPaused(true);
                _paused[killer.Data.PlayerId] = new PausedPlayer
                {
                    PlayerId = killer.Data.PlayerId,
                    Position = killerPosition,
                    Until = Time.time + PauseDurationSeconds,
                };
            }

            killer.RpcMurderPlayer(target, options.ResultFlags);

            if (killer.NetTransform == null)
                killer.transform.position = new Vector3(killerPosition.x, killerPosition.y, killer.transform.position.z);
        }

        public static void Tick()
        {
            if (_paused.Count == 0) return;

            List<byte> expired = null;
            foreach (var kv in _paused)
            {
                var pause = kv.Value;
                var player = FindPlayer(pause.PlayerId);

                if (Time.time >= pause.Until)
                {
                    (expired ??= new List<byte>()).Add(kv.Key);
                    if (player != null) ResumePlayer(player, pause.Position);
                    continue;
                }

                if (player == null) continue;
                player.transform.position = new Vector3(pause.Position.x, pause.Position.y, player.transform.position.z);
            }

            if (expired == null) return;
            foreach (var id in expired)
                _paused.Remove(id);
        }

        private static void ResumePlayer(PlayerControl player, Vector2 position)
        {
            try
            {
                if (player.NetTransform != null)
                {
                    player.NetTransform.SnapTo(position);
                    player.NetTransform.RpcSnapTo(position);
                    player.NetTransform.SetPaused(false);
                }
            }
            catch (Exception e)
            {
                ManactorPlugin.Log.LogError("KillPlayer resume position failed: " + e);
            }
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
