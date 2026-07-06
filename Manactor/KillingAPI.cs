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
        private const float SuppressWindowSeconds = 2f;

        private static byte? _suppressPlayerId;
        private static float _suppressUntil;

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
                _suppressPlayerId = killer.Data.PlayerId;
                _suppressUntil = Time.time + SuppressWindowSeconds;
            }

            killer.RpcMurderPlayer(target, options.ResultFlags);
        }

        public static void Tick()
        {
        }

        internal static bool ShouldSuppressKillAnimation(byte playerId)
        {
            if (_suppressPlayerId == null || _suppressPlayerId.Value != playerId) return false;
            if (Time.time > _suppressUntil)
            {
                _suppressPlayerId = null;
                return false;
            }

            _suppressPlayerId = null;
            return true;
        }
    }
}
