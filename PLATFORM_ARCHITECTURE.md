# JustAGame Ventures — Multi-Title Game Platform Architecture & Integration Guide

> **Version:** `1.0.0-robust`  
> **Target Audience:** Core Platform Engineers, Game Developers, External Interface & Launcher Integrators.  
> **Compliance:** Strict compliance with [DESIGN_PATTERNS.md](file:///c:/Users/T_CJ/JustAGame%20Ventures%20Case/DESIGN_PATTERNS.md).

---

## 1. Architectural Overview

This repository is engineered not merely as a standalone demo, but as a **reusable, production-grade multiplayer game platform**. Any game (racing, top-down shooter, party game, RPG, physics sandbox) can be built directly on top of this foundation without re-inventing networking, cross-platform identity, cloud sync, or external launcher connectivity.```
 ┌────────────────────────────────────────────────────────────────────────┐
 │                   EXTERNAL INTERFACES & LAUNCHERS                      │
 │      Web Dashboard  •  Electron Client  •  CLI Arguments  •  Deep Links│
 └───────────────────────────────────┬────────────────────────────────────┘
                                     │
 ┌───────────────────────────────────▼────────────────────────────────────┐
 │                GAMEPLATFORM FACADE (JustAGame.Core.Platform)           │
 │      GamePlatform.Auth  •  .Network  •  .NetworkManager  •  .Stats     │
 └──────┬────────────────────┬────────────────────┬────────────────┬──────┘
        │                    │                    │                │
 ┌──────▼──────┐   ┌─────────▼──────────┐   ┌─────▼──────────┐   ┌─▼───────────────┐
 │IPlatformAuth│   │IPlatformNetworkServ│   │INetworkManager │   │IPlatformStats   │
 │ • Device ID │   │ • Mirror P2P Host  │   │ • StartLocal   │   │ • Cloud Stats   │
 │ • Steam     │   │ • Mirror Client    │   │ • ConnectLocal │   │ • Achievements  │
 │ • DevAuth   │   │ • Whitelisting     │   │ • StartRemote  │   │ • Distance Ingest│
 │ • Epic Acct │   │ • Reconnection     │   │ • JoinRemote   │   │ • Local Profile │
 │             │   │                    │   │ • GetCode      │   │                 │
 └──────┬──────┘   └─────────┬──────────┘   └─────┬──────────┘   └─┬───────────────┘
        │                    │                    │                │
        │                    └──────────┬─────────┘                │
        │                               │                          │
        │                 ┌─────────────▼─────────────┐            │
        │                 │ EOSPlatformNetworkService │            │
        │                 │ (Dual-Interface Impl)     │            │
        │                 └─────────────┬─────────────┘            │
        │                               │                          │
 ┌──────▼───────────────────────────────▼──────────────────────────▼───────────────┐
 │                       CORE INFRASTRUCTURE ENGINES                              │
 │    Epic Online Services (v1.19)  •  Steamworks.NET  •  Mirror Netcode          │
 └────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Fast-Track: 3-Minute Integration for New Games

When building a new game mode or scene, game logic interacts exclusively with the static `GamePlatform` facade.

### A. Authentication
```csharp
using JustAGame.Core.Platform;

// 1. One-click Guest Device ID login:
GamePlatform.Auth.LoginWithDeviceId("Player1");

// 2. Steam login (auto-requests Steam WebApi session ticket):
GamePlatform.Auth.LoginWithSteam();

// 3. Multi-instance local testing via Epic DevAuthTool:
GamePlatform.Auth.LoginWithDevAuth("localhost:7878", "Player2");

// 4. Official Epic Games Account Portal:
GamePlatform.Auth.LoginWithEpicAccount();

// Hook into auth callbacks:
GamePlatform.Auth.OnLoginSuccess += (PlatformAuthInfo info) =>
{
    Debug.Log($"Welcome {info.DisplayName}! PUID: {info.ProductUserId}");
};
```

### B. Multiplayer Hosting & Joining (Advanced Platform Service)
```csharp
// Start hosting a P2P session:
GamePlatform.Network.StartHost();

// Join an existing host using their Epic Product User ID (PUID):
GamePlatform.Network.JoinHost(hostProductUserId);

// Gracefully stop session:
GamePlatform.Network.StopSession();

// Listen to session events:
GamePlatform.Network.OnClientConnected += () => Debug.Log("Connected to game match!");
GamePlatform.Network.OnClientDisconnected += (reason) => Debug.Log($"Session ended: {reason}");
```

### C. Unified Network Contract (`INetworkManager` & `NetworkStartResult`)
For external launchers, UI dashboards, and standardized game connectors, `GamePlatform.NetworkManager` exposes the `INetworkManager` interface, decoupling caller code from EOS-specific primitives and handling both LAN and Remote sessions:

```csharp
using JustAGame.Core.Platform;

// 1. Local LAN Hosting (localhost / Direct IP):
GamePlatform.NetworkManager.StartLocal();

// 2. Local LAN Client Connection:
GamePlatform.NetworkManager.ConnectLocal("192.168.1.100");

// 3. Remote EOS Relay Hosting:
NetworkStartResult hostResult = await GamePlatform.NetworkManager.StartRemote();
if (hostResult == NetworkStartResult.Success)
{
    string myJoinCode = GamePlatform.NetworkManager.GetCode();
    Debug.Log($"Hosting remote session. Share this code: {myJoinCode}");
}
else
{
    Debug.LogError($"Failed to start remote host: {hostResult}");
}

// 4. Remote EOS Relay Client Joining:
string targetCode = "0002b84f3e6a4b3d81829e01f02c4819";
NetworkStartResult joinResult = await GamePlatform.NetworkManager.JoinRemote(targetCode);
if (joinResult == NetworkStartResult.Success)
{
    Debug.Log("Connected to remote host successfully!");
}
else
{
    Debug.LogError($"Failed to join remote session: {joinResult}");
}
```

#### `NetworkStartResult` Status Codes:
| Code | Enum Value | Description |
| :--- | :--- | :--- |
| `Success` | `0` | Session started or joined successfully. |
| `NotInitialized` | `1` | EOS SDK or underlying network transport is not initialized. |
| `AlreadyActive` | `2` | A server, client, or host session is already running. |
| `InvalidCode` | `3` | Join code is empty, null, or not a valid 32-character hexadecimal PUID. |
| `MissingDependency` | `4` | `GameNetworkManager` or `EOSNetworkManagerBridge` component is missing from scene. |
| `InternalError` | `5` | Unhandled internal transport error or bridge exception. |
| `Timeout` | `6` | Connection attempt timed out before completing handshake. |

### D. Player Metrics & Achievements
```csharp
// Increment player distance or game score in the cloud:
GamePlatform.Stats.IngestDistanceMetric(10); // adds 10 meters

// Check unlock state:
bool unlocked = GamePlatform.Stats.IsCenturyWalkerUnlocked;

// Listen to unlocks:
GamePlatform.Stats.OnAchievementUnlocked += (achievementId) =>
{
    Debug.Log($"Achievement Unlocked: {achievementId}");
};
```

---

## 3. External Interface & Launcher Linking

The platform natively supports orchestration by **external desktop launchers, web portals, matchmakers, and CI test harnesses**.

### Command-Line Arguments (CLI)
Pass standard arguments when launching the game executable:

| Parameter | Example | Description |
| :--- | :--- | :--- |
| `-autologin <Type>` | `-autologin Steam` | Options: `DeviceId`, `Steam`, `DevAuth`, `Epic`. Logs in immediately at startup. |
| `-playername <Name>` | `-playername CyberNinja` | Sets user display name for Device ID / Guest sessions. |
| `-devauth-server <Addr>` | `-devauth-server localhost:7878` | Host and port for local Developer Authentication Tool. |
| `-devauth-cred <Cred>` | `-devauth-cred Player2` | Credential name for multi-client testing. |
| `-autohost` | `-autohost` | Automatically starts hosting immediately once authenticated. |
| `-autojoin <PUID>` | `-autojoin 00021b3...` | Automatically joins the specified host PUID once authenticated. |
| `-whitelist <PUIDs>` | `-whitelist PUID1,PUID2` | Restricts incoming connections to specified authorized peer list. |
| `-hideui` | `-hideui` | Hides manual login canvas for clean external launcher execution. |
| `-headless` / `-batchmode`| `-batchmode -nographics` | Runs as dedicated headless relay/server. |

#### CLI Automation Examples:
```powershell
# 1. Launcher starts Host with Steam:
"JustAGame Ventures Case.exe" -autologin Steam -autohost -hideui

# 2. Launcher starts Client joining Host on multi-instance DevAuth:
"JustAGame Ventures Case.exe" -autologin DevAuth -devauth-cred Player2 -autojoin 0002b84f3... -hideui
```

### Deep Linking Support (`PlatformDeepLinkHandler`)
The game automatically registers and processes custom URI schemes:
* **Join Match:** `justagame://join?host=<PUID>`
* **Host Match:** `justagame://host`

Clicking these links in web dashboards, Discord bots, or launcher interfaces launches/focuses the game and triggers the join flow automatically.

---

## 4. Robustness & Fault-Tolerance Guarantees

1. **Persistent Across Scenes (`DontDestroyOnLoad`):**
   * The `EOSManager` root GameObject (housing `EOSSDKComponent`, `EosTransport`, `EOSNetworkAuthenticator`, and `EOSNetworkManagerBridge`) is marked `DontDestroyOnLoad`.
   * Game developers can load arbitrary map scenes, lobby scenes, and result screens without tearing down the underlying P2P transport.

2. **Full Disconnection & Session Recovery:**
   * `GameNetworkManager` now overrides `OnClientConnect`, `OnClientDisconnect`, `OnClientError`, and `OnServerError`.
   * When a remote host disconnects or packet loss occurs, `OnEosSessionStopped` fires automatically, allowing the UI and game state to return cleanly to the main menu without hanging in an undefined state.

3. **Anti-Cheat & Identity Spoofing Protection:**
   * `EOSNetworkAuthenticator` performs zero-trust validation on every connection.
   * If a client claims a PUID that does not match their verified transport socket address, the connection is instantly rejected with an anti-spoofing alert.

4. **Zero-Slop Standard (`DESIGN_PATTERNS.md`):**
   * Every function is guarded with structured `try-catch` exception handling.
   * Every conditional `if` maintains an explicit `else` branch.
   * Zero memory allocations inside hot `Update()` loops.

5. **Unified Contract & Non-Throwing Remote Operations (`INetworkManager`):**
   * `StartRemote()` and `JoinRemote(code)` return typed `Task<NetworkStartResult>` enum outcomes instead of throwing uncaught runtime exceptions across process/UI boundaries.
   * Codes are pre-validated before network calls (e.g. verifying 32-character hexadecimal format for EOS Product User IDs).
   * Dual-mode architecture enables seamless toggling between local testing (`StartLocal`/`ConnectLocal`) and global peer-to-peer relay sessions without code rewriting.
