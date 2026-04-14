using System.Collections.Generic;
using UnityEngine;

namespace InternalDebugMenu
{
    /// <summary>
    /// Implement these adapters against your internal gameplay systems.
    /// They are the only place where debug requests should touch live game state.
    /// </summary>
    public abstract class PlayerDebugServiceBase : MonoBehaviour
    {
        public virtual bool SupportsGodMode => true;
        public virtual bool SupportsMovementSpeedOverride => true;
        public virtual bool SupportsHeal => true;

        public abstract void SetGodMode(bool enabled);
        public abstract void SetMovementSpeedMultiplier(float multiplier);
        public abstract void HealToFull();

        public virtual void ResetDebugState()
        {
            SetGodMode(false);
            SetMovementSpeedMultiplier(1.0f);
        }
    }

    public abstract class WeaponDebugServiceBase : MonoBehaviour
    {
        public virtual bool SupportsInfiniteAmmo => true;
        public virtual bool SupportsInstantReload => true;
        public virtual bool SupportsWeaponSpawn => true;
        public virtual IReadOnlyList<string> AvailableWeaponIds => System.Array.Empty<string>();

        public abstract void SetInfiniteAmmo(bool enabled);
        public abstract void SetInstantReload(bool enabled);
        public abstract bool TrySpawnWeapon(string weaponId, out string message);

        public virtual void ResetDebugState()
        {
            SetInfiniteAmmo(false);
            SetInstantReload(false);
        }
    }

    public abstract class EnemyDebugServiceBase : MonoBehaviour
    {
        public virtual bool SupportsSpawn => true;
        public virtual bool SupportsFreeze => true;
        public virtual bool SupportsDifficulty => true;

        public abstract bool TrySpawnEnemies(int count, out string message);
        public abstract void SetFrozen(bool enabled);
        public abstract void SetDifficulty(float difficultyMultiplier);

        public virtual void ResetDebugState()
        {
            SetFrozen(false);
            SetDifficulty(1.0f);
        }
    }

    public abstract class VisualDebugServiceBase : MonoBehaviour
    {
        public virtual bool SupportsHitboxes => true;
        public virtual bool SupportsRaycasts => true;
        public virtual bool SupportsEnemyOutline => true;

        public abstract void SetHitboxesVisible(bool visible);
        public abstract void SetRaycastsVisible(bool visible);
        public abstract void SetEnemyOutlineVisible(bool visible);

        public virtual void ResetDebugState()
        {
            SetHitboxesVisible(false);
            SetRaycastsVisible(false);
            SetEnemyOutlineVisible(false);
        }
    }

    public abstract class NetworkSimulationServiceBase : MonoBehaviour
    {
        public virtual bool SupportsLatencySimulation => true;
        public virtual bool SupportsPacketLossSimulation => true;
        public virtual bool SupportsSyncLogs => true;

        public abstract void SetLatencyMs(int latencyMs);
        public abstract void SetPacketLossPercent(int packetLossPercent);
        public abstract void SetSyncDebugLogs(bool enabled);

        public virtual void ResetDebugState()
        {
            SetLatencyMs(0);
            SetPacketLossPercent(0);
            SetSyncDebugLogs(false);
        }
    }
}
