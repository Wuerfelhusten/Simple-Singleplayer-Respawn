# Simple Singleplayer Respawn

MelonLoader Mod for *Schedule I*: replaces the default **singleplayer** death behavior (reload last save) with the **multiplayer-style respawn** (respawn at the medical center + hospital bill).

## Installation

1. Install **MelonLoader** for your game runtime.
2. Download `SimpleSingleplayerRespawn.dll` from the GitHub Release.
3. Copy the DLL into the correct `Mods` folder:
  - **IL2CPP build:** `Schedule I/Mods/`
  - **Mono build:** `Schedule I - Mono/Mods/`
4. Start the game once. The config file will be created at `UserData/SimpleSingleplayerRespawn.cfg`.

## Mono vs. IL2CPP

This mod contains **both** Mono and IL2CPP implementations and auto-detects the runtime at startup.

- **Mono:** game code is managed C# assemblies; patches use reflection/Harmony.
- **IL2CPP:** game code is AOT native; patches use Il2CppInterop bindings.

If you maintain two separate installs (one Mono, one IL2CPP), install the DLL into each install’s `Mods` folder.

---

# Developer Documentation

## Project Overview

**Simple Singleplayer Respawn** is a MelonLoader mod for *Schedule I* that replaces the default singleplayer death mechanic with the multiplayer respawn system. Instead of reloading the last save when dying in singleplayer, players respawn at the medical center with hospital bills, exactly like in multiplayer mode.

### Key Technical Features
- **Harmony Patching**: Non-destructive hooks into `PlayerHealth` and `DeathScreen` classes.
- **Zero Custom Logic**: Uses the existing multiplayer death system without reimplementation.
- **Smart Detection**: Automatically differentiates between singleplayer and multiplayer sessions.
- **Runtime UI Manipulation**: Forces correct button visibility in the death screen.

## Architecture

### Directory Structure
```
src/
├── SimpleSingleplayerRespawnMod.cs   # Main mod class and configuration
├── Core/
│   ├── RuntimeDetector.cs           # Detects Mono vs Il2Cpp
│   └── IRuntimePatchers.cs          # Runtime patcher interfaces/factory
└── Runtime/
  ├── Il2Cpp/                      # Il2Cpp-specific patchers
  └── Mono/                        # Mono-specific patchers
```

### Core Components

#### 1. PlayerHealth Patching
The `PlayerHealthPatch` intercepts the RPC logic to override singleplayer death behavior:
- **Target Method**: `RpcLogic___SendDie_2166136261` - the core death processing method.
- **Logic**: In singleplayer, skips the original method and directly calls `RpcLogic___Die_2166136261()`.
- **Multiplayer Preservation**: Leaves multiplayer sessions completely untouched.

#### 2. DeathScreen UI Management
The `DeathScreenPatch` ensures the correct UI is displayed:
- **Target Method**: `Open()` - called when the death screen is shown.
- **Strategy**: Forces respawn button visibility immediately and after animation delay.
- **Button Logic**: 
  - `respawnButton.gameObject.SetActive(true)` - Shows respawn option.
  - `loadSaveButton.gameObject.SetActive(false)` - Hides load save option.

#### 3. Session Detection
Robust multiplayer detection using multiple indicators:
- **NetworkManager**: Checks for FishNet NetworkManager existence and state.
- **Connection Count**: Counts authenticated network connections.
- **Player Count**: Analyzes PlayerHealth objects in scene.
- **Client/Host Status**: Detects if connected as client to remote server.

## Technical Implementation

### Death Flow Transformation

**Original Singleplayer Flow:**
```
Player Dies → SendDie() → RpcLogic___SendDie_2166136261() 
→ Singleplayer Logic → DeathScreen Shows "Load Save"
```

**Modded Singleplayer Flow:**
```
Player Dies → SendDie() → [INTERCEPTED] RpcLogic___Die_2166136261() 
→ Multiplayer Logic → DeathScreen Shows "Respawn" → HospitalBillScreen
```

### Hospital Bill Integration
The mod leverages the existing `HospitalBillScreen` system:
- **Amount**: Uses game's default `250f` cost.
- **Processing**: Handled by original `MoneyManager.ChangeCashBalance()`.
- **UI Flow**: Original respawn coroutine manages the complete flow.

## Build Instructions

### Prerequisites
- **Visual Studio 2022** (or newer) with **.NET Framework 4.7.2** targeting pack.
- **MelonLoader** installed in the game directory.
- **Game Assemblies**: References Il2Cpp assemblies from `Schedule I/MelonLoader/net6/`.

### Building
1. **Clone/Open**: Open `SimpleSingleplayerRespawn.sln`.
2. **Restore**: Run `dotnet restore`.
3. **Configuration**:
   - Ensure `SimpleSingleplayerRespawn.csproj` points to correct game directory for references.
   - Verify `<Reference Include="...">` paths for MelonLoader and Il2Cpp assemblies.
4. **Compile**: Build in **Debug** or **Release** mode.
5. **Output**: `SimpleSingleplayerRespawn.dll` generated in `bin/Debug/` (project targets `net472`).

### Post-Build
The project includes automatic deployment:
- **Target**: Copies DLL to `Schedule I/Mods/` folder.
- **Configuration**: Post-build event in project file handles deployment.

## Configuration

The mod creates `UserData/SimpleSingleplayerRespawn.cfg` with these options:
- **EnableMod** (bool, default: true): Master toggle for mod functionality.
- **EnableDebugMode** (bool, default: false): Enables detailed logging for troubleshooting.

## Troubleshooting

### Debug Logging
Enable debug mode to see detailed session detection and patch execution:
```
[Simple_Singleplayer_Respawn] [DEBUG] === SendDie RpcLogic called ===
[Simple_Singleplayer_Respawn] [DEBUG] Singleplayer session detected, implementing multiplayer-style death
[Simple_Singleplayer_Respawn] [DEBUG] === DeathScreen.Open called ===
[Simple_Singleplayer_Respawn] [DEBUG] Respawn button set to active (immediate)
```

### Common Issues
- **Still shows Load Save**: Check if mod is enabled in config and properly loaded.
- **Multiplayer affected**: Verify session detection logic with debug logging.
- **No hospital bill**: Ensure original game systems are functional.

## Contribution Guidelines

Pull Requests are welcome! Please ensure adherence to the GPLv3 License.

### Code Style
- **Headers**: All files must include the standard GPLv3 copyright header.
- **Naming**: PascalCase for public members, camelCase for local variables.
- **Logging**: Use `SimpleSingleplayerRespawnMod.LogDebug()` for development logs.
- **Documentation**: XML documentation comments for all public methods.

### Adding Features
1. **Maintain Zero Logic**: Avoid reimplementing game mechanics.
2. **Preserve Multiplayer**: Ensure all changes only affect singleplayer.
3. **Test Both Modes**: Verify functionality in both single and multiplayer.

## License

This project is licensed under the **GNU General Public License v3.0**. See [LICENSE](LICENSE) for details.

Copyright (c) 2026 Modding Forge.