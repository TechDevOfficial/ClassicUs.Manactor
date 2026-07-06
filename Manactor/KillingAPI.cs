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
        private const float PinDurationSeconds = 1.5f;

        private sealed class PinnedPlayer
        {
            public byte PlayerId;
            public Vector2 Position;
            public float Until;
        }

        private static readonly Dictionary<byte, PinnedPlayer> _pinned = new();

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

            if (killer.Data != null)
            {
                _pinned[killer.Data.PlayerId] = new PinnedPlayer
                {
                    PlayerId = killer.Data.PlayerId,
                    Position = killerPosition,
                    Until = Time.time + PinDurationSeconds,
                };
            }

            killer.RpcMurderPlayer(target, options.ResultFlags);
            SnapToPinnedPosition(killer, killerPosition);
        }

        public static void Tick()
        {
            if (_pinned.Count == 0) return;

            List<byte> expired = null;
            foreach (var kv in _pinned)
            {
                var pin = kv.Value;
                if (Time.time >= pin.Until)
                {
                    (expired ??= new List<byte>()).Add(kv.Key);
                    continue;
                }

                var player = FindPlayer(pin.PlayerId);
                if (player == null) continue;
                SnapToPinnedPosition(player, pin.Position);
            }

            if (expired == null) return;
            foreach (var id in expired)
                _pinned.Remove(id);
        }

        private static void SnapToPinnedPosition(PlayerControl player, Vector2 position)
        {
            try
            {
                if (player.NetTransform != null)
                {
                    player.NetTransform.SnapTo(position);
                    player.NetTransform.RpcSnapTo(position);
                }
                else
                {
                    player.transform.position = new Vector3(position.x, position.y, player.transform.position.z);
                }
            }
            catch (Exception e)
            {
                ManactorPlugin.Log.LogError("KillPlayer restore position failed: " + e);
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
