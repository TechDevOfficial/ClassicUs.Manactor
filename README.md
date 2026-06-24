# ClassicUs.Manactor

Networking framework for **Classic Us** (Among Us) BepInEx IL2CPP mods.

## Features
- **Automatic handshake** — broadcasts your mod list to all players when joining a lobby
- **Lobby tracker** — knows which players have Manactor and which mods they're running
- **Version check** — detects version mismatches across players
- **Unmodded lobby detection** — fires an event if the host doesn't have Manactor

## Usage

```csharp
// In your BepInEx plugin Load():
ManactorAPI.Register("YourModName", "1.0.0");

// Events
ManactorAPI.OnPlayerModded += (playerId, mods) => { };
ManactorAPI.OnLobbyFullyModded += () => { };
ManactorAPI.OnJoiningUnmoddedLobby += () => { };
ManactorAPI.OnModVersionMismatch += (playerId, mod, localVer, remoteVer) => { };

// Queries
bool ok = ManactorAPI.IsCompatibleToPlay();
List<byte> unmodded = ManactorAPI.GetUnmoddedPlayers();
```

## Requirements
- BepInEx IL2CPP for Classic Us
- `ClassicUs.Manactor.dll` installed in `BepInEx/plugins/`
