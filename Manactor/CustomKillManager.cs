using System;
using UnityEngine;

namespace ClassicUs.Manactor
{
    public sealed class CustomKillOptions
    {
        public bool CreateDeadBody = true;
        public bool TeleportKiller = true;
        public bool PlayKillSound = true;
        public MurderResultFlags ResultFlags = MurderResultFlags.Succeeded;
    }

    public static class CustomKillManager
    {
        private const string RpcRequestKey = "classicus.manactor.RequestCustomKill";
        private const string RpcConfirmKey = "classicus.manactor.ConfirmCustomKill";

        public static void Kill(PlayerControl killer, PlayerControl target, CustomKillOptions options = null)
        {
            if (killer == null || killer.Data == null || target == null || target.Data == null) return;
            if (target.Data.IsDead || target.Data.Disconnected) return;

            options ??= new CustomKillOptions();

            var client = AmongUsClient.Instance;
            if (client != null && client.AmHost)
            {
                PerformKill(killer, target, options);
                ManactorAPI.SendRpcMethod(RpcConfirmKey, killer.Data.PlayerId, target.Data.PlayerId,
                    options.CreateDeadBody, options.TeleportKiller, options.PlayKillSound);
            }
            else
            {
                ManactorAPI.SendRpcMethod(RpcRequestKey, killer.Data.PlayerId, target.Data.PlayerId,
                    options.CreateDeadBody, options.TeleportKiller, options.PlayKillSound);
            }
        }

        [ManactorRpc(RpcRequestKey)]
        private static void OnRequestCustomKill(byte senderId, byte killerId, byte targetId, bool createDeadBody, bool teleportKiller, bool playKillSound)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;

            var killer = FindPlayer(killerId);
            var target = FindPlayer(targetId);
            if (killer == null || target == null || target.Data == null || target.Data.IsDead || target.Data.Disconnected) return;

            var options = new CustomKillOptions
            {
                CreateDeadBody = createDeadBody,
                TeleportKiller = teleportKiller,
                PlayKillSound = playKillSound,
            };

            PerformKill(killer, target, options);
            ManactorAPI.SendRpcMethod(RpcConfirmKey, killerId, targetId, createDeadBody, teleportKiller, playKillSound);
        }

        [ManactorRpc(RpcConfirmKey)]
        private static void OnConfirmCustomKill(byte senderId, byte killerId, byte targetId, bool createDeadBody, bool teleportKiller, bool playKillSound)
        {
            var client = AmongUsClient.Instance;
            if (client != null && client.AmHost) return;

            var killer = FindPlayer(killerId);
            var target = FindPlayer(targetId);
            if (killer == null || target == null || target.Data == null || target.Data.IsDead) return;

            PerformKill(killer, target, new CustomKillOptions
            {
                CreateDeadBody = createDeadBody,
                TeleportKiller = teleportKiller,
                PlayKillSound = playKillSound,
            });
        }

        private static void PerformKill(PlayerControl killer, PlayerControl target, CustomKillOptions options)
        {
            if (target == null || target.Data == null || target.Data.IsDead) return;

            try
            {
                if (options.CreateDeadBody)
                    SpawnDeadBody(killer, target);

                if (options.PlayKillSound && killer.AmOwner && killer.KillSfx != null)
                    SoundManager.Instance?.PlaySound(killer.KillSfx, false, 0.8f);

                target.gameObject.layer = LayerMask.NameToLayer("Ghost");
                target.Die(DeathReason.Kill, killer);

                if (options.TeleportKiller)
                {
                    Vector2 pos = target.GetTruePosition();
                    if (killer.NetTransform != null)
                    {
                        killer.NetTransform.SnapTo(pos);
                        killer.NetTransform.RpcSnapTo(pos);
                    }
                    else
                    {
                        killer.transform.position = new Vector3(pos.x, pos.y, killer.transform.position.z);
                    }
                }
            }
            catch (Exception e)
            {
                ManactorPlugin.Log.LogError("CustomKillManager.PerformKill failed: " + e);
            }
        }

        private static void SpawnDeadBody(PlayerControl killer, PlayerControl target)
        {
            KillAnimation anim = null;
            if (killer.KillAnimations != null)
            {
                foreach (var candidate in killer.KillAnimations)
                {
                    if (candidate == null || candidate.bodyPrefab == null) continue;
                    anim = candidate;
                    break;
                }
            }

            if (anim == null)
            {
                ManactorPlugin.Log.LogWarning("CustomKillManager.SpawnDeadBody: no KillAnimation with a bodyPrefab found on killer.KillAnimations, skipping dead body.");
                return;
            }

            var body = UnityEngine.Object.Instantiate(anim.bodyPrefab);
            body.ParentId = target.Data.PlayerId;

            Vector3 pos = target.transform.position + anim.BodyOffset;
            pos.z = pos.y / 1000f;
            body.transform.position = pos;
        }

        private static PlayerControl FindPlayer(byte playerId)
        {
            foreach (var p in PlayerControl.AllPlayerControls)
                if (p != null && p.Data != null && p.Data.PlayerId == playerId)
                    return p;
            return null;
        }
    }
}
