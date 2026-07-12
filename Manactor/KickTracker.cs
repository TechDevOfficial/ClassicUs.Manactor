using System.Collections.Generic;
using UnityEngine;

namespace ClassicUs.Manactor
{
    internal static class KickTracker
    {
        private const float HandshakeTimeoutSeconds = 7f;
        private static readonly Dictionary<int, float> _pendingClients = new();

        public static void TrackJoin(int clientId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost || clientId == client.ClientId) return;
            _pendingClients[clientId] = Time.time + HandshakeTimeoutSeconds;
            ManactorPlugin.Log.LogInfo($"[Handshake] Waiting for client {clientId}.");
        }

        public static void Untrack(int clientId) => _pendingClients.Remove(clientId);

        public static void ConfirmHandshake(byte playerId, bool compatible, string reason)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;

            int? clientId = FindClientId(playerId);
            if (!clientId.HasValue) return;
            _pendingClients.Remove(clientId.Value);
            if (!compatible) Kick(clientId.Value, $"incompatible client ({reason})");
        }

        public static void CheckPending()
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost || _pendingClients.Count == 0) return;

            var expired = new List<int>();
            foreach (var pair in _pendingClients)
                if (Time.time >= pair.Value) expired.Add(pair.Key);

            foreach (var clientId in expired)
            {
                _pendingClients.Remove(clientId);
                Kick(clientId, "missing Manactor handshake");
            }
        }

        public static void Clear() => _pendingClients.Clear();

        private static int? FindClientId(byte playerId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.Data != null && player.Data.PlayerId == playerId)
                    return player.OwnerId;
            return null;
        }

        private static void Kick(int clientId, string reason)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost || clientId == client.ClientId) return;

            ManactorPlugin.Log.LogWarning($"[Handshake] Kicking client {clientId}: {reason}.");
            try { client.KickPlayer(clientId, false); }
            catch (System.Exception e) { ManactorPlugin.Log.LogError($"[Handshake] Kick failed for client {clientId}: {e}"); }
        }
    }
}
