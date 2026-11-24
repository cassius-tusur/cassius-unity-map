using System;
using System.Linq;
using Map.Core.Model;
using NUnit.Framework;
using UnityEngine;

namespace Map.Unity.EditorTests
{
    public sealed class MapSnapshotRendererVisualTests
    {
        [Test]
        public void Render_DrawsTheStoredPathBetweenLocations()
        {
            var host = new GameObject("MapSnapshotRenderer.VisualTestHost");
            try
            {
                var renderer = host.AddComponent<MapSnapshotRenderer>();
                renderer.Render(CreateLocationRoadSnapshot());

                var line = renderer.GeneratedRoot.GetComponentsInChildren<LineRenderer>().Single();
                var positions = new Vector3[line.positionCount];
                line.GetPositions(positions);

                Assert.That(line.positionCount, Is.EqualTo(3));
                Assert.That(positions, Is.EqualTo(new[]
                {
                    new Vector3(0f, 0.05f, 0f),
                    new Vector3(1f, 0.05f, 0f),
                    new Vector3(2f, 0.05f, 1f)
                }));
                Assert.That(line.sharedMaterial, Is.Not.Null);
                AssertRoadColor(line.sharedMaterial.color);
                AssertRoadColor(line.startColor);
                AssertRoadColor(line.endColor);
                Assert.That(line.startWidth, Is.EqualTo(0.4f).Within(0.0001f));
                Assert.That(line.endWidth, Is.EqualTo(0.4f).Within(0.0001f));
                Assert.That(renderer.GeneratedRoot.transform.Cast<Transform>().Count(child => child.name.StartsWith("Location_")), Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Render_ScalesLocationMarkersLabelsAndRoadsForMapsBeyondTheReferenceSize()
        {
            var baselineHost = new GameObject("MapSnapshotRenderer.VisualScaleBaselineHost");
            var oversizedHost = new GameObject("MapSnapshotRenderer.VisualScaleOversizedHost");
            try
            {
                var baselineRenderer = baselineHost.AddComponent<MapSnapshotRenderer>();
                var oversizedRenderer = oversizedHost.AddComponent<MapSnapshotRenderer>();
                var baselineSnapshot = CreateWideLocationRoadSnapshot(64);
                var oversizedSnapshot = CreateWideLocationRoadSnapshot(128);
                baselineRenderer.Render(baselineSnapshot);
                oversizedRenderer.Render(oversizedSnapshot);

                var baselineMarker = FindMarker(baselineRenderer, baselineSnapshot.Placements[0].ElementId);
                var oversizedMarker = FindMarker(oversizedRenderer, oversizedSnapshot.Placements[0].ElementId);
                var baselineLabel = baselineMarker.GetComponentInChildren<TextMesh>();
                var oversizedLabel = oversizedMarker.GetComponentInChildren<TextMesh>();
                var baselineRoad = baselineRenderer.GeneratedRoot.GetComponentInChildren<LineRenderer>();
                var oversizedRoad = oversizedRenderer.GeneratedRoot.GetComponentInChildren<LineRenderer>();

                Assert.That(baselineMarker.localScale.x, Is.GreaterThan(0f));
                Assert.That(baselineLabel.transform.lossyScale.x, Is.GreaterThan(0f));
                Assert.That(baselineRoad.startWidth, Is.GreaterThan(0f));
                Assert.That(oversizedMarker.localScale.x,
                    Is.EqualTo(baselineMarker.localScale.x * 2f).Within(0.0001f));
                Assert.That(oversizedMarker.localScale.y,
                    Is.EqualTo(baselineMarker.localScale.y * 2f).Within(0.0001f));
                Assert.That(oversizedLabel.transform.lossyScale.x,
                    Is.EqualTo(baselineLabel.transform.lossyScale.x * 2f).Within(0.0001f));
                Assert.That(oversizedRoad.startWidth,
                    Is.EqualTo(baselineRoad.startWidth * 2f).Within(0.0001f));
                Assert.That(oversizedRoad.endWidth,
                    Is.EqualTo(baselineRoad.endWidth * 2f).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baselineHost);
                UnityEngine.Object.DestroyImmediate(oversizedHost);
            }
        }

        private static MapSnapshot CreateLocationRoadSnapshot()
        {
            var firstId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            var secondId = Guid.Parse("55555555-5555-5555-5555-555555555555");
            return new MapSnapshot(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                new MapDimensions(3, 2),
                1,
                "v1",
                new[] { new BiomeId("grass") },
                new[]
                {
                    new MapCell(new GridPosition(0, 0), 0f, new BiomeId("grass")),
                    new MapCell(new GridPosition(1, 0), 0f, new BiomeId("grass")),
                    new MapCell(new GridPosition(2, 0), 0f, new BiomeId("grass")),
                    new MapCell(new GridPosition(0, 1), 0f, new BiomeId("grass")),
                    new MapCell(new GridPosition(1, 1), 0f, new BiomeId("grass")),
                    new MapCell(new GridPosition(2, 1), 0f, new BiomeId("grass"))
                },
                new[]
                {
                    new LocationPlacement(firstId, new GridPosition(0, 0), new BiomeId("grass"), "First"),
                    new LocationPlacement(secondId, new GridPosition(2, 1), new BiomeId("grass"))
                },
                new[]
                {
                    new Road(Guid.Parse("66666666-6666-6666-6666-666666666666"), firstId, secondId,
                        new[] { new GridPosition(0, 0), new GridPosition(1, 0), new GridPosition(2, 1) })
                });
        }

        private static Transform FindMarker(MapSnapshotRenderer renderer, Guid locationId) => renderer.GeneratedRoot.transform
            .Find("Location_" + locationId.ToString("D"));

        private static void AssertRoadColor(Color actual)
        {
            Assert.That(actual.r, Is.EqualTo(0.78f).Within(1f / 255f));
            Assert.That(actual.g, Is.EqualTo(0.08f).Within(1f / 255f));
            Assert.That(actual.b, Is.EqualTo(0.08f).Within(1f / 255f));
            Assert.That(actual.a, Is.EqualTo(1f).Within(1f / 255f));
        }

        private static MapSnapshot CreateWideLocationRoadSnapshot(int width)
        {
            var firstId = Guid.Parse("77777777-7777-7777-7777-777777777777");
            var secondId = Guid.Parse("88888888-8888-8888-8888-888888888888");
            const int height = 32;
            var cells = new MapCell[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                cells[y * width + x] = new MapCell(new GridPosition(x, y), 0f, new BiomeId("grass"));

            return new MapSnapshot(
                Guid.Parse("99999999-9999-9999-9999-999999999999"),
                new MapDimensions(width, height),
                1,
                "v1",
                new[] { new BiomeId("grass") },
                cells,
                new[]
                {
                    new LocationPlacement(firstId, new GridPosition(0, 0), new BiomeId("grass"), "Large map location"),
                    new LocationPlacement(secondId, new GridPosition(width - 1, 0), new BiomeId("grass"))
                },
                new[]
                {
                    new Road(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), firstId, secondId,
                        CreateHorizontalPath(width))
                });
        }

        private static GridPosition[] CreateHorizontalPath(int width)
        {
            var path = new GridPosition[width];
            for (var x = 0; x < width; x++) path[x] = new GridPosition(x, 0);
            return path;
        }
    }
}
