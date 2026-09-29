# JustAGame Ventures — Multi-Title Game Platform Architecture & Integration Guide

> **Version:** `1.0.0-robust`  
> **Target Audience:** Core Platform Engineers, Game Developers, External Interface & Launcher Integrators.  
> **Compliance:** Strict compliance with [DESIGN_PATTERNS.md](file:///c:/Users/T_CJ/JustAGame%20Ventures%20Case/DESIGN_PATTERNS.md).

---

## 1. Architectural Overview

This repository is engineered not merely as a standalone demo, but as a **reusable, production-grade multiplayer game platform**. Any game (racing, top-down shooter, party game, RPG, physics sandbox) can be built directly on top of this foundation without re-inventing networking, cross-platform identity, cloud sync, or external launcher connectivity.

```
 ┌────────────────────────────────────────────────────────────────────────┐
 │                   EXTERNAL INTERFACES & LAUNCHERS                      │
 │      Web Dashboard  •  Electron Client  •  CLI Arguments  •  Deep Links│
 └───────────────────────────────────┬────────────────────────────────────┘
                                     │
 ┌───────────────────────────────────▼────────────────────────────────────┐
 │                GAMEPLATFORM FACADE (JustAGame.Core.Platform)           │
 │               GamePlatform.Auth  •  .Network  •  .Stats                │
 └──────────────┬────────────────────┬────────────────────┬───────────────┘
                │                    │                    │
 ┌──────────────▼──────┐   ┌─────────▼──────────┐   ┌─────▼───────────────┐
 │ IPlatformAuthService│   │IPlatformNetworkServ│   │IPlatformStatsService│
 │ • Device ID (Guest) │   │ • Mirror P2P Host  │   │ • Cloud Stats Ingest│
 │ • Steam WebApi      │   │ • Mirror Client    │   │ • EOS Achievements  │
 │ • DevAuthTool       │   │ • PUID Whitelisting│   │ • Distance Metric   │
 │ • Epic Account      │   │ • Auto-Reconnection│   │ • Local Profile Red │
 └──────────────┬──────┘   └─────────┬──────────┘   └─────┬───────────────┘
                │                    │                    │
 ┌──────────────▼────────────────────▼────────────────────▼───────────────┐
 │                       CORE INFRASTRUCTURE ENGINES                      │
 │    Epic Online Services (v1.19)  •  Steamworks.NET  •  Mirror Netcode  │
 └────────────────────────────────────────────────────────────────────────┘
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

### B. Multiplayer Hosting & Joining
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

### C. Player Metrics & Achievements
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
