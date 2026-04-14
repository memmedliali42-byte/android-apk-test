using System;
using System.Collections.Generic;
using UnityEngine;

namespace InternalDebugMenu
{
    public sealed class DebugManager : MonoBehaviour
    {
        public static DebugManager Instance { get; private set; }

        [Header("Core References")]
        [SerializeField] private DebugMenuSettings settings;
        [SerializeField] private DebugAuthorizationBridge authorizationBridge;
        [SerializeField] private DebugAuditLogger auditLogger;

        [Header("Gameplay Adapters")]
        [SerializeField] private PlayerDebugServiceBase playerService;
        [SerializeField] private WeaponDebugServiceBase weaponService;
        [SerializeField] private EnemyDebugServiceBase enemyService;
        [SerializeField] private VisualDebugServiceBase visualService;
        [SerializeField] private NetworkSimulationServiceBase networkService;

        [Header("Lifecycle")]
        [SerializeField] private bool persistAcrossScenes = true;

        private DebugConsoleCommandProcessor commandProcessor;
        private DebugAuthorizationState authorizationState;
        private bool menuVisible;
        private bool accessCodeValidated;
        private bool safeModeEnabled;
        private bool godModeEnabled;
        private bool infiniteAmmoEnabled;
        private bool instantReloadEnabled;
        private bool aiFrozen;
        private bool hitboxesVisible;
        private bool raycastsVisible;
        private bool enemyOutlineVisible;
        private bool syncLogsEnabled;
        private float movementSpeedMultiplier = 1.0f;
        private float difficultyMultiplier = 1.0f;
        private int latencyMs;
        private int packetLossPercent;

        public event Action StateChanged;
        public event Action<string> ConsoleResponseLogged;

        public DebugMenuSettings Settings => settings;
        public DebugAuthorizationState AuthorizationState => authorizationState;
        public DebugAuditLogger AuditLogger => auditLogger;
        public bool IsSupportedBuild => DebugBuildGate.IsBuildSupported;
        public bool IsMenuVisible => menuVisible;
        public bool IsSafeModeEnabled => safeModeEnabled;
        public bool IsAccessCodeUnlocked => accessCodeValidated || !settings.RequiresSecretCode;
        public bool CanShowMenu => authorizationState.IsAuthorized;
        public bool CanExecuteActions => authorizationState.IsAuthorized && IsAccessCodeUnlocked && !safeModeEnabled;
        public bool HasPlayerTools => playerService != null;
        public bool HasWeaponTools => weaponService != null;
        public bool HasEnemyTools => enemyService != null;
        public bool HasVisualTools => visualService != null;
        public bool HasNetworkTools => networkService != null;
        public IReadOnlyList<string> AvailableWeaponIds => weaponService != null ? weaponService.AvailableWeaponIds : Array.Empty<string>();
        public float MovementSpeedMultiplier => movementSpeedMultiplier;
        public float DifficultyMultiplier => difficultyMultiplier;
        public int LatencyMs => latencyMs;
        public int PacketLossPercent => packetLossPercent;
        public bool GodModeEnabled => godModeEnabled;
        public bool InfiniteAmmoEnabled => infiniteAmmoEnabled;
        public bool InstantReloadEnabled => instantReloadEnabled;
        public bool AiFrozen => aiFrozen;
        public bool HitboxesVisible => hitboxesVisible;
        public bool RaycastsVisible => raycastsVisible;
        public bool EnemyOutlineVisible => enemyOutlineVisible;
        public bool SyncLogsEnabled => syncLogsEnabled;

