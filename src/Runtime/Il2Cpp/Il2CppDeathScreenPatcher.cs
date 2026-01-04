// =============================================================================
// Copyright (c) 2026 Modding Forge
// This file is part of Simple Singleplayer Respawn
// and falls under the license GPLv3.
// =============================================================================

using System;
using HarmonyLib;
using Il2CppScheduleOne.UI;
using MelonLoader;
using UnityEngine;
using SimpleSingleplayerRespawn.Core;

namespace SimpleSingleplayerRespawn.Runtime.Il2Cpp
{
    /// <summary>
    /// Il2Cpp-specific implementation of DeathScreen patching.
    /// </summary>
    public class Il2CppDeathScreenPatcher : IDeathScreenPatcher
    {
        /// <summary>
        /// Logger instance for this patcher.
        /// </summary>
        private static MelonLogger.Instance Logger => SimpleSingleplayerRespawnMod.Instance.LoggerInstance;
        
        /// <summary>
        /// Reference to the PlayerHealth patcher for session detection.
        /// </summary>
        private readonly IPlayerHealthPatcher _playerHealthPatcher;
        
        /// <summary>
        /// Initializes a new instance of the Il2CppDeathScreenPatcher.
        /// </summary>
        public Il2CppDeathScreenPatcher()
        {
            _playerHealthPatcher = RuntimePatcherFactory.CreatePlayerHealthPatcher();
        }
        
        /// <summary>
        /// Applies Harmony patches for Il2Cpp DeathScreen.
        /// </summary>
        public void ApplyPatches()
        {
            var harmony = new HarmonyLib.Harmony("modding-forge.simple-singleplayer-respawn.il2cpp.deathscreen");
            
            try
            {
                // Patch the Il2Cpp-specific Open method
                var targetMethod = typeof(DeathScreen).GetMethod("Open");
                if (targetMethod != null)
                {
                    var postfixMethod = typeof(Il2CppDeathScreenPatcher).GetMethod(nameof(Open_Postfix));
                    harmony.Patch(targetMethod, postfix: new HarmonyMethod(postfixMethod));
                        SimpleSingleplayerRespawnMod.LogDebug("Il2Cpp DeathScreen Open patch applied successfully");
                }
                else
                {
                    Logger.Error("Could not find Il2Cpp DeathScreen Open method to patch");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to apply Il2Cpp DeathScreen patches: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Patches the Open method to force respawn button visibility in singleplayer.
        /// This is a more direct approach than patching CanRespawn.
        /// </summary>
        public static void Open_Postfix(DeathScreen __instance)
        {
            try
            {
                SimpleSingleplayerRespawnMod.LogDebug("=== Il2Cpp DeathScreen.Open called ===");
                
                // Check if mod is enabled
                if (!(SimpleSingleplayerRespawnMod.EnableMod?.Value ?? false))
                {
                    SimpleSingleplayerRespawnMod.LogDebug("Mod is disabled, using original DeathScreen behavior");
                    return;
                }

                // Get PlayerHealth patcher instance for session detection
                var playerHealthPatcher = RuntimePatcherFactory.CreatePlayerHealthPatcher();
                
                // Check if we're in multiplayer mode
                if (playerHealthPatcher.IsMultiplayerSession())
                {
                    SimpleSingleplayerRespawnMod.LogDebug("Multiplayer session detected, using original DeathScreen behavior");
                    return;
                }

                // Force respawn button to be shown and load save button to be hidden in singleplayer
                SimpleSingleplayerRespawnMod.LogDebug("Singleplayer session with mod enabled, forcing respawn button to show");
                
                // Immediately force the buttons to the correct state
                ForceButtonStates(__instance);
                
                // Also use MelonCoroutines to set buttons after the animation to ensure they stay correct
                MelonCoroutines.Start(ForceButtonStatesDelayed(__instance));
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in Open_Postfix: {ex}");
            }
        }

        /// <summary>
        /// Immediately forces the button states to the correct values.
        /// </summary>
        private static void ForceButtonStates(DeathScreen deathScreen)
        {
            try
            {
                if (deathScreen.respawnButton != null)
                {
                    deathScreen.respawnButton.gameObject.SetActive(true);
                    SimpleSingleplayerRespawnMod.LogDebug("Respawn button set to active (immediate)");
                }
                
                if (deathScreen.loadSaveButton != null)
                {
                    deathScreen.loadSaveButton.gameObject.SetActive(false);
                    SimpleSingleplayerRespawnMod.LogDebug("LoadSave button set to inactive (immediate)");
                }
            }
            catch (Exception ex)
            {
                SimpleSingleplayerRespawnMod.Instance.LoggerInstance.Error($"Error in ForceButtonStates: {ex}");
            }
        }

        /// <summary>
        /// Coroutine that sets the button states with a delay to override any other changes.
        /// </summary>
        private static System.Collections.IEnumerator ForceButtonStatesDelayed(DeathScreen deathScreen)
        {
            // Wait just after the animation starts but before UI becomes interactive (0.55f + small buffer)
            yield return new UnityEngine.WaitForSeconds(0.6f);
            
            try
            {
                SimpleSingleplayerRespawnMod.LogDebug("Setting button states after delay");
                
                // Force the buttons to the correct state
                if (deathScreen.respawnButton != null)
                {
                    deathScreen.respawnButton.gameObject.SetActive(true);
                    SimpleSingleplayerRespawnMod.LogDebug("Respawn button set to active (delayed)");
                }
                
                if (deathScreen.loadSaveButton != null)
                {
                    deathScreen.loadSaveButton.gameObject.SetActive(false);
                    SimpleSingleplayerRespawnMod.LogDebug("LoadSave button set to inactive (delayed)");
                }
            }
            catch (System.Exception ex)
            {
                SimpleSingleplayerRespawnMod.Instance.LoggerInstance.Error($"Error in ForceButtonStatesDelayed: {ex}");
            }
        }
    }
}