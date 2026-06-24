using System;
using System.Collections.Generic;

namespace ClassicUs.Manactor
{
    internal static class LobbyTracker
    {
        private static readonly Dictionary<byte, List<(string mod, string version)>> _players = new();

        public static void SetPlayerMods(byte playerId, List<(string mod, string version)> mods)
        {
            _players[playerId] = mods;
        }

        public static void RemovePlayer(byte playerId)
        {
            _players.Remove(playerId);
        }

        public static void Clear() => _players.Clear();

        public static bool IsPlayerModded(byte playerId) => _players.ContainsKey(playerId);

        public static bool IsFullyModded()
        {
            if (PlayerControl.AllPlayerControls == null) return false;
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.Data == null || p.Data.Disconnected) continue;
                if (!_players.ContainsKey(p.Data.PlayerId)) return false;
            }
            return true;
        }

        public static List<byte> GetUnmoddedIds()
        {
            var result = new List<byte>();
            if (PlayerControl.AllPlayerControls == null) return result;
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.Data == null || p.Data.Disconnected) continue;
                if (!_players.ContainsKey(p.Data.PlayerId))
                    result.Add(p.Data.PlayerId);
            }
            return result;
        }

        public static bool AllPlayersHaveVersion(string modName, string version)
        {
            if (PlayerControl.AllPlayerControls == null) return true;
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.Data == null || p.Data.Disconnected) continue;
                var id = p.Data.PlayerId;
                if (!_players.TryGetValue(id, out var mods)) return false;
                var entry = mods.Find(m => m.mod == modName);
                if (entry.mod == null || entry.version != version) return false;
            }
            return true;
        }

        public static bool HostIsModded()
        {
            var client = AmongUsClient.Instance;
            if (client == null) return false;
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.Data == null) continue;
                if (p.OwnerId == client.HostId)
                    return _players.ContainsKey(p.Data.PlayerId);
            }
            return false;
        }
    }
}
