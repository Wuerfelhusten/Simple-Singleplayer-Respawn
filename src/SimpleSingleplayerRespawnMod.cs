// =============================================================================
// Copyright (c) 2026 Modding Forge
// This file is part of Simple Singleplayer Respawn
// and falls under the license GPLv3.
// =============================================================================

using System;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using SimpleSingleplayerRespawn.Core;

namespace SimpleSingleplayerRespawn
{
    /// <summary>
    /// Main mod class for Simple Singleplayer Respawn.
    /// Changes singleplayer death to use multiplayer-style respawn with hospital bills
    /// instead of loading the last save.
    /// </summary>
    public class SimpleSingleplayerRespawnMod : MelonMod
    {
        #region Configuration Categories

        /// <summary>Main settings category.</summary>
        public static MelonPreferences_Category MainCategory = null;

        #endregion

        #region Configuration Entries

        /// <summary>Whether to enable debug logging.</summary>
        public static MelonPreferences_Entry<bool> EnableDebugMode = null;

        /// <summary>Whether the mod is enabled.</summary>
        public static MelonPreferences_Entry<bool> EnableMod = null;

        #endregion

        #region Private Fields

        /// <summary>Logger instance for this mod.</summary>
        private static MelonLogger.Instance Logger => Instance.LoggerInstance;

        /// <summary>Singleton instance of this mod.</summary>
        public static SimpleSingleplayerRespawnMod Instance { get; private set; } = null;

        /// <summary>Harmony instance for patching.</summary>
        private static HarmonyLib.Harmony _harmonyInstance;
        
        /// <summary>Runtime-specific patch implementations.</summary>
        private IPlayerHealthPatcher _playerHealthPatcher;
        private IDeathScreenPatcher _deathScreenPatcher;

        #endregion

        #region MelonMod Lifecycle

        /// <summary>
        /// Called when the mod is initialized.
        /// </summary>
        public override void OnInitializeMelon()
        {
            Instance = this;
            
            try
            {
                InitializeConfiguration();
                
                // Log runtime information (debug only)
                if (EnableDebugMode?.Value ?? false)
                {
                    RuntimeDetector.LogRuntimeInfo();
                }
                
                // Check if mod is enabled
                if (EnableMod?.Value ?? false)
                {
                    InitializeHarmony();
                    LoggerInstance.Msg($"Simple Singleplayer Respawn enabled ({RuntimeDetector.RuntimeName})");
                }
                else
                {
                    LoggerInstance.Msg("Simple Singleplayer Respawn disabled");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to initialize Schedule1Mod: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Called when the scene is loaded.
        /// </summary>
        /// <param name="buildIndex">The build index of the scene.</param>
        /// <param name="sceneName">The name of the scene.</param>
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            try
            {
                if (EnableDebugMode?.Value ?? false)
                {
                    Logger.Msg($"Scene loaded: {sceneName} (Index: {buildIndex})");
                }
                
                // TODO: Add scene-specific logic here
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in scene {sceneName}: {ex}");
            }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Initializes the mod configuration.
        /// </summary>
        private void InitializeConfiguration()
        {
            MainCategory = MelonPreferences.CreateCategory("SimpleSingleplayerRespawn");
            MainCategory.SetFilePath("UserData/SimpleSingleplayerRespawn.cfg");

            EnableDebugMode = MainCategory.CreateEntry(
                "EnableDebugMode",
                false,
                "Enable Debug Mode",
                "Whether to enable detailed debug logging."
            );

            EnableMod = MainCategory.CreateEntry(
                "EnableMod",
                true,
                "Enable Mod",
                "Whether the mod functionality is enabled."
            );

            LogDebug("Configuration initialized");
        }

        /// <summary>
        /// Initializes Harmony patching.
        /// </summary>
        private void InitializeHarmony()
        {
            try
            {
                if (!EnableMod.Value)
                {
                    LogDebug("Mod is disabled, skipping Harmony patches");
                    return;
                }

                _harmonyInstance = new HarmonyLib.Harmony("modding-forge.simple-singleplayer-respawn");
                
                // Create runtime-specific patchers
                _playerHealthPatcher = RuntimePatcherFactory.CreatePlayerHealthPatcher();
                _deathScreenPatcher = RuntimePatcherFactory.CreateDeathScreenPatcher();
                
                // Apply patches
                _playerHealthPatcher.ApplyPatches();
                _deathScreenPatcher.ApplyPatches();

                LogDebug($"Harmony patches applied successfully for {RuntimeDetector.RuntimeName}");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to initialize Harmony: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Cleans up Harmony patches when the mod is unloaded.
        /// </summary>
        public override void OnApplicationQuit()
        {
            try
            {
                _harmonyInstance?.UnpatchSelf();
                LogDebug("Harmony patches removed");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error removing Harmony patches: {ex}");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Logs a debug message if debug mode is enabled.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogDebug(string message)
        {
            if (EnableDebugMode?.Value ?? false)
            {
                Logger.Msg($"[DEBUG] {message}");
            }
        }

        #endregion
    }
}