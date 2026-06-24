using System;
using System.Collections.Generic;
using Hazel;

namespace ClassicUs.Manactor
{
    internal static class NetworkManager
    {
        public const byte RpcHandshake = 211;

        private static readonly Dictionary<byte, Action<byte, MessageReader>> _handlers = new();

        public static void RegisterHandler(byte callId, Action<byte, MessageReader> handler)
        {
            _handlers[callId] = handler;
        }

        public static bool TryDispatch(PlayerControl sender, byte callId, MessageReader reader)
        {
            ManactorRpc.EnsureFlushed();

            if (sender == null || sender.Data == null) return false;

            if (callId == RpcHandshake)
            {
                HandleHandshake(sender.Data.PlayerId, reader);
                return true;
            }

            if (_handlers.TryGetValue(callId, out var handler))
            {
                handler(sender.Data.PlayerId, reader);
                return true;
            }

            return false;
        }

        public static void SendRpc(byte callId, Action<MessageWriter> writePayload)
        {
            var client = AmongUsClient.Instance;
            var local = PlayerControl.LocalPlayer;
            if (client == null || local == null) return;

            try
            {
                var w = client.StartRpcImmediately(local.NetId, callId, SendOption.Reliable, -1);
                writePayload?.Invoke(w);
                client.FinishRpcImmediately(w);
            }
            catch (Exception e)
            {
                ManactorPlugin.Log.LogError("SendRpc failed: " + e);
            }
        }

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
