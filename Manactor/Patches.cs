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
            try
            {
                if (NetworkManager.TryDispatch(__instance, callId, reader)) return false;
            }
            catch (Exception e) { ManactorPlugin.Log.LogError("RPC dispatch failed: " + e); }
            return true;
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    internal static class AmongUsClient_OnPlayerJoined_Patch
    {
        private static void Postfix(AmongUsClient __instance, ClientData data)
        {
            if (__instance == null) return;
            NetworkManager.SendHandshake();

            if (__instance.AmHost && data != null)
                KickTracker.TrackJoin(data.Id);
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    internal static class AmongUsClient_OnPlayerLeft_Patch
    {
        private static void Postfix(ClientData data, DisconnectReasons reason)
        {
            if (data == null) return;
            KickTracker.Untrack(data.Id);

            if (data.Character == null || data.Character.Data == null) return;
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
            KickTracker.CheckPending();

            if (!_checking || _fired) return;
            if (Time.time - _joinTime < 5f) return;

            _checking = false;
            _fired = true;

            if (!LobbyTracker.HostIsModded() && ManactorAPI.HasLocalMods())
            {
                ManactorPlugin.Log.LogWarning("Host has no recorded Manactor handshake after the grace period. Auto-leave is disabled until handshake delivery is more reliable; not leaving.");
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

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    internal static class IntroCutscene_CoBegin_Patch
    {
        private static void Prefix()
        {
            try { ManactorAPI.FireGameStarted(); }
            catch (Exception e) { ManactorPlugin.Log.LogError("OnGameStarted event: " + e); }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    internal static class MeetingHud_Start_Patch
    {
        private static void Postfix()
        {
            try { ManactorAPI.FireMeetingStarted(); }
            catch (Exception e) { ManactorPlugin.Log.LogError("OnMeetingStarted event: " + e); }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    internal static class PlayerControl_MurderPlayer_Patch
    {
        private static void Postfix(PlayerControl target)
        {
            if (target == null || target.Data == null) return;
            try { ManactorAPI.FirePlayerDied(target.Data.PlayerId); }
            catch (Exception e) { ManactorPlugin.Log.LogError("OnPlayerDied event: " + e); }
        }
    }

    [HarmonyPatch(typeof(RoleBehaviour), nameof(RoleBehaviour.OnAssign))]
    internal static class RoleBehaviour_OnAssign_Patch
    {
        private static void Postfix(RoleBehaviour __instance, PlayerControl player)
        {
            if (__instance == null || player == null || player.Data == null) return;
            try { ManactorAPI.FireRoleAssigned(player.Data.PlayerId, __instance.GetType().Name); }
            catch (Exception e) { ManactorPlugin.Log.LogError("OnRoleAssigned event: " + e); }
        }
    }
}
