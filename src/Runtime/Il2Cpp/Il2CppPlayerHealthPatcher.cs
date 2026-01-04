// =============================================================================
// Copyright (c) 2026 Modding Forge
// This file is part of Simple Singleplayer Respawn
// and falls under the license GPLv3.
// =============================================================================

using System;
using HarmonyLib;
using Il2CppScheduleOne.PlayerScripts.Health;
using Il2CppFishNet.Object;
using Il2CppFishNet.Managing;
using MelonLoader;
using UnityEngine;
using SimpleSingleplayerRespawn.Core;

namespace SimpleSingleplayerRespawn.Runtime.Il2Cpp
{
    /// <summary>
    /// Il2Cpp-specific implementation of PlayerHealth patching.
    /// </summary>
    public class Il2CppPlayerHealthPatcher : IPlayerHealthPatcher
    {
        /// <summary>
        /// Logger instance for this patcher.
        /// </summary>
        private static MelonLogger.Instance Logger => SimpleSingleplayerRespawnMod.Instance.LoggerInstance;
        
        /// <summary>
        /// Singleton instance for static Harmony method access.
        /// </summary>
        private static Il2CppPlayerHealthPatcher _instance;

        /// <summary>
        /// Applies Harmony patches for Il2Cpp PlayerHealth.
        /// </summary>
        public void ApplyPatches()
        {
            _instance = this;
            var harmony = new HarmonyLib.Harmony("modding-forge.simple-singleplayer-respawn.il2cpp.playerhealth");
            
            try
            {
                // Patch the Il2Cpp-specific RPC method
                var targetMethod = typeof(PlayerHealth).GetMethod("RpcLogic___SendDie_2166136261");
                if (targetMethod != null)
                {
                    var prefixMethod = typeof(Il2CppPlayerHealthPatcher).GetMethod(nameof(SendDie_RpcLogic_Prefix));
                    harmony.Patch(targetMethod, new HarmonyMethod(prefixMethod));
                    SimpleSingleplayerRespawnMod.LogDebug("Il2Cpp PlayerHealth RPC patch applied successfully");
                }
                else
                {
                    Logger.Error("Could not find Il2Cpp PlayerHealth RPC method to patch");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to apply Il2Cpp PlayerHealth patches: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Patches the RPC logic for SendDie - this is where the actual multiplayer vs singleplayer decision is made.
        /// In FishNet, RPCs are the core of multiplayer functionality, so we patch the RPC logic directly.
        /// </summary>
        public static bool SendDie_RpcLogic_Prefix(PlayerHealth __instance)
        {
            try
            {
                SimpleSingleplayerRespawnMod.LogDebug("=== SendDie RpcLogic called ===");

                // Check if mod is enabled
                if (!(SimpleSingleplayerRespawnMod.EnableMod?.Value ?? false))
                {
                    SimpleSingleplayerRespawnMod.LogDebug("Mod is disabled, using original behavior");
                    return true;
                }

                SimpleSingleplayerRespawnMod.LogDebug("Mod is enabled, checking session type");

                // Check if we're in singleplayer mode
                if (_instance.IsMultiplayerSession())
                {
                    SimpleSingleplayerRespawnMod.LogDebug("Multiplayer session detected, using original death behavior");
                    return true; // Use original multiplayer behavior
                }

                SimpleSingleplayerRespawnMod.LogDebug("Singleplayer session detected, implementing multiplayer-style death via RPC");
                
                // Call the Die RPC logic directly - this should trigger the real multiplayer death behavior
                // This bypasses any singleplayer checks in SendDie and goes straight to the multiplayer death logic
                // The HospitalBillScreen will handle the hospital bill when respawn is clicked
                __instance.RpcLogic___Die_2166136261();
                
                SimpleSingleplayerRespawnMod.LogDebug("=== SendDie RpcLogic finished (called Die RPC directly) ===");
                return false; // Skip original SendDie RPC
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in SendDie_RpcLogic_Prefix: {ex}");
                return true; // Fallback to original behavior on error
            }
        }

        /// <summary>
        /// Checks if the current session is multiplayer.
        /// </summary>
        /// <returns>True if multiplayer, false if singleplayer</returns>
        public bool IsMultiplayerSession()
        {
            try
            {
                // Look for NetworkManager (FishNet) to determine if we're in multiplayer
                var networkManager = GameObject.FindObjectOfType<NetworkManager>();
                if (networkManager == null)
                {
                    return false;
                }

                // Count authenticated connections
                int authenticatedConnections = 0;
                var serverManager = networkManager.ServerManager;
                if (serverManager != null && serverManager.Clients != null)
                {
                    authenticatedConnections = serverManager.Clients.Count;
                }

                // Check for multiple PlayerHealth objects (indicates multiple players)
                var playerHealthObjects = GameObject.FindObjectsOfType<PlayerHealth>();
                var totalPlayers = playerHealthObjects?.Length ?? 0;

                // It's multiplayer if we have either:
                // 1. More than 1 authenticated connection, OR
                // 2. More than 1 player in the scene, OR
                // 3. We are connected as client to a remote server (not hosting)
                bool isMultiplayer = authenticatedConnections > 1 || 
                                   totalPlayers > 1 || 
                                   (networkManager.IsClient && !networkManager.IsHost);
                
                return isMultiplayer;
            }
            catch (Exception ex)
            {
                // Only log errors, not debug info
                Logger.Error($"Error checking multiplayer status: {ex.Message}");
                return false; // Default to singleplayer if uncertain
            }
        }


    }
}