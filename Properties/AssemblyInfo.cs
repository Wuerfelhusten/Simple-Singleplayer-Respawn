using System.Reflection;
using MelonLoader;

// Assembly info
[assembly: AssemblyTitle("Simple Singleplayer Respawn")]
[assembly: AssemblyDescription("Changes singleplayer death to use multiplayer-style respawn instead of loading save")]
[assembly: AssemblyCompany("Modding Forge")]
[assembly: AssemblyProduct("Simple Singleplayer Respawn")]
[assembly: AssemblyCopyright("Copyright (c) 2026 Modding Forge")]
[assembly: AssemblyVersion("1.0.0")]
[assembly: AssemblyFileVersion("1.0.0")]

// MelonLoader attributes
[assembly: MelonInfo(typeof(SimpleSingleplayerRespawn.SimpleSingleplayerRespawnMod), "Simple Singleplayer Respawn", "1.0.0", "Modding Forge")]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: MelonColor(255, 165, 0, 255)] // Orange color for console
[assembly: MelonOptionalDependencies("Il2CppFishNet.Runtime", "Il2CppInterop.Runtime", "Il2Cppmscorlib")]
