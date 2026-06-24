using System;
using System.Collections.Generic;
using Hazel;

namespace ClassicUs.Manactor
{
    public static class ManactorAPI
    {
        private static readonly List<(string mod, string version)> _localMods = new();

        public static event Action<byte, List<(string mod, string version)>> OnPlayerModded;
        public static event Action<byte> OnPlayerUnmodded;
        public static event Action OnLobbyFullyModded;
        public static event Action OnJoiningUnmoddedLobby;
        public static event Action<byte, string, string, string> OnModVersionMismatch;

        public static void Register(string modName, string version)
        {
            _localMods.RemoveAll(m => m.mod == modName);
            _localMods.Add((modName, version));
            ManactorPlugin.Log.LogInfo($"Registered mod: {modName} v{version}");
        }

        public static bool IsLobbyFullyModded() => LobbyTracker.IsFullyModded();

        public static List<byte> GetUnmoddedPlayers() => LobbyTracker.GetUnmoddedIds();

        public static bool IsModCompatible(string modName)
        {
            var localVersion = _localMods.Find(m => m.mod == modName).version;
            if (localVersion == null) return true;
            return LobbyTracker.AllPlayersHaveVersion(modName, localVersion);
        }

        public static bool IsCompatibleToPlay()
        {
            foreach (var (mod, version) in _localMods)
                if (!LobbyTracker.AllPlayersHaveVersion(mod, version))
                    return false;
            return true;
        }

        public static void RegisterRpcHandler(byte callId, Action<byte, MessageReader> handler) =>
            NetworkManager.RegisterHandler(callId, handler);

        public static void SendRpc(byte callId, Action<MessageWriter> writePayload) =>
            NetworkManager.SendRpc(callId, writePayload);

        public static void RegisterRpcMethods(object target) =>
            ManactorRpc.RegisterMethods(target);

        public static void SendRpcMethod(byte callId, params object[] args) =>
            ManactorRpc.Send(callId, args);

        public static void SendRpcMethod(string key, params object[] args) =>
            ManactorRpc.Send(key, args);

        public static event Action OnGameStarted;
        public static event Action OnMeetingStarted;
        public static event Action<byte> OnPlayerDied;
        public static event Action<byte, string> OnRoleAssigned;

        internal static void FireGameStarted() => OnGameStarted?.Invoke();
        internal static void FireMeetingStarted() => OnMeetingStarted?.Invoke();
        internal static void FirePlayerDied(byte playerId) => OnPlayerDied?.Invoke(playerId);
        internal static void FireRoleAssigned(byte playerId, string roleName) => OnRoleAssigned?.Invoke(playerId, roleName);

        internal static IReadOnlyList<(string mod, string version)> GetLocalMods() => _localMods;

        internal static void FirePlayerModded(byte id, List<(string mod, string version)> mods)
        {
            OnPlayerModded?.Invoke(id, mods);

            foreach (var (mod, remoteVer) in mods)
            {
                var local = _localMods.Find(m => m.mod == mod);
                if (local.mod != null && local.version != remoteVer)
                    OnModVersionMismatch?.Invoke(id, mod, local.version, remoteVer);
            }

            if (LobbyTracker.IsFullyModded())
                OnLobbyFullyModded?.Invoke();
        }

        internal static void FirePlayerUnmodded(byte id) => OnPlayerUnmodded?.Invoke(id);

        internal static void FireJoiningUnmoddedLobby() => OnJoiningUnmoddedLobby?.Invoke();
    }
}
