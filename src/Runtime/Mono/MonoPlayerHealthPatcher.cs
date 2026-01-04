// =============================================================================
// Copyright (c) 2026 Modding Forge
// This file is part of Simple Singleplayer Respawn
// and falls under the license GPLv3.
// =============================================================================

using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using SimpleSingleplayerRespawn.Core;

namespace SimpleSingleplayerRespawn.Runtime.Mono
{
    /// <summary>
    /// Mono-specific implementation of PlayerHealth patching.
    /// </summary>
    public class MonoPlayerHealthPatcher : IPlayerHealthPatcher
    {
        /// <summary>
        /// Logger instance for this patcher.
        /// </summary>
        private static MelonLogger.Instance Logger => SimpleSingleplayerRespawnMod.Instance.LoggerInstance;
        
        /// <summary>
        /// Singleton instance for static Harmony method access.
        /// </summary>
        private static MonoPlayerHealthPatcher _instance;

        /// <summary>
        /// Applies Harmony patches for Mono PlayerHealth.
        /// </summary>
        public void ApplyPatches()
        {
            _instance = this;
            var harmony = new HarmonyLib.Harmony("modding-forge.simple-singleplayer-respawn.mono.playerhealth");
            
            try
            {
                // Use the same approach as Il2Cpp: patch SendDie RpcLogic and call Die directly
                var targetType = Type.GetType("ScheduleOne.PlayerScripts.Health.PlayerHealth, Assembly-CSharp");
                if (targetType != null)
                {
                    var targetMethod = targetType.GetMethod("RpcLogic___SendDie_2166136261");
                    if (targetMethod != null)
                    {
                        var prefixMethod = typeof(MonoPlayerHealthPatcher).GetMethod(nameof(SendDie_RpcLogic_Prefix));
                        harmony.Patch(targetMethod, new HarmonyMethod(prefixMethod));
                            SimpleSingleplayerRespawnMod.LogDebug("Mono PlayerHealth RpcLogic SendDie patch applied successfully");
                    }
                    else
                    {
                        Logger.Warning("Mono PlayerHealth.RpcLogic___SendDie_2166136261 method not found");
                    }
                }
                else
                {
                    Logger.Warning("Mono PlayerHealth type not found");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to patch Mono PlayerHealth: {ex}");
            }
        }

        /// <summary>
        /// Patches the RPC logic for SendDie - same approach as Il2Cpp version.
        /// In FishNet, RPCs are the core of multiplayer functionality, so we patch the RPC logic directly.
        /// </summary>
        [HarmonyPrefix]
        public static bool SendDie_RpcLogic_Prefix(object __instance)
        {
            try
            {
                SimpleSingleplayerRespawnMod.LogDebug("=== Mono SendDie RpcLogic called ===");

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
                
                // Call the Die RPC logic directly - same as Il2Cpp version
                // This bypasses any singleplayer checks in SendDie and goes straight to the multiplayer death logic
                var playerHealthType = __instance.GetType();
                var dieRpcMethod = playerHealthType.GetMethod("RpcLogic___Die_2166136261");
                if (dieRpcMethod != null)
                {
                    SimpleSingleplayerRespawnMod.LogDebug("Calling RpcLogic___Die_2166136261 directly");
                    dieRpcMethod.Invoke(__instance, null);
                }
                else
                {
                    Logger.Warning("RpcLogic___Die_2166136261 method not found on PlayerHealth instance");
                    return true; // Fall back to original behavior
                }
                
                SimpleSingleplayerRespawnMod.LogDebug("=== Mono SendDie RpcLogic finished (called Die RPC directly) ===");
                return false; // Skip original SendDie RPC
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in Mono SendDie_RpcLogic_Prefix: {ex}");
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
                // Prefer the game's own multiplayer signal when available.
                // In Mono, FishNet can exist even in singleplayer/host scenarios.
                // DeathScreen.CanRespawn() uses Player.PlayerList.Count > 1, so mirror that.
                var playerType = Type.GetType("ScheduleOne.PlayerScripts.Player, Assembly-CSharp");
                if (playerType != null)
                {
                    var playerListField = playerType.GetField("PlayerList", BindingFlags.Public | BindingFlags.Static);
                    var playerList = playerListField?.GetValue(null);
                    var countProperty = playerList?.GetType().GetProperty("Count");
                    if (countProperty != null)
                    {
                        var countObj = countProperty.GetValue(playerList);
                        if (countObj is int count)
                        {
                            SimpleSingleplayerRespawnMod.LogDebug($"Player.PlayerList.Count = {count}");
                            return count > 1;
                        }
                    }
                }

                // Look for NetworkManager (FishNet) to determine if we're in multiplayer
                // Use reflection to access FishNet types without compile-time dependency
                var fishNetManagerType = Type.GetType("FishNet.Managing.NetworkManager, FishNet.Runtime");
                if (fishNetManagerType != null)
                {
                    // Get the generic FindObjectOfType method
                    var findObjectOfTypeMethod = typeof(UnityEngine.Object).GetMethod("FindObjectOfType", Type.EmptyTypes);
                    if (findObjectOfTypeMethod != null)
                    {
                        // Make the generic method with FishNet.Managing.NetworkManager as the type argument
                        var genericMethod = findObjectOfTypeMethod.MakeGenericMethod(fishNetManagerType);
                        var networkManager = genericMethod.Invoke(null, null);
                        
                        if (networkManager == null)
                        {
                            SimpleSingleplayerRespawnMod.LogDebug("No NetworkManager found, assuming singleplayer");
                            return false;
                        }

                        // Check IsServer and IsClient properties via reflection
                        var isServerProperty = fishNetManagerType.GetProperty("IsServer");
                        var isClientProperty = fishNetManagerType.GetProperty("IsClient");
                        
                        if (isServerProperty != null && isClientProperty != null)
                        {
                            bool isServer = (bool)(isServerProperty.GetValue(networkManager) ?? false);
                            bool isClient = (bool)(isClientProperty.GetValue(networkManager) ?? false);
                            
                            SimpleSingleplayerRespawnMod.LogDebug($"NetworkManager state - IsServer: {isServer}, IsClient: {isClient}");
                            
                            bool isMultiplayer = isServer || isClient;
                            SimpleSingleplayerRespawnMod.LogDebug($"Session determined as: {(isMultiplayer ? "Multiplayer" : "Singleplayer")}");
                            return isMultiplayer;
                        }
                        else
                        {
                            Logger.Warning("IsServer or IsClient properties not found on NetworkManager");
                            return false;
                        }
                    }
                    else
                    {
                        Logger.Warning("FindObjectOfType method not found");
                        return false;
                    }
                }
                else
                {
                    SimpleSingleplayerRespawnMod.LogDebug("FishNet.Managing.NetworkManager type not found, assuming singleplayer");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error determining session type: {ex}. Assuming singleplayer for safety.");
                return false; // Default to singleplayer if we can't determine
            }
        }
    }
}