using System;
using System.Collections.Generic;

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
