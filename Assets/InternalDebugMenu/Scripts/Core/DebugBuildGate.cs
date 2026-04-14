using UnityEngine;

namespace InternalDebugMenu
{
    /// <summary>
    /// Hard gate that prevents internal QA tools from becoming active in production builds.
    /// </summary>
    public static class DebugBuildGate
    {
        public static bool IsBuildSupported => Application.isEditor || Debug.isDebugBuild;
    }
}
