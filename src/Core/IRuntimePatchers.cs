// =============================================================================
// Copyright (c) 2026 Modding Forge
// This file is part of Simple Singleplayer Respawn
// and falls under the license GPLv3.
// =============================================================================

using System;

namespace SimpleSingleplayerRespawn.Core
{
    /// <summary>
    /// Interface for runtime-specific PlayerHealth patching implementations.
    /// </summary>
    public interface IPlayerHealthPatcher
    {
        /// <summary>
        /// Applies the PlayerHealth patches for the current runtime.
        /// </summary>
        void ApplyPatches();
        
        /// <summary>
        /// Checks if the current session is multiplayer.
        /// </summary>
        /// <returns>True if multiplayer, false if singleplayer</returns>
        bool IsMultiplayerSession();
    }
    
    /// <summary>
    /// Interface for runtime-specific DeathScreen patching implementations.
    /// </summary>
    public interface IDeathScreenPatcher
    {
        /// <summary>
        /// Applies the DeathScreen patches for the current runtime.
        /// </summary>
        void ApplyPatches();
    }
    
    /// <summary>
    /// Factory for creating runtime-specific patch implementations.
    /// </summary>
    public static class RuntimePatcherFactory
    {
        /// <summary>
        /// Creates a PlayerHealth patcher for the current runtime.
        /// </summary>
        /// <returns>Runtime-specific PlayerHealth patcher</returns>
        public static IPlayerHealthPatcher CreatePlayerHealthPatcher()
        {
            if (RuntimeDetector.IsIl2Cpp)
            {
                return CreateIl2CppPlayerHealthPatcher();
            }
            else
            {
                return CreateMonoPlayerHealthPatcher();
            }
        }
        
        /// <summary>
        /// Creates a DeathScreen patcher for the current runtime.
        /// </summary>
        /// <returns>Runtime-specific DeathScreen patcher</returns>
        public static IDeathScreenPatcher CreateDeathScreenPatcher()
        {
            if (RuntimeDetector.IsIl2Cpp)
            {
                return CreateIl2CppDeathScreenPatcher();
            }
            else
            {
                return CreateMonoDeathScreenPatcher();
            }
        }
        
        private static IPlayerHealthPatcher CreateIl2CppPlayerHealthPatcher()
        {
            var type = Type.GetType("SimpleSingleplayerRespawn.Runtime.Il2Cpp.Il2CppPlayerHealthPatcher");
            if (type == null)
                throw new InvalidOperationException("Il2Cpp PlayerHealth patcher not found");
                
            return (IPlayerHealthPatcher)Activator.CreateInstance(type);
        }
        
        private static IDeathScreenPatcher CreateIl2CppDeathScreenPatcher()
        {
            var type = Type.GetType("SimpleSingleplayerRespawn.Runtime.Il2Cpp.Il2CppDeathScreenPatcher");
            if (type == null)
                throw new InvalidOperationException("Il2Cpp DeathScreen patcher not found");
                
            return (IDeathScreenPatcher)Activator.CreateInstance(type);
        }
        
        private static IPlayerHealthPatcher CreateMonoPlayerHealthPatcher()
        {
            var type = Type.GetType("SimpleSingleplayerRespawn.Runtime.Mono.MonoPlayerHealthPatcher");
            if (type == null)
                throw new InvalidOperationException("Mono PlayerHealth patcher not found");
                
            return (IPlayerHealthPatcher)Activator.CreateInstance(type);
        }
        
        private static IDeathScreenPatcher CreateMonoDeathScreenPatcher()
        {
            var type = Type.GetType("SimpleSingleplayerRespawn.Runtime.Mono.MonoDeathScreenPatcher");
            if (type == null)
                throw new InvalidOperationException("Mono DeathScreen patcher not found");
                
            return (IDeathScreenPatcher)Activator.CreateInstance(type);
        }
    }
}