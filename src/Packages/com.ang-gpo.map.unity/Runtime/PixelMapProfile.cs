using System;
using System.Collections.Generic;
using Map.Core.Generation;
using Map.Core.Model;
using UnityEngine;
using UnityEngine.Serialization;

namespace Map.Unity
{
    /// <summary>
    /// Inspector-editable generation settings for the legacy-style pixel map.
    /// </summary>
    [Serializable]
    public sealed class PixelMapProfile
    {
        private const string AlgorithmVersion = MapAlgorithmVersions.V1;

        private static readonly string[] BiomeNames =
        {
            "Water", "Deep Water", "Sand", "Grassland", "Forest", "Jungle",
            "MountainBase", "MountainMid", "MountainHigh", "MountainPeak"
        };

        [SerializeField, Min(1)] private int width = 207;
        [SerializeField, Min(1)] private int height = 200;
        [SerializeField] private int seed = 9;

        [Header("Terrain Noise")]
        [SerializeField, Min(0.0001f)] private float terrainNoiseScale = 25f;
        [SerializeField, Min(1)] private int terrainNoiseOctaves = 5;
        [SerializeField, Range(0.0001f, 1f)] private float terrainNoisePersistence = 0.472f;
        [SerializeField, Min(0.0001f)] private float terrainNoiseLacunarity = 2f;
        [SerializeField] private float terrainNoiseOffsetX = 47.4f;
        [SerializeField] private float terrainNoiseOffsetY = -482.23f;

        [Header("Biome Noise")]
        [SerializeField, Min(0.0001f)] private float biomeNoiseScale = 50f;
        [SerializeField, Min(1)] private int biomeNoiseOctaves = 2;
        [SerializeField, Range(0.0001f, 1f)] private float biomeNoisePersistence = 0.5f;
        [SerializeField, Min(0.0001f)] private float biomeNoiseLacunarity = 2f;
        [SerializeField] private float biomeNoiseOffsetX = 5861.087f;
        [SerializeField] private float biomeNoiseOffsetY = 8971.25f;

        [Header("Road Policy")]
        [SerializeField] private bool roadConnectFeatures = true;
        [FormerlySerializedAs("roadMaximumTerrainHeight")]
        [SerializeField, Range(0f, 1f)] private float roadPreferredMaximumTerrainHeight = 0.79f;
        [SerializeField, Min(0f)] private float roadTerrainHeightCost = 2f;

        public int Width { get => width; set => width = value; }
        public int Height { get => height; set => height = value; }
        public int Seed { get => seed; set => seed = value; }

        public float TerrainNoiseScale { get => terrainNoiseScale; set => terrainNoiseScale = value; }
        public int TerrainNoiseOctaves { get => terrainNoiseOctaves; set => terrainNoiseOctaves = value; }
        public float TerrainNoisePersistence { get => terrainNoisePersistence; set => terrainNoisePersistence = value; }
        public float TerrainNoiseLacunarity { get => terrainNoiseLacunarity; set => terrainNoiseLacunarity = value; }
        public float TerrainNoiseOffsetX { get => terrainNoiseOffsetX; set => terrainNoiseOffsetX = value; }
        public float TerrainNoiseOffsetY { get => terrainNoiseOffsetY; set => terrainNoiseOffsetY = value; }

        public float BiomeNoiseScale { get => biomeNoiseScale; set => biomeNoiseScale = value; }
        public int BiomeNoiseOctaves { get => biomeNoiseOctaves; set => biomeNoiseOctaves = value; }
        public float BiomeNoisePersistence { get => biomeNoisePersistence; set => biomeNoisePersistence = value; }
        public float BiomeNoiseLacunarity { get => biomeNoiseLacunarity; set => biomeNoiseLacunarity = value; }
        public float BiomeNoiseOffsetX { get => biomeNoiseOffsetX; set => biomeNoiseOffsetX = value; }
        public float BiomeNoiseOffsetY { get => biomeNoiseOffsetY; set => biomeNoiseOffsetY = value; }

        public bool RoadConnectFeatures { get => roadConnectFeatures; set => roadConnectFeatures = value; }
        public float RoadPreferredMaximumTerrainHeight
        {
            get => roadPreferredMaximumTerrainHeight;
            set => roadPreferredMaximumTerrainHeight = value;
        }

        public float RoadTerrainHeightCost
        {
            get => roadTerrainHeightCost;
            set => roadTerrainHeightCost = value;
        }

        /// <summary>
        /// Creates an instance populated with the default configuration.
        /// </summary>
        /// <returns>The resulting instance populated with the default configuration.</returns>
        public static PixelMapProfile CreateDefault() => new PixelMapProfile();

        /// <summary>
        /// Creates options.
        /// </summary>
        /// <param name="mapId">The identifier of the map.</param>
        /// <returns>The options.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is outside the supported range.</exception>
        public MapGenerationOptions CreateOptions(Guid mapId)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");

            var biomes = CreateBiomes();
            return new MapGenerationOptions(
                mapId,
                new MapDimensions(width, height),
                seed,
                biomes,
                AlgorithmVersion,
                new NoiseFieldOptions(terrainNoiseScale, terrainNoiseOctaves, terrainNoisePersistence,
                    terrainNoiseLacunarity, terrainNoiseOffsetX, terrainNoiseOffsetY),
                new NoiseFieldOptions(biomeNoiseScale, biomeNoiseOctaves, biomeNoisePersistence,
                    biomeNoiseLacunarity, biomeNoiseOffsetX, biomeNoiseOffsetY),
                CreateBiomeRules(biomes),
                new RoadGenerationPolicy(roadConnectFeatures, roadPreferredMaximumTerrainHeight, roadTerrainHeightCost));
        }

        /// <summary>
        /// Creates biomes.
        /// </summary>
        /// <returns>The biomes.</returns>
        private static BiomeId[] CreateBiomes()
        {
            var biomes = new BiomeId[BiomeNames.Length];
            for (var index = 0; index < BiomeNames.Length; index++)
                biomes[index] = new BiomeId(BiomeNames[index]);
            return biomes;
        }

        /// <summary>
        /// Creates biome rules.
        /// </summary>
        /// <param name="biomes">The available biome identifiers.</param>
        /// <returns>The biome rules.</returns>
        private static BiomeRule[] CreateBiomeRules(IReadOnlyList<BiomeId> biomes) => new[]
        {
            new BiomeRule(biomes[0], 0.40f, 0.50f),
            new BiomeRule(biomes[1], 0.40f, 1.00f),
            new BiomeRule(biomes[2], 0.44f, 1.00f),
            new BiomeRule(biomes[3], 0.60f, 0.50f),
            new BiomeRule(biomes[4], 0.60f, 1.00f),
            new BiomeRule(biomes[4], 0.69f, 0.50f),
            new BiomeRule(biomes[5], 0.69f, 1.00f),
            new BiomeRule(biomes[6], 0.80f, 1.00f),
            new BiomeRule(biomes[7], 0.85f, 1.00f),
            new BiomeRule(biomes[8], 0.90f, 1.00f),
            new BiomeRule(biomes[9], 1.00f, 1.00f)
        };

    }
}
