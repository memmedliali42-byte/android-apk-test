using System;
using UnityEngine;

namespace InternalDebugMenu
{
    /// <summary>
    /// Receives trusted session state from the game's auth/session layer.
    /// Only call ApplySecureServerAuthorization after validating a secure backend response.
    /// </summary>
    public sealed class DebugAuthorizationBridge : MonoBehaviour
    {
        [SerializeField] private bool emulateOfflineInEditor = true;

        private string accountId = string.Empty;
        private bool offlineMode;
        private bool serverSnapshotReceived;
        private bool developerFlag;
        private bool secureDeveloperFlag;
        private bool publicMultiplayerSession;

        public event Action<DebugAuthorizationState> AuthorizationChanged;

        public DebugAuthorizationState CurrentState { get; private set; }

        private void OnEnable()
        {
            RefreshState();
        }

        public void UpdateSessionState(bool isOfflineMode, bool isPublicMultiplayerSession)
        {
            offlineMode = isOfflineMode;
            publicMultiplayerSession = isPublicMultiplayerSession;
            RefreshState();
        }

        public void ApplySecureServerAuthorization(string authorizedAccountId, bool isDeveloper, bool secureFlag)
        {
            accountId = authorizedAccountId ?? string.Empty;
            developerFlag = isDeveloper;
            secureDeveloperFlag = secureFlag;
            serverSnapshotReceived = true;
            RefreshState();
        }

        public void ClearServerAuthorization()
        {
            accountId = string.Empty;
            developerFlag = false;
            secureDeveloperFlag = false;
            serverSnapshotReceived = false;
            RefreshState();
        }

        public void RefreshState()
        {
            var effectiveOfflineMode = offlineMode || (Application.isEditor && emulateOfflineInEditor);
            CurrentState = new DebugAuthorizationState(
                DebugBuildGate.IsBuildSupported,
                effectiveOfflineMode,
                serverSnapshotReceived,
                developerFlag,
                secureDeveloperFlag,
                publicMultiplayerSession,
                accountId);

            AuthorizationChanged?.Invoke(CurrentState);
        }
    }
}
