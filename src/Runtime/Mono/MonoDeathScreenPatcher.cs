// =============================================================================
// Copyright (c) 2026 Modding Forge
// This file is part of Simple Singleplayer Respawn
// and falls under the license GPLv3.
// =============================================================================

using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using SimpleSingleplayerRespawn.Core;

namespace SimpleSingleplayerRespawn.Runtime.Mono
{
    /// <summary>
    /// Mono-specific implementation of DeathScreen patching.
    /// </summary>
    public class MonoDeathScreenPatcher : IDeathScreenPatcher
    {
        /// <summary>
        /// Logger instance for this patcher.
        /// </summary>
        private static MelonLogger.Instance Logger => SimpleSingleplayerRespawnMod.Instance.LoggerInstance;
        
        /// <summary>
        /// Singleton instance for static Harmony method access.
        /// </summary>
        private static MonoDeathScreenPatcher _instance;

        /// <summary>
        /// Applies Harmony patches for Mono DeathScreen.
        /// </summary>
        public void ApplyPatches()
        {
            _instance = this;
            var harmony = new HarmonyLib.Harmony("modding-forge.simple-singleplayer-respawn.mono.deathscreen");
            
            try
            {
                // Patch the Mono-specific DeathScreen Open method with a Postfix to modify buttons after creation
                var targetType = Type.GetType("ScheduleOne.UI.DeathScreen, Assembly-CSharp");
                if (targetType != null)
                {
                    var targetMethod = targetType.GetMethod("Open");
                    if (targetMethod != null)
                    {
                        var postfixMethod = typeof(MonoDeathScreenPatcher).GetMethod(nameof(Open_Postfix));
                        harmony.Patch(targetMethod, postfix: new HarmonyMethod(postfixMethod));
                            SimpleSingleplayerRespawnMod.LogDebug("Mono DeathScreen.Open patch applied successfully");
                    }
                    else
                    {
                        Logger.Warning("Mono DeathScreen.Open method not found");
                    }
                }
                else
                {
                    Logger.Warning("Mono DeathScreen type not found");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to patch Mono DeathScreen: {ex}");
            }
        }

        /// <summary>
        /// Harmony postfix for Mono DeathScreen.Open method.
        /// Same route as IL2CPP: force respawn button visible in singleplayer.
        /// </summary>
        [HarmonyPostfix]
        public static void Open_Postfix(object __instance)
        {
            try
            {
                // Check if mod is enabled
                if (!(SimpleSingleplayerRespawnMod.EnableMod?.Value ?? false))
                    return;

                // If multiplayer, don't interfere
                if (_instance.IsMultiplayerSession())
                    return;

                // Immediately force the buttons to the correct state
                ForceButtonStates(__instance);

                // Also force them after the animation starts to override later UI logic
                MelonCoroutines.Start(ForceButtonStatesDelayed(__instance));
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in Mono DeathScreen.Open postfix: {ex}");
            }
        }

        private static void ForceButtonStates(object deathScreenInstance)
        {
            try
            {
                if (deathScreenInstance == null) return;
                var type = deathScreenInstance.GetType();

                // In Mono decompile these fields are public, so read them as public instance fields.
                var respawnButtonField = type.GetField("respawnButton", BindingFlags.Public | BindingFlags.Instance);
                var loadSaveButtonField = type.GetField("loadSaveButton", BindingFlags.Public | BindingFlags.Instance);

                var respawnButton = respawnButtonField?.GetValue(deathScreenInstance) as Button;
                var loadSaveButton = loadSaveButtonField?.GetValue(deathScreenInstance) as Button;

                if (respawnButton != null)
                    respawnButton.gameObject.SetActive(true);

                if (loadSaveButton != null)
                    loadSaveButton.gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error forcing Mono button states: {ex}");
            }
        }

        /// <summary>
        /// Coroutine to force button states after a delay.
        /// </summary>
        /// <param name="deathScreenInstance">DeathScreen instance</param>
        /// <returns>Coroutine enumerator</returns>
        private static IEnumerator ForceButtonStatesDelayed(object deathScreenInstance)
        {
            // Wait just after the animation starts but before UI becomes interactive (0.55f + small buffer)
            yield return new WaitForSeconds(0.6f);
            ForceButtonStates(deathScreenInstance);
        }

        /// <summary>
        /// Checks if the current session is multiplayer.
        /// </summary>
        /// <returns>True if multiplayer, false if singleplayer</returns>
        public bool IsMultiplayerSession()
        {
            try
            {
                // Prefer the game's own multiplayer signal.
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

                // Use the same logic as PlayerHealthPatcher - use reflection for FishNet access
                // For Mono, we need to access the regular FishNet namespace, not Il2Cpp
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
                return false;
            }
        }
    }
}