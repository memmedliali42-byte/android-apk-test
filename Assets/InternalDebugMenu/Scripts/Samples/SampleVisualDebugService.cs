using UnityEngine;

namespace InternalDebugMenu
{
    public sealed class SampleVisualDebugService : VisualDebugServiceBase
    {
        public override void SetHitboxesVisible(bool visible)
        {
            Debug.Log($"SampleVisualDebugService: Hitboxes {(visible ? "visible" : "hidden")}");
        }

        public override void SetRaycastsVisible(bool visible)
        {
            Debug.Log($"SampleVisualDebugService: Raycasts {(visible ? "visible" : "hidden")}");
        }

        public override void SetEnemyOutlineVisible(bool visible)
        {
            Debug.Log($"SampleVisualDebugService: Enemy outline {(visible ? "enabled" : "disabled")}");
        }
    }
}
