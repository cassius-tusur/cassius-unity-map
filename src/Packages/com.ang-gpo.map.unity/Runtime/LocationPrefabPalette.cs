using System;
using Map.Core.Model;
using UnityEngine;

namespace Map.Unity
{
    /// <summary>
    /// Maps biome identifiers to Unity prefabs used for rendered locations.
    /// </summary>
    [CreateAssetMenu(menuName = "ANG GPO/Map/Location Prefab Palette")]
    public sealed class LocationPrefabPalette : ScriptableObject
    {
        /// <summary>
        /// Associates one biome identifier with a location prefab.
        /// </summary>
        [Serializable]
        public sealed class Entry
        {
            public string BiomeId = string.Empty;
            public GameObject Prefab;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        [SerializeField] private GameObject fallbackPrefab;

        public Entry[] Entries { get => entries; set => entries = value ?? Array.Empty<Entry>(); }
        public GameObject FallbackPrefab { get => fallbackPrefab; set => fallbackPrefab = value; }

        /// <summary>
        /// Finds the prefab configured for the supplied biome.
        /// </summary>
        /// <param name="biome">The biome identifier.</param>
        /// <returns>The prefab configured for the supplied biome.</returns>
        public GameObject FindPrefab(BiomeId biome)
        {
            foreach (var entry in entries)
                if (entry != null && string.Equals(entry.BiomeId, biome.Value, StringComparison.Ordinal))
                    return entry.Prefab;
            return fallbackPrefab;
        }
    }
}
