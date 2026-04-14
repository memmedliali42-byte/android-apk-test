using System;
using System.Collections.Generic;
using UnityEngine;

namespace InternalDebugMenu
{
    [Serializable]
    public struct WeaponSpawnCatalogEntry
    {
        public string Id;
        public string DisplayName;
        public GameObject Prefab;
    }

    [CreateAssetMenu(fileName = "WeaponSpawnCatalog", menuName = "Internal Debug Menu/Weapon Spawn Catalog")]
    public sealed class WeaponSpawnCatalog : ScriptableObject
    {
        [SerializeField] private List<WeaponSpawnCatalogEntry> weapons = new List<WeaponSpawnCatalogEntry>();

        public IReadOnlyList<WeaponSpawnCatalogEntry> Weapons => weapons;

        public bool TryGet(string weaponId, out WeaponSpawnCatalogEntry entry)
        {
            for (var index = 0; index < weapons.Count; index++)
            {
                if (string.Equals(weapons[index].Id, weaponId, StringComparison.OrdinalIgnoreCase))
                {
                    entry = weapons[index];
                    return true;
                }
            }

            entry = default;
            return false;
        }
    }
}
