using System;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;

namespace ClassicUs.Manactor
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    internal static class PlayerControl_HandleRpc_Patch
    {
        private static bool Prefix(PlayerControl __instance, byte callId, MessageReader reader)
        {
            if (callId != NetworkManager.RpcHandshake) return true;
            try
            {
                if (__instance == null || __instance.Data == null) return false;
                NetworkManager.HandleHandshake(__instance.Data.PlayerId, reader);
            }
            catch (Exception e) { ManactorPlugin.Log.LogError("RPC 211 handling failed: " + e); }
            return false;
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    internal static class AmongUsClient_OnPlayerJoined_Patch
    {
        private static void Postfix(AmongUsClient __instance)
        {
            if (__instance == null) return;
            NetworkManager.SendHandshake();
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    internal static class AmongUsClient_OnPlayerLeft_Patch
    {
        private static void Postfix(ClientData data, DisconnectReasons reason)
        {
            if (data == null || data.Character == null || data.Character.Data == null) return;
            try
            {
                var pid = data.Character.Data.PlayerId;
                LobbyTracker.RemovePlayer(pid);
                ManactorAPI.FirePlayerUnmodded(pid);
            }
            catch (Exception e) { ManactorPlugin.Log.LogError("OnPlayerLeft tracker: " + e); }
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    internal static class GameStartManager_Start_Patch
    {
        private static void Postfix()
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost) return;
            LobbyTracker.Clear();
            GameStartManager_Update_Patch.StartCheck();
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    internal static class GameStartManager_Update_Patch
    {
        private static float _joinTime = -1f;
        private static bool _checking;
        private static bool _fired;

        internal static void StartCheck()
        {
            _joinTime = Time.time;
            _checking = true;
            _fired = false;
        }

        private static void Postfix()
        {
            if (!_checking || _fired) return;
            if (Time.time - _joinTime < 5f) return;

            _checking = false;
            _fired = true;

            if (!LobbyTracker.HostIsModded())
            {
                ManactorPlugin.Log.LogInfo("Host has no Manactor — unmodded lobby.");
                ManactorAPI.FireJoiningUnmoddedLobby();
            }
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    internal static class AmongUsClient_OnGameJoined_Patch
    {
        private static void Postfix(AmongUsClient __instance)
        {
            if (__instance == null || __instance.AmHost) return;
            LobbyTracker.Clear();
            GameStartManager_Update_Patch.StartCheck();
            NetworkManager.SendHandshake();
        }
    }
}
