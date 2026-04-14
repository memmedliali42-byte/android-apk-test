using UnityEngine;

namespace InternalDebugMenu
{
    [CreateAssetMenu(fileName = "DebugMenuSettings", menuName = "Internal Debug Menu/Settings")]
    public sealed class DebugMenuSettings : ScriptableObject
    {
        [Header("Activation")]
        [SerializeField] private bool requireSecretCode = true;
        [SerializeField] private string accessCodeSha256 = string.Empty;
        [SerializeField] private string accessCodeHint = "Developer access code";
        [SerializeField] [Min(0.25f)] private float threeFingerHoldSeconds = 1.0f;
        [SerializeField] [Min(0.5f)] private float gestureCooldownSeconds = 1.25f;

        [Header("Player")]
        [SerializeField] private float minimumMovementSpeedMultiplier = 0.5f;
        [SerializeField] private float maximumMovementSpeedMultiplier = 3.0f;
        [SerializeField] private float defaultMovementSpeedMultiplier = 1.0f;

        [Header("AI")]
        [SerializeField] [Min(1)] private int maximumEnemySpawnCount = 12;
        [SerializeField] private float minimumDifficulty = 0.5f;
        [SerializeField] private float maximumDifficulty = 3.0f;
        [SerializeField] private float defaultDifficulty = 1.0f;

        [Header("Network Simulation")]
        [SerializeField] [Min(0)] private int minimumLatencyMs = 50;
        [SerializeField] [Min(0)] private int maximumLatencyMs = 300;
        [SerializeField] [Range(0, 100)] private int maximumPacketLossPercent = 30;

        public bool RequiresSecretCode => requireSecretCode && !string.IsNullOrWhiteSpace(accessCodeSha256);
        public string AccessCodeHint => accessCodeHint;
        public float ThreeFingerHoldSeconds => threeFingerHoldSeconds;
        public float GestureCooldownSeconds => gestureCooldownSeconds;
        public float MinimumMovementSpeedMultiplier => minimumMovementSpeedMultiplier;
        public float MaximumMovementSpeedMultiplier => maximumMovementSpeedMultiplier;
        public float DefaultMovementSpeedMultiplier => defaultMovementSpeedMultiplier;
        public int MaximumEnemySpawnCount => maximumEnemySpawnCount;
        public float MinimumDifficulty => minimumDifficulty;
        public float MaximumDifficulty => maximumDifficulty;
        public float DefaultDifficulty => defaultDifficulty;
        public int MinimumLatencyMs => minimumLatencyMs;
        public int MaximumLatencyMs => maximumLatencyMs;
        public int MaximumPacketLossPercent => maximumPacketLossPercent;

        public float ClampMovementSpeed(float value)
        {
            return Mathf.Clamp(value, minimumMovementSpeedMultiplier, maximumMovementSpeedMultiplier);
        }

        public int ClampEnemySpawnCount(int value)
        {
            return Mathf.Clamp(value, 1, maximumEnemySpawnCount);
        }

        public float ClampDifficulty(float value)
        {
            return Mathf.Clamp(value, minimumDifficulty, maximumDifficulty);
        }

        public int ClampLatencyMs(int value)
        {
            return Mathf.Clamp(value, minimumLatencyMs, maximumLatencyMs);
        }

        public int ClampPacketLossPercent(int value)
        {
            return Mathf.Clamp(value, 0, maximumPacketLossPercent);
        }

        public bool ValidateAccessCode(string rawCode)
        {
            if (!RequiresSecretCode)
            {
                return true;
            }

            var submittedHash = DebugCodeUtility.ComputeSha256(rawCode);
            return DebugCodeUtility.SecureEquals(submittedHash, accessCodeSha256);
        }

        private void OnValidate()
        {
            minimumMovementSpeedMultiplier = Mathf.Clamp(minimumMovementSpeedMultiplier, 0.1f, 10.0f);
            maximumMovementSpeedMultiplier = Mathf.Max(minimumMovementSpeedMultiplier, maximumMovementSpeedMultiplier);
            defaultMovementSpeedMultiplier = Mathf.Clamp(defaultMovementSpeedMultiplier, minimumMovementSpeedMultiplier, maximumMovementSpeedMultiplier);

            maximumEnemySpawnCount = Mathf.Max(1, maximumEnemySpawnCount);
            maximumDifficulty = Mathf.Max(minimumDifficulty, maximumDifficulty);
            defaultDifficulty = Mathf.Clamp(defaultDifficulty, minimumDifficulty, maximumDifficulty);

            minimumLatencyMs = Mathf.Max(0, minimumLatencyMs);
            maximumLatencyMs = Mathf.Max(minimumLatencyMs, maximumLatencyMs);
        }
    }
}
