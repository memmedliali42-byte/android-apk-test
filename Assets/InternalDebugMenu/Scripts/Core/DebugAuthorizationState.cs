namespace InternalDebugMenu
{
    public readonly struct DebugAuthorizationState
    {
        public DebugAuthorizationState(
            bool buildSupported,
            bool offlineMode,
            bool serverSnapshotReceived,
            bool developerFlag,
            bool secureDeveloperFlag,
            bool publicMultiplayerSession,
            string accountId)
        {
            BuildSupported = buildSupported;
            OfflineMode = offlineMode;
            ServerSnapshotReceived = serverSnapshotReceived;
            DeveloperFlag = developerFlag;
            SecureDeveloperFlag = secureDeveloperFlag;
            PublicMultiplayerSession = publicMultiplayerSession;
            AccountId = accountId ?? string.Empty;
        }

        public bool BuildSupported { get; }
        public bool OfflineMode { get; }
        public bool ServerSnapshotReceived { get; }
        public bool DeveloperFlag { get; }
        public bool SecureDeveloperFlag { get; }
        public bool PublicMultiplayerSession { get; }
        public string AccountId { get; }

        public bool HasSecureDeveloperAccess => ServerSnapshotReceived && DeveloperFlag && SecureDeveloperFlag;
        public bool IsAuthorized => BuildSupported && !PublicMultiplayerSession && (OfflineMode || HasSecureDeveloperAccess);

        public string StatusMessage
        {
            get
            {
                if (!BuildSupported)
                {
                    return "Disabled: non-development build.";
                }

                if (PublicMultiplayerSession)
                {
                    return "Locked: public multiplayer session.";
                }

                if (OfflineMode)
                {
                    return "Authorized: offline sandbox.";
                }

                if (HasSecureDeveloperAccess)
                {
                    return "Authorized: secure developer account.";
                }

                if (!ServerSnapshotReceived)
                {
                    return "Locked: waiting for secure server authorization.";
                }

                return "Locked: secure developer flag missing.";
            }
        }
    }
}
