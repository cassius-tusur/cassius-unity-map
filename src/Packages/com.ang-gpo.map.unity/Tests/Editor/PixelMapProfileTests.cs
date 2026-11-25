using System;
using System.Linq;
using Map.Core.Generation;
using Map.Unity;
using NUnit.Framework;

namespace Map.Unity.EditorTests
{
    public sealed class PixelMapProfileTests
    {
        private static readonly Guid MapId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");

        [Test]
        public void CreateDefault_UsesLegacyPixelDimensionsRulesAndDeterministicSettings()
        {
            var profile = PixelMapProfile.CreateDefault();

            Assert.That(profile.Width, Is.EqualTo(207));
            Assert.That(profile.Height, Is.EqualTo(200));

            var options = profile.CreateOptions(MapId);
            Assert.That(options.Dimensions.Width, Is.EqualTo(207));
            Assert.That(options.Dimensions.Height, Is.EqualTo(200));
            Assert.That(options.Seed, Is.EqualTo(9));
            Assert.That(options.Biomes.Select(biome => biome.Value), Is.EqualTo(new[]
            {
                "Water", "Deep Water", "Sand", "Grassland", "Forest", "Jungle",
                "MountainBase", "MountainMid", "MountainHigh", "MountainPeak"
            }));

            Assert.That(options.BiomeRules.Select(rule =>
                (rule.Biome.Value, rule.MaximumTerrainHeight, rule.MaximumBiomeNoise)), Is.EqualTo(new[]
            {
                ("Water", 0.40f, 0.50f),
                ("Deep Water", 0.40f, 1.00f),
                ("Sand", 0.44f, 1.00f),
                ("Grassland", 0.60f, 0.50f),
                ("Forest", 0.60f, 1.00f),
                ("Forest", 0.69f, 0.50f),
                ("Jungle", 0.69f, 1.00f),
                ("MountainBase", 0.80f, 1.00f),
                ("MountainMid", 0.85f, 1.00f),
                ("MountainHigh", 0.90f, 1.00f),
                ("MountainPeak", 1.00f, 1.00f)
            }));

            Assert.That(options.TerrainNoise, Is.EqualTo(new NoiseFieldOptions(25f, 5, 0.472f, 2f, 47.4f, -482.23f)));
            Assert.That(options.BiomeNoise, Is.EqualTo(new NoiseFieldOptions(50f, 2, 0.5f, 2f, 5861.087f, 8971.25f)));
            Assert.That(options.RoadPolicy.ConnectFeatures, Is.True);
            Assert.That(options.RoadPolicy.PreferredMaximumTerrainHeight, Is.EqualTo(0.79f));
            Assert.That(options.RoadPolicy.TerrainHeightCost, Is.EqualTo(2f));
        }

        [Test]
        public void CreateOptions_PreservesNonSquareDimensionsAndLegacyRules()
        {
            var profile = PixelMapProfile.CreateDefault();
            profile.Width = 13;
            profile.Height = 7;

            var options = profile.CreateOptions(MapId);

            Assert.That(options.Dimensions.Width, Is.EqualTo(13));
            Assert.That(options.Dimensions.Height, Is.EqualTo(7));
            Assert.That(options.BiomeRules.Count, Is.EqualTo(11));
            Assert.That(options.BiomeRules[4].Biome.Value, Is.EqualTo("Forest"));
            Assert.That(options.BiomeRules[5].Biome.Value, Is.EqualTo("Forest"));
            Assert.That(options.BiomeRules[6].Biome.Value, Is.EqualTo("Jungle"));
        }

        [Test]
        public void CreateOptions_RejectsZeroWidthOrHeightBeforeReturningOptions()
        {
            var profile = PixelMapProfile.CreateDefault();
            profile.Width = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.CreateOptions(MapId));

            profile.Width = 13;
            profile.Height = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.CreateOptions(MapId));
        }
    }
}
