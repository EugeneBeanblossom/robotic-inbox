﻿using HarmonyLib;
using RoboticInbox.Utilities;
using System;
using System.Reflection;
using static ModEvents;

namespace RoboticInbox
{
    public class ModApi : IModApi
    {
        private const string MOD_MAINTAINER = "kanaverum";
        private const string SUPPORT_LINK = "https://discord.gg/hYa2sNHXya";
        private const string DLL_VERSION = "dev-dll-version";
        private const string BUILD_TARGET = "dev-build-target";

        private static readonly ModLog<ModApi> _log = new ModLog<ModApi>();

        public static bool DebugMode { get; set; } = false;

        public void InitMod(Mod _modInstance)
        {
            try
            {
                _log.Info($"Robotic Inbox DLL Version {DLL_VERSION} build for 7DTD {BUILD_TARGET}");
                new Harmony(GetType().ToString()).PatchAll(Assembly.GetExecutingAssembly());
                SettingsManager.Load();
                ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
                ModEvents.PlayerSpawnedInWorld.RegisterHandler(OnPlayerSpawnedInWorld);
            }
            catch (Exception e)
            {
                _log.Error($"Failed to start up Robotic Inbox mod; take a look at logs for guidance but feel free to also reach out to the mod maintainer {MOD_MAINTAINER} via {SUPPORT_LINK}", e);
            }
        }

        private void OnGameStartDone(ref SGameStartDoneData args)
        {
            try
            {
                StorageManager.OnGameStartDone();
                SignManager.OnGameStartDone();
            }
            catch (Exception e)
            {
                _log.Error("OnGameStartDone Failed", e);
            }
        }

        //private void OnPlayerSpawnedInWorld(ClientInfo clientInfo, RespawnType respawnType, Vector3i pos)
        private void OnPlayerSpawnedInWorld(ref SPlayerSpawnedInWorldData args)
        {
            try
            {
                if (args.ClientInfo == null)
                {
                    switch (args.RespawnType)
                    {
                        case RespawnType.NewGame: // local player creating a new game
                        case RespawnType.LoadedGame: // local player loading existing game
                        case RespawnType.Died: // existing player returned from death
                            for (var i = 0; i < GameManager.Instance.World.GetLocalPlayers().Count; i++)
                            {
                                SettingsManager.PropagateHorizontalRange(GameManager.Instance.World.GetLocalPlayers()[i]);
                                SettingsManager.PropagateVerticalRange(GameManager.Instance.World.GetLocalPlayers()[i]);
                            }
                            break;
                    }
                }
                else
                {
                    if (!GameManager.Instance.World.Players.dict.TryGetValue(args.ClientInfo.entityId, out var player) || !player.IsAlive())
                    {
                        return; // player not found or player not ready
                    }

                    switch (args.RespawnType)
                    {
                        case RespawnType.EnterMultiplayer: // first-time login for new player
                        case RespawnType.JoinMultiplayer: // existing player rejoining
                        case RespawnType.Died: // existing player returned from death

                            SettingsManager.PropagateHorizontalRange(player);
                            SettingsManager.PropagateVerticalRange(player);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error("Failed to handle PlayerSpawnedInWorld event.", e);
            }
        }
    }
}