        private void Awake()
        {
            if (persistAcrossScenes)
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject);
                    return;
                }

                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance == null)
            {
                Instance = this;
            }

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<DebugMenuSettings>();
                settings.hideFlags = HideFlags.DontSave;
            }

            if (auditLogger == null)
            {
                auditLogger = GetComponent<DebugAuditLogger>();
            }

            if (auditLogger == null)
            {
                auditLogger = gameObject.AddComponent<DebugAuditLogger>();
            }

            if (playerService == null)
            {
                playerService = GetComponent<PlayerDebugServiceBase>();
            }

            if (weaponService == null)
            {
                weaponService = GetComponent<WeaponDebugServiceBase>();
            }

            if (enemyService == null)
            {
                enemyService = GetComponent<EnemyDebugServiceBase>();
            }

            if (visualService == null)
            {
                visualService = GetComponent<VisualDebugServiceBase>();
            }

            if (networkService == null)
            {
                networkService = GetComponent<NetworkSimulationServiceBase>();
            }

            if (authorizationBridge == null)
            {
                authorizationBridge = GetComponent<DebugAuthorizationBridge>();
            }

            if (authorizationBridge == null)
            {
                authorizationBridge = gameObject.AddComponent<DebugAuthorizationBridge>();
            }

            commandProcessor = new DebugConsoleCommandProcessor(this);
            ResetRuntimeState();
        }

        private void OnEnable()
        {
            if (authorizationBridge != null)
            {
                authorizationBridge.AuthorizationChanged += HandleAuthorizationChanged;
                authorizationBridge.RefreshState();
            }
            else
            {
                authorizationState = new DebugAuthorizationState(DebugBuildGate.IsBuildSupported, false, false, false, false, false, string.Empty);
                NotifyStateChanged();
            }
        }

        private void OnDisable()
        {
            if (authorizationBridge != null)
            {
                authorizationBridge.AuthorizationChanged -= HandleAuthorizationChanged;
            }
        }

        public bool TryToggleMenu()
        {
            if (!CanShowMenu)
            {
                LogDenied("menu", "toggle", authorizationState.StatusMessage);
                return false;
            }

            menuVisible = !menuVisible;
            LogInfo("menu", "visibility", menuVisible ? "shown" : "hidden");
            NotifyStateChanged();
            return true;
        }

        public void HideMenu()
        {
            if (!menuVisible)
            {
                return;
            }

            menuVisible = false;
            NotifyStateChanged();
        }

        public bool TryUnlockAccessCode(string rawCode)
        {
            rawCode = rawCode?.Trim();

            if (!authorizationState.IsAuthorized)
            {
                LogDenied("auth", "access_code", authorizationState.StatusMessage);
                return false;
            }

            if (!settings.RequiresSecretCode)
            {
                accessCodeValidated = true;
                NotifyStateChanged();
                return true;
            }

            if (!settings.ValidateAccessCode(rawCode))
            {
                LogDenied("auth", "access_code", "Invalid access code.");
                return false;
            }

            accessCodeValidated = true;
            LogInfo("auth", "access_code", "Access code accepted.");
            NotifyStateChanged();
            return true;
        }

        public bool SetSafeMode(bool enabled)
        {
            if (!enabled && !authorizationState.IsAuthorized)
            {
                LogDenied("system", "safe_mode", authorizationState.StatusMessage);
                return false;
            }

            if (!enabled && settings.RequiresSecretCode && !accessCodeValidated)
            {
                LogDenied("system", "safe_mode", "Access code required.");
                return false;
            }

            safeModeEnabled = enabled;

            if (enabled)
            {
                ResetAllServiceState();
            }

            LogInfo("system", "safe_mode", enabled ? "enabled" : "disabled");
            NotifyStateChanged();
            return true;
        }

        public bool SetGodMode(bool enabled)
        {
            return ExecuteFeatureAction(
                "player",
                "god_mode",
                enabled ? "on" : "off",
                () => playerService == null || !playerService.SupportsGodMode ? "Player debug service does not support God Mode." : null,
                () =>
                {
                    playerService.SetGodMode(enabled);
                    godModeEnabled = enabled;
                });
        }

        public bool SetMovementSpeedMultiplier(float multiplier)
        {
            var clamped = settings.ClampMovementSpeed(multiplier);
            return ExecuteFeatureAction(
                "player",
                "movement_speed",
                clamped.ToString("0.00"),
                () => playerService == null || !playerService.SupportsMovementSpeedOverride ? "Player debug service does not support speed override." : null,
                () =>
                {
                    playerService.SetMovementSpeedMultiplier(clamped);
                    movementSpeedMultiplier = clamped;
                });
        }

        public bool HealPlayer()
        {
            return ExecuteFeatureAction(
                "player",
                "heal",
                "full",
                () => playerService == null || !playerService.SupportsHeal ? "Player debug service does not support healing." : null,
                () => playerService.HealToFull());
        }

        public bool SetInfiniteAmmo(bool enabled)
        {
            return ExecuteFeatureAction(
                "weapon",
                "infinite_ammo",
                enabled ? "on" : "off",
                () => weaponService == null || !weaponService.SupportsInfiniteAmmo ? "Weapon debug service does not support infinite ammo." : null,
                () =>
                {
                    weaponService.SetInfiniteAmmo(enabled);
                    infiniteAmmoEnabled = enabled;
                });
        }

        public bool SetInstantReload(bool enabled)
        {
            return ExecuteFeatureAction(
                "weapon",
                "instant_reload",
                enabled ? "on" : "off",
                () => weaponService == null || !weaponService.SupportsInstantReload ? "Weapon debug service does not support instant reload." : null,
                () =>
                {
                    weaponService.SetInstantReload(enabled);
                    instantReloadEnabled = enabled;
                });
        }

        public bool TrySpawnWeapon(string weaponId, out string message)
        {
            weaponId = weaponId?.Trim();
            message = "Unknown error.";

            if (!CanRunAction(out var denialReason))
            {
                message = denialReason;
                LogDenied("weapon", "spawn", denialReason);
                return false;
            }

            if (weaponService == null || !weaponService.SupportsWeaponSpawn)
            {
                message = "Weapon debug service does not support weapon spawning.";
                LogDenied("weapon", "spawn", message);
                return false;
            }

            try
            {
                var success = weaponService.TrySpawnWeapon(weaponId, out message);
                Log(success ? DebugAuditSeverity.Info : DebugAuditSeverity.Warning, "weapon", "spawn", message);
                NotifyStateChanged();
                return success;
            }
            catch (Exception exception)
            {
                EnterSafeMode($"Weapon spawn failed: {exception.Message}");
                message = "Weapon spawn failed. SAFE MODE enabled.";
                return false;
            }
        }

        public bool TrySpawnEnemies(int count, out string message)
        {
            message = "Unknown error.";

            if (!CanRunAction(out var denialReason))
            {
                message = denialReason;
                LogDenied("enemy", "spawn", denialReason);
                return false;
            }

            if (enemyService == null || !enemyService.SupportsSpawn)
            {
                message = "Enemy debug service does not support enemy spawning.";
                LogDenied("enemy", "spawn", message);
                return false;
            }

            var clampedCount = settings.ClampEnemySpawnCount(count);

            try
            {
                var success = enemyService.TrySpawnEnemies(clampedCount, out message);
                Log(success ? DebugAuditSeverity.Info : DebugAuditSeverity.Warning, "enemy", "spawn", message);
                NotifyStateChanged();
                return success;
            }
            catch (Exception exception)
            {
                EnterSafeMode($"Enemy spawn failed: {exception.Message}");
                message = "Enemy spawn failed. SAFE MODE enabled.";
                return false;
            }
        }

        public bool SetAiFrozen(bool enabled)
        {
            return ExecuteFeatureAction(
                "enemy",
                "freeze",
                enabled ? "on" : "off",
                () => enemyService == null || !enemyService.SupportsFreeze ? "Enemy debug service does not support freeze toggle." : null,
                () =>
                {
                    enemyService.SetFrozen(enabled);
                    aiFrozen = enabled;
                });
        }

        public bool SetDifficulty(float multiplier)
        {
            var clamped = settings.ClampDifficulty(multiplier);
            return ExecuteFeatureAction(
                "enemy",
                "difficulty",
                clamped.ToString("0.00"),
                () => enemyService == null || !enemyService.SupportsDifficulty ? "Enemy debug service does not support difficulty override." : null,
                () =>
                {
                    enemyService.SetDifficulty(clamped);
                    difficultyMultiplier = clamped;
                });
        }

        public bool SetHitboxesVisible(bool visible)
        {
            return ExecuteFeatureAction(
                "visual",
                "hitboxes",
                visible ? "on" : "off",
                () => visualService == null || !visualService.SupportsHitboxes ? "Visual debug service does not support hitbox rendering." : null,
                () =>
                {
                    visualService.SetHitboxesVisible(visible);
                    hitboxesVisible = visible;
                });
        }

        public bool SetRaycastsVisible(bool visible)
        {
            return ExecuteFeatureAction(
                "visual",
                "raycasts",
                visible ? "on" : "off",
                () => visualService == null || !visualService.SupportsRaycasts ? "Visual debug service does not support raycast rendering." : null,
                () =>
                {
                    visualService.SetRaycastsVisible(visible);
                    raycastsVisible = visible;
                });
        }

        public bool SetEnemyOutlineVisible(bool visible)
        {
            return ExecuteFeatureAction(
                "visual",
                "enemy_outline",
                visible ? "on" : "off",
                () => visualService == null || !visualService.SupportsEnemyOutline ? "Visual debug service does not support enemy outlines." : null,
                () =>
                {
                    visualService.SetEnemyOutlineVisible(visible);
                    enemyOutlineVisible = visible;
                });
        }

        public bool SetLatencyMs(int value)
        {
            var clamped = settings.ClampLatencyMs(value);
            return ExecuteFeatureAction(
                "network",
                "latency_ms",
                clamped.ToString(),
                () => networkService == null || !networkService.SupportsLatencySimulation ? "Network debug service does not support latency simulation." : null,
                () =>
                {
                    networkService.SetLatencyMs(clamped);
                    latencyMs = clamped;
                });
        }

        public bool SetPacketLossPercent(int value)
        {
            var clamped = settings.ClampPacketLossPercent(value);
            return ExecuteFeatureAction(
                "network",
                "packet_loss",
                clamped.ToString(),
                () => networkService == null || !networkService.SupportsPacketLossSimulation ? "Network debug service does not support packet loss simulation." : null,
                () =>
                {
                    networkService.SetPacketLossPercent(clamped);
                    packetLossPercent = clamped;
                });
        }

        public bool SetSyncDebugLogs(bool enabled)
        {
            return ExecuteFeatureAction(
                "network",
                "sync_logs",
                enabled ? "on" : "off",
                () => networkService == null || !networkService.SupportsSyncLogs ? "Network debug service does not support sync logs." : null,
                () =>
                {
                    networkService.SetSyncDebugLogs(enabled);
                    syncLogsEnabled = enabled;
                });
        }

        public string ExecuteConsoleCommand(string rawCommand)
        {
            LogInfo("console", "input", rawCommand ?? string.Empty);
            var result = commandProcessor.Execute(rawCommand);
            Log(result.Severity, "console", "result", result.Message);
            ConsoleResponseLogged?.Invoke(result.Message);
            NotifyStateChanged();
            return result.Message;
        }

        public string GetMenuStatusLine()
        {
            if (!authorizationState.IsAuthorized)
            {
                return authorizationState.StatusMessage;
            }

            if (safeModeEnabled)
            {
                return "SAFE MODE active. Actions blocked until manually disabled.";
            }

            if (settings.RequiresSecretCode && !accessCodeValidated)
            {
                return "Authorized. Secret code required.";
            }

            return authorizationState.StatusMessage;
        }

        private void HandleAuthorizationChanged(DebugAuthorizationState state)
        {
            authorizationState = state;

            if (!state.IsAuthorized)
            {
                DisableDebugSession(state.StatusMessage);
            }

            NotifyStateChanged();
        }

        private bool ExecuteFeatureAction(string category, string action, string payload, Func<string> featureGuard, Action execute)
        {
            if (!CanRunAction(out var denialReason))
            {
                LogDenied(category, action, denialReason);
                return false;
            }

            var featureDenial = featureGuard?.Invoke();
            if (!string.IsNullOrWhiteSpace(featureDenial))
            {
                LogDenied(category, action, featureDenial);
                return false;
            }

            try
            {
                execute.Invoke();
                LogInfo(category, action, payload);
                NotifyStateChanged();
                return true;
            }
            catch (Exception exception)
            {
                EnterSafeMode($"{category}.{action} failed: {exception.Message}");
                return false;
            }
        }

        private bool CanRunAction(out string reason)
        {
            if (!DebugBuildGate.IsBuildSupported)
            {
                reason = "Internal debug tools are disabled in non-development builds.";
                return false;
            }

            if (!authorizationState.IsAuthorized)
            {
                reason = authorizationState.StatusMessage;
                return false;
            }

            if (settings.RequiresSecretCode && !accessCodeValidated)
            {
                reason = "Secret access code required.";
                return false;
            }

            if (safeModeEnabled)
            {
                reason = "SAFE MODE is enabled.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private void DisableDebugSession(string reason)
        {
            menuVisible = false;
            accessCodeValidated = !settings.RequiresSecretCode;
            ResetAllServiceState();
            Log(DebugAuditSeverity.Warning, "system", "session_disabled", reason);
        }

        private void EnterSafeMode(string reason)
        {
            safeModeEnabled = true;
            DisableDebugSession(reason);
            Log(DebugAuditSeverity.Error, "system", "safe_mode_fallback", reason);
        }

        private void ResetAllServiceState()
        {
            if (playerService != null)
            {
                playerService.ResetDebugState();
            }

            if (weaponService != null)
            {
                weaponService.ResetDebugState();
            }

            if (enemyService != null)
            {
                enemyService.ResetDebugState();
            }

            if (visualService != null)
            {
                visualService.ResetDebugState();
            }

            if (networkService != null)
            {
                networkService.ResetDebugState();
            }

            ResetFeatureState();
        }

        private void ResetRuntimeState()
        {
            menuVisible = false;
            accessCodeValidated = !settings.RequiresSecretCode;
            ResetFeatureState();
        }

        private void ResetFeatureState()
        {
            godModeEnabled = false;
            infiniteAmmoEnabled = false;
            instantReloadEnabled = false;
            aiFrozen = false;
            hitboxesVisible = false;
            raycastsVisible = false;
            enemyOutlineVisible = false;
            syncLogsEnabled = false;
            movementSpeedMultiplier = settings.DefaultMovementSpeedMultiplier;
            difficultyMultiplier = settings.DefaultDifficulty;
            latencyMs = 0;
            packetLossPercent = 0;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }

        private void LogInfo(string category, string action, string payload)
        {
            Log(DebugAuditSeverity.Info, category, action, payload);
        }

        private void LogDenied(string category, string action, string reason)
        {
            Log(DebugAuditSeverity.Warning, category, action, reason);
        }

        private void Log(DebugAuditSeverity severity, string category, string action, string payload)
        {
            if (auditLogger != null)
            {
                auditLogger.Log(severity, category, action, payload);
            }
        }
    }
}
