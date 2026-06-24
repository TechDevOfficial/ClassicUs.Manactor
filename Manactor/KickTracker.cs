using System.Collections.Generic;
using UnityEngine;

namespace ClassicUs.Manactor
{
    internal static class KickTracker
    {
        private const float GracePeriodSeconds = 15f;
        private const float SecondChanceSeconds = 8f;

        private static readonly Dictionary<int, float> _pendingClients = new();
        private static readonly HashSet<int> _secondChance = new();

        public static void TrackJoin(int clientId)
        {
            _pendingClients[clientId] = Time.time + GracePeriodSeconds;
            _secondChance.Remove(clientId);
        }

        public static void Untrack(int clientId)
        {
            _pendingClients.Remove(clientId);
            _secondChance.Remove(clientId);
        }

        public static void CheckPending()
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost || _pendingClients.Count == 0) return;
            if (!ManactorAPI.HasLocalMods()) return;

            var due = new List<int>();
            foreach (var kvp in _pendingClients)
                if (Time.time >= kvp.Value) due.Add(kvp.Key);

            foreach (var clientId in due)
            {
                if (clientId == client.ClientId)
                {
                    _pendingClients.Remove(clientId);
                    continue;
                }

                if (IsClientModded(clientId))
                {
                    _pendingClients.Remove(clientId);
                    _secondChance.Remove(clientId);
                    continue;
                }

                if (_secondChance.Add(clientId))
                {
                    _pendingClients[clientId] = Time.time + SecondChanceSeconds;
                    continue;
                }

                _pendingClients.Remove(clientId);
                _secondChance.Remove(clientId);
                ManactorPlugin.Log.LogInfo($"[KickTracker] Client {clientId} has no compatible mods after the grace period, kicking.");
                client.KickPlayer(clientId, false);
            }
        }

        private static bool IsClientModded(int clientId)
        {
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.Data == null) continue;
                if (p.OwnerId == clientId)
                    return LobbyTracker.IsPlayerModded(p.Data.PlayerId);
            }
            return false;
        }
    }
}
