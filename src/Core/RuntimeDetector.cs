// =============================================================================
// Copyright (c) 2026 Modding Forge
// This file is part of Simple Singleplayer Respawn
// and falls under the license GPLv3.
// =============================================================================

using System;
using System.Reflection;
using MelonLoader;

namespace SimpleSingleplayerRespawn.Core
{
    /// <summary>
    /// Detects whether the game is running on Mono or Il2Cpp runtime.
    /// </summary>
    public static class RuntimeDetector
    {
        private static bool? _isIl2Cpp;
        
        /// <summary>
        /// Gets whether the current runtime is Il2Cpp.
        /// </summary>
        public static bool IsIl2Cpp
        {
            get
            {
                if (_isIl2Cpp.HasValue)
                    return _isIl2Cpp.Value;
                    
                _isIl2Cpp = DetectRuntimeType();
                return _isIl2Cpp.Value;
            }
        }
        
        /// <summary>
        /// Gets whether the current runtime is Mono.
        /// </summary>
        public static bool IsMono => !IsIl2Cpp;
        
        /// <summary>
        /// Gets a string representation of the current runtime.
        /// </summary>
        public static string RuntimeName => IsIl2Cpp ? "Il2Cpp" : "Mono";
        
        /// <summary>
        /// Detects the runtime type by checking for Il2Cpp-specific assemblies and types.
        /// </summary>
        /// <returns>True if Il2Cpp, false if Mono</returns>
        private static bool DetectRuntimeType()
        {
            try
            {
                // Method 1: Check for Il2CppInterop assembly
                try
                {
                    Assembly.Load("Il2CppInterop.Runtime");
                    return true;
                }
                catch
                {
                    // Il2CppInterop.Runtime not found, continue checking
                }
                
                // Method 2: Check for Il2Cpp-specific types in current assemblies
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var assemblyName = assembly.GetName().Name;
                        if (assemblyName != null && 
                            (assemblyName.StartsWith("Il2Cpp") || 
                             assemblyName.Contains("Il2CppInterop")))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // Skip assemblies that can't be examined
                        continue;
                    }
                }
                
                // Method 3: Check MelonLoader's domain type
                var melonLoaderAssembly = typeof(MelonMod).Assembly;
                var melonLoaderLocation = melonLoaderAssembly.Location;
                if (!string.IsNullOrEmpty(melonLoaderLocation))
                {
                    // Il2Cpp builds typically have MelonLoader in a different path structure
                    if (melonLoaderLocation.Contains("net6") || melonLoaderLocation.Contains("Il2Cpp"))
                    {
                        return true;
                    }
                }
                
                // Default to Mono if no Il2Cpp indicators found
                return false;
            }
            catch (Exception ex)
            {
                // If detection fails, log error and default to Mono for safety
                MelonLogger.Error($"RuntimeDetector failed to detect runtime type: {ex}");
                return false;
            }
        }
        
        /// <summary>
        /// Logs the detected runtime information.
        /// </summary>
        public static void LogRuntimeInfo()
        {
            SimpleSingleplayerRespawn.SimpleSingleplayerRespawnMod.LogDebug($"Detected Runtime: {RuntimeName}");
            SimpleSingleplayerRespawn.SimpleSingleplayerRespawnMod.LogDebug($"CLR Version: {Environment.Version}");
            
            try
            {
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    SimpleSingleplayerRespawn.SimpleSingleplayerRespawnMod.LogDebug($"Entry Assembly: {entryAssembly.GetName().Name}");
                }
                
                var melonLoaderAssembly = typeof(MelonMod).Assembly;
                SimpleSingleplayerRespawn.SimpleSingleplayerRespawnMod.LogDebug($"MelonLoader Location: {melonLoaderAssembly.Location}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Could not gather additional runtime info: {ex.Message}");
            }
        }
    }
}