using System.Collections.Generic;
using UnityEngine;

namespace InternalDebugMenu
{
    public sealed class SampleWeaponDebugService : WeaponDebugServiceBase
    {
        private static readonly string[] WeaponIds = { "weapon_ak", "weapon_m4", "weapon_awp", "weapon_glock" };

        private bool infiniteAmmoEnabled;
        private bool instantReloadEnabled;

        public override IReadOnlyList<string> AvailableWeaponIds => WeaponIds;

        public override void SetInfiniteAmmo(bool enabled)
        {
            infiniteAmmoEnabled = enabled;
            Debug.Log($"SampleWeaponDebugService: Infinite ammo {(enabled ? "enabled" : "disabled")}");
        }

        public override void SetInstantReload(bool enabled)
        {
            instantReloadEnabled = enabled;
            Debug.Log($"SampleWeaponDebugService: Instant reload {(enabled ? "enabled" : "disabled")}");
        }

        public override bool TrySpawnWeapon(string weaponId, out string message)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                message = "Weapon id is empty.";
                return false;
            }

            message = $"Spawned sample weapon '{weaponId}'. Ammo={(infiniteAmmoEnabled ? "infinite" : "normal")}, Reload={(instantReloadEnabled ? "instant" : "default")}.";
            Debug.Log($"SampleWeaponDebugService: {message}");
            return true;
        }
    }
}
