using System;
using Map.Core.Model;
using UnityEngine;

namespace Map.Unity
{
    /// <summary>
    /// Maps biome identifiers to Unity tile prefabs, materials, and colors.
    /// </summary>
    [CreateAssetMenu(menuName = "ANG GPO/Map/Biome Tile Palette")]
    public sealed class BiomeTilePalette : ScriptableObject
    {
        /// <summary>
        /// Associates one biome identifier with its Unity rendering resources.
        /// </summary>
        [Serializable]
        public sealed class Entry
        {
            public string BiomeId = string.Empty;
            public GameObject TilePrefab;
            public Material Material;
            public UnityEngine.Color Color = UnityEngine.Color.magenta;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        [SerializeField] private GameObject neutralTilePrefab;
        [SerializeField] private Material neutralMaterial;
        [SerializeField] private Color fallbackColor = Color.magenta;

        public Entry[] Entries { get => entries; set => entries = value ?? Array.Empty<Entry>(); }
        public GameObject NeutralTilePrefab { get => neutralTilePrefab; set => neutralTilePrefab = value; }
        public Material NeutralMaterial { get => neutralMaterial; set => neutralMaterial = value; }
        public Color FallbackColor { get => fallbackColor; set => fallbackColor = value; }

        /// <summary>
        /// Finds neutral prefab.
        /// </summary>
        /// <returns>The neutral prefab.</returns>
        public GameObject FindNeutralPrefab() => neutralTilePrefab;

        /// <summary>
        /// Finds neutral material.
        /// </summary>
        /// <returns>The neutral material.</returns>
        public Material FindNeutralMaterial() => neutralMaterial;

        /// <summary>
        /// Finds the prefab configured for the supplied biome.
        /// </summary>
        /// <param name="biome">The biome identifier.</param>
        /// <returns>The prefab configured for the supplied biome.</returns>
        public GameObject FindPrefab(BiomeId biome)
        {
            foreach (var entry in entries)
                if (entry != null && string.Equals(entry.BiomeId, biome.Value, StringComparison.Ordinal))
                    return entry.TilePrefab;
            return null;
        }

        /// <summary>
        /// Finds material.
        /// </summary>
        /// <param name="biome">The biome identifier.</param>
        /// <returns>The material.</returns>
        public Material FindMaterial(BiomeId biome)
        {
            foreach (var entry in entries)
                if (entry != null && string.Equals(entry.BiomeId, biome.Value, StringComparison.Ordinal))
                    return entry.Material != null ? entry.Material : neutralMaterial;
            return neutralMaterial;
        }

        /// <summary>
        /// Finds color.
        /// </summary>
        /// <param name="biome">The biome identifier.</param>
        /// <returns>The color.</returns>
        public Color FindColor(BiomeId biome)
        {
            foreach (var entry in entries)
                if (entry != null && string.Equals(entry.BiomeId, biome.Value, StringComparison.Ordinal))
                    return entry.Color;
            return fallbackColor;
        }
    }
}
