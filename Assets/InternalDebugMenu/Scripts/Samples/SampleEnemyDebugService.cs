using UnityEngine;

namespace InternalDebugMenu
{
    public sealed class SampleEnemyDebugService : EnemyDebugServiceBase
    {
        private bool frozen;
        private float difficulty = 1.0f;

        public override bool TrySpawnEnemies(int count, out string message)
        {
            message = $"Spawned {count} sample enemies. Frozen={frozen}, Difficulty={difficulty:0.00}.";
            Debug.Log($"SampleEnemyDebugService: {message}");
            return true;
        }

        public override void SetFrozen(bool enabled)
        {
            frozen = enabled;
            Debug.Log($"SampleEnemyDebugService: Freeze {(enabled ? "enabled" : "disabled")}");
        }

        public override void SetDifficulty(float difficultyMultiplier)
        {
            difficulty = difficultyMultiplier;
            Debug.Log($"SampleEnemyDebugService: Difficulty set to {difficulty:0.00}");
        }
    }
}
