using System;
using System.Collections.Generic;
using Hazel;

namespace ClassicUs.Manactor
{
    internal static class NetworkManager
    {
        public const byte RpcHandshake = 211;

        public static void SendHandshake()
        {
            var client = AmongUsClient.Instance;
            var local = PlayerControl.LocalPlayer;
            if (client == null || local == null) return;

            try
            {
                var payload = BuildModString();
                var w = client.StartRpcImmediately(local.NetId, RpcHandshake, SendOption.Reliable, -1);
                w.Write(payload);
                client.FinishRpcImmediately(w);
                ManactorPlugin.Log.LogInfo($"Handshake sent: {payload}");
            }
            catch (Exception e)
            {
                ManactorPlugin.Log.LogError("SendHandshake failed: " + e);
            }
        }

        public static void HandleHandshake(byte senderId, MessageReader reader)
        {
            try
            {
                var raw = reader.ReadString();
                var mods = ParseModString(raw);
                LobbyTracker.SetPlayerMods(senderId, mods);
                ManactorAPI.FirePlayerModded(senderId, mods);
                ManactorPlugin.Log.LogInfo($"Handshake from {senderId}: {raw}");
            }
            catch (Exception e)
            {
                ManactorPlugin.Log.LogError("HandleHandshake failed: " + e);
            }
        }

        private static string BuildModString()
        {
            var mods = ManactorAPI.GetLocalMods();
            var parts = new List<string>();
            foreach (var (mod, version) in mods)
                parts.Add($"{mod}:{version}");
            return string.Join("|", parts);
        }

        private static List<(string mod, string version)> ParseModString(string raw)
        {
            var result = new List<(string, string)>();
            if (string.IsNullOrWhiteSpace(raw)) return result;
            foreach (var entry in raw.Split('|'))
            {
                var parts = entry.Split(':');
                if (parts.Length == 2)
                    result.Add((parts[0], parts[1]));
            }
            return result;
        }
    }
}
