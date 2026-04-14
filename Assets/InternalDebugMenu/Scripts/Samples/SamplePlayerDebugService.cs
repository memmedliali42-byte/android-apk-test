using UnityEngine;

namespace InternalDebugMenu
{
    public sealed class SamplePlayerDebugService : PlayerDebugServiceBase
    {
        private bool godModeEnabled;
        private float speedMultiplier = 1.0f;

        public override void SetGodMode(bool enabled)
        {
            godModeEnabled = enabled;
            Debug.Log($"SamplePlayerDebugService: God Mode {(enabled ? "enabled" : "disabled")}");
        }

        public override void SetMovementSpeedMultiplier(float multiplier)
        {
            speedMultiplier = multiplier;
            Debug.Log($"SamplePlayerDebugService: Speed multiplier set to {speedMultiplier:0.00}");
        }

        public override void HealToFull()
        {
            Debug.Log($"SamplePlayerDebugService: HealToFull invoked. GodMode={godModeEnabled}, Speed={speedMultiplier:0.00}");
        }
    }
}
