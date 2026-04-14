# Internal Debug Menu

Secure in-game developer debug tools for Unity mobile FPS projects.

## Security Model

- Disabled automatically in non-development builds through `DebugBuildGate`.
- Authorized only when one of these is true:
  - Offline sandbox session
  - Secure server-approved developer account
- Always disabled in public multiplayer sessions.
- All actions flow through internal adapter components. No external overlay, no memory patching, no runtime injection.
- Every action is audit logged through `DebugAuditLogger`.
- Runtime failures force a SAFE MODE fallback.

## Included Scripts

- `Scripts/Core/DebugManager.cs`
- `Scripts/Core/DebugAuthorizationBridge.cs`
- `Scripts/Core/DebugAuditLogger.cs`
- `Scripts/Core/DebugServiceAdapters.cs`
- `Scripts/Core/DebugMenuSettings.cs`
- `Scripts/Commands/DebugConsoleCommandProcessor.cs`
- `Scripts/Input/DebugMenuGestureActivator.cs`
- `Scripts/UI/DebugMenuRuntimeUI.cs`
- `Scripts/Data/WeaponSpawnCatalog.cs`

## Unity Setup

1. Import the `Assets/InternalDebugMenu` folder into your Unity project.
2. Create a `DebugMenuSettings` asset:
   - `Assets > Create > Internal Debug Menu > Settings`
3. Add a persistent bootstrap object to your first scene, for example `InternalDebugRoot`.
4. Add these components to the bootstrap object:
   - `DebugManager`
   - `DebugAuthorizationBridge`
   - `DebugAuditLogger`
   - `DebugMenuGestureActivator`
   - `DebugMenuRuntimeUI`
5. Assign your `DebugMenuSettings` asset to `DebugManager`.
6. Create concrete gameplay adapters by inheriting from:
   - `PlayerDebugServiceBase`
   - `WeaponDebugServiceBase`
   - `EnemyDebugServiceBase`
   - `VisualDebugServiceBase`
   - `NetworkSimulationServiceBase`
7. Assign those adapter components into the matching `DebugManager` fields.

## Minimal Mobile Smoke Test

If you want a fast device build before wiring real gameplay systems:

1. Create an empty scene.
2. Add a GameObject named `InternalDebugRoot`.
3. Add these components to it:
   - `DebugDevelopmentOfflineBootstrap`
   - `SamplePlayerDebugService`
   - `SampleWeaponDebugService`
   - `SampleEnemyDebugService`
   - `SampleVisualDebugService`
   - `SampleNetworkSimulationService`
4. Press Play in editor and open the menu with `F10`.
5. For Android/iOS, make a `Development Build` and open the menu with the three-finger hold gesture.

These sample services only log actions. They are for UI and pipeline verification, not for real gameplay control.

## Secure Authorization Wiring

Use the bridge only with trusted session state from your own auth/session pipeline.

```csharp
using InternalDebugMenu;

public sealed class SessionDebugAuthBinder : MonoBehaviour
{
    [SerializeField] private DebugAuthorizationBridge authorizationBridge;

    public void OnSessionChanged(bool isOfflineMode, bool isPublicMultiplayerMatch)
    {
        authorizationBridge.UpdateSessionState(isOfflineMode, isPublicMultiplayerMatch);
    }

    public void OnDeveloperAuthReceived(string accountId, bool isDeveloper, bool secureDeveloperFlag)
    {
        authorizationBridge.ApplySecureServerAuthorization(accountId, isDeveloper, secureDeveloperFlag);
    }
}
```

Rules:

- `secureDeveloperFlag` must come from your backend, not local prefs or player data.
- When entering public matchmaking, call `UpdateSessionState(false, true)`.
- When auth is lost or refreshed, call `ClearServerAuthorization()` and push a new secure snapshot.

## Secret Code Hash

Do not store the plaintext code in the asset.

Use `DebugCodeUtility.ComputeSha256` once in editor tooling or a temporary script, then paste the hash into `DebugMenuSettings.accessCodeSha256`.

Example plaintext:

```text
dev_access_247
```

Example hash generation:

```csharp
var hash = InternalDebugMenu.DebugCodeUtility.ComputeSha256("dev_access_247");
Debug.Log(hash);
```

## Gameplay Adapter Guidance

Each adapter must call your normal gameplay APIs only.

- `PlayerDebugServiceBase`
  - Apply invincibility through your health/damage service.
  - Apply speed changes through your movement controller.
- `WeaponDebugServiceBase`
  - Route infinite ammo and instant reload through the weapon/inventory service.
  - Validate weapon IDs against a controlled spawn catalog.
- `EnemyDebugServiceBase`
  - Spawn only inside offline/dev sandbox contexts.
  - Clamp enemy count and use your AI director/spawner.
- `VisualDebugServiceBase`
  - Reuse your own debug renderer, gizmo layer, or outline component.
- `NetworkSimulationServiceBase`
  - Wrap your existing transport/network abstraction instead of patching packets directly.

## Activation

- Hidden gesture: three-finger hold.
- Optional editor shortcut: `F10`.
- Optional secret code gate.

## Console Commands

- `god`
- `speed 2.0`
- `heal`
- `spawn enemy 5`
- `spawn weapon weapon_ak`
- `ammo on`
- `reload on`
- `ai freeze on`
- `difficulty 1.5`
- `hitboxes on`
- `raycasts on`
- `outline on`
- `latency 150`
- `packetloss 10`
- `synclogs on`
- `safe on`

## Performance Notes

- The UI is built once at runtime and hidden with `CanvasGroup`.
- Debug rendering should be pooled and toggled, not recreated every frame.
- Keep hitbox and raycast rendering disabled by default on mobile hardware.
- Network simulation should sit behind your transport abstraction so it can be compiled out of release configurations.
