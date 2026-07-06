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
        private const float MaxPinSeconds = 3f;

        private sealed class PinState
        {
            public Vector2 Position;
            public bool Locked;
            public float CreatedAt;
        }

        private static readonly Dictionary<byte, PinState> _pins = new();

        public static void KillPlayer(PlayerControl killer, PlayerControl target, KillOptions options = null)
        {
            if (killer == null || target == null || target.Data == null || target.Data.IsDead) return;
            options ??= new KillOptions();

            if (options.WillTeleportMurder)
            {
                killer.RpcMurderPlayer(target, options.ResultFlags);
                return;
            }

            if (killer.Data != null)
            {
                _pins[killer.Data.PlayerId] = new PinState
                {
                    Position = killer.GetTruePosition(),
                    Locked = false,
                    CreatedAt = Time.time,
                };
            }

            Vector2 preKillPosition = killer.GetTruePosition();
            killer.RpcMurderPlayer(target, options.ResultFlags);
            RestorePosition(killer, preKillPosition);
        }

        internal static void OnSetMovement(PlayerControl source, bool canMove)
        {
            if (source == null || source.Data == null) return;
            if (!_pins.TryGetValue(source.Data.PlayerId, out var pin)) return;

            if (!canMove)
            {
                pin.Locked = true;
                RestorePosition(source, pin.Position);
                return;
            }

            _pins.Remove(source.Data.PlayerId);
            RestorePosition(source, pin.Position);
        }

        public static void Tick()
        {
            if (_pins.Count == 0) return;

            List<byte> expired = null;
            foreach (var kv in _pins)
            {
                if (Time.time - kv.Value.CreatedAt > MaxPinSeconds)
                {
                    (expired ??= new List<byte>()).Add(kv.Key);
                    continue;
                }

                if (!kv.Value.Locked) continue;

                var player = FindPlayer(kv.Key);
                if (player == null) continue;

                RestorePosition(player, kv.Value.Position);
            }

            if (expired == null) return;
            foreach (var id in expired)
                _pins.Remove(id);
        }

        private static void RestorePosition(PlayerControl player, Vector2 position)
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

        private static PlayerControl FindPlayer(byte playerId)
        {
            foreach (var p in PlayerControl.AllPlayerControls)
                if (p != null && p.Data != null && p.Data.PlayerId == playerId)
                    return p;
            return null;
        }
    }
}
