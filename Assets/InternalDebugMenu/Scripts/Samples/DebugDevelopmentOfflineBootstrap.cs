using UnityEngine;

namespace InternalDebugMenu
{
    /// <summary>
    /// Convenience bootstrap for mobile smoke testing in editor/development builds.
    /// Never rely on this in production matchmaking flows.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DebugManager))]
    [RequireComponent(typeof(DebugAuthorizationBridge))]
    [RequireComponent(typeof(DebugMenuGestureActivator))]
    [RequireComponent(typeof(DebugMenuRuntimeUI))]
    public sealed class DebugDevelopmentOfflineBootstrap : MonoBehaviour
    {
        [SerializeField] private bool authorizeOfflineInDevelopmentBuilds = true;
        [SerializeField] private bool markAsNonPublicSession = true;

        private DebugAuthorizationBridge authorizationBridge;

        private void Awake()
        {
            authorizationBridge = GetComponent<DebugAuthorizationBridge>();
        }

        private void Start()
        {
            if (!authorizeOfflineInDevelopmentBuilds || authorizationBridge == null)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            authorizationBridge.UpdateSessionState(true, !markAsNonPublicSession);
#endif
        }
    }
}
