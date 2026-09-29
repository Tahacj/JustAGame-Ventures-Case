# JustAGame Ventures — AI & Engineering Design Pattern Guidelines

> **Mandatory Standard for All AI Agents and Developers**  
> Every script written or modified in this repository MUST strictly follow the design rules defined in this document. Submissions containing "AI slop", unhandled branches, naked null checks, or unmanaged exceptions are unacceptable.

---

## 1. Exhaustive Branching (No `if` Without `else`)

* **Rule:** Every conditional `if` statement MUST have an explicit `else` block.
* **Rationale:** Implicit fallthroughs create hidden edge-case regressions and ambiguous states in multiplayer games.
* **Standard:**
  ```csharp
  //  CORRECT:
  if (playerInventory.IsNotNull() && playerInventory.isOwned)
  {
      OnPlayerEnteredZone(playerInventory);
  }
  else
  {
      // Ignored: entity is null, remote player, or non-player collider
  }

  // ❌ FORBIDDEN (AI Slop):
  if (playerInventory != null)
      OnPlayerEnteredZone(playerInventory);
  ```

---

## 2. Comprehensive Exception Handling (No Error Without `catch`)

* **Rule:** All Unity lifecycle callbacks (`Awake`, `Start`, `Update`, `OnEnable`, `OnDisable`, `OnDestroy`), event subscriptions, network callbacks, and business functions MUST be encapsulated within `try-catch` blocks.
* **Standard:**
  ```csharp
  public void ExecuteAction()
  {
      try
      {
          // Functional logic here
      }
      catch (Exception ex)
      {
          Debug.LogError($"[{GetType().Name}] Exception in ExecuteAction: {ex.Message}");
      }
  }
  ```
* Never catch exceptions silently. Always log with component context (`[{GetType().Name}] Exception in ...`).

---

## 3. Strict Null Checking & Safe Extension Queries

* **Rule:** Do NOT use raw `== null` or `!= null` for Unity Objects or class instances.
* **Standard Methods (from `JustAGame.ObjectExtensions`):**
  | Forbidden Naked Syntax | Required Extension Syntax |
  | :--- | :--- |
  | `obj == null` | `obj.IsNull()` |
  | `obj != null` | `obj.IsNotNull()` |
  | `GetComponent<T>()` | `this.GetComponentOrNull<T>()` |
  | `GetComponentInChildren<T>()` | `this.GetInChildrenOrNull<T>()` |
  | `GetComponentInParent<T>()` | `this.GetComponentInParentOrNull<T>()` |
  | `a != null ? a : b` | `a.IsNotNull() ? a : b` |

---

## 4. Zero Slop & High-Performance Optimization

* **No Garbage Collection in Hot Paths:**
  * Never instantiate or destroy objects repeatedly in gameplay loops. Use `JustAGame.Pooling.ObjectPool<T>`.
  * In UI and string concatenation, use a cached `StringBuilder` instead of string interpolation `$"..."` inside `Update()` or collection loops.
* **Method Inlining:**
  * Tag performance-critical utility and extension methods with `[MethodImpl(MethodImplOptions.AggressiveInlining)]`.
* **Lean Code:**
  * Avoid unnecessary proxy classes, dummy interfaces, or duplicate data holders. Keep classes focused, cohesive, and performant.

---

## 5. Mirror Networking & Epic Online Services (EOS) Architecture

* **Zero-Trust Server Authority:**
  * Clients only send *intents* via `[Command]` (e.g., `CmdRequestPurchase(int itemId)`).
  * The Server validates all business logic:
    1. Distance & proximity checks
    2. Action cooldown timers
    3. Currency & inventory capacity
    4. Entity ownership and authority
  * Server replicates state via `[SyncVar]`, `SyncList<T>`, or `[TargetRpc]`.
* **EOS P2P Addressing:**
  * In EOS Transport, network addresses are **Epic Product User IDs** (`ProductUserId` strings), NOT IP addresses or hostnames.
  * Connect using `EOSNetworkManagerBridge.Instance.StartEosClient(hostProductUserId)`.
  * Host using `EOSNetworkManagerBridge.Instance.StartEosHost()`.
* **Client Synchronization Guards:**
  * Input processing scripts (like `ClientAuthoritativeMovement`) must verify `isOwned` AND `NetworkClient.ready` before reading inputs or moving.
* **Steam to EOS Cross-Platform Identity Standards:**
  * Always use modern asynchronous Steam WebApi session tickets (`SteamUser.GetAuthTicketForWebApi("epiconlineservices")`) paired with `ExternalCredentialType.SteamSessionTicket` (enum value `18`).
  * Never use legacy Steam Encrypted App Tickets for test App IDs (like `480`), as Valve does not provide private encryption keys.
  * All native x64 EOS SDK runtime binaries must be v1.15.1 or newer (officially v1.19.2.1) to avoid enum bounds validation rejections in `Connect.Login`.

---

## 6. Verification Checklist Before Code Commit

- [ ] Every `if` has an explicit `else`.
- [ ] Every function has a `try-catch` block.
- [ ] All null checks use `.IsNull()` / `.IsNotNull()`.
- [ ] Component queries use `GetComponentOrNull<T>()` / `GetInChildrenOrNull<T>()` / `GetComponentInParentOrNull<T>()`.
- [ ] Zero GC allocations inside `Update()`.
- [ ] Network commands are validated on the server.
