using System;
using System.Linq;
using Map.Core.Model;
using NUnit.Framework;
using UnityEngine;

namespace Map.Unity.EditorTests
{
    public sealed class MapSnapshotRendererTests
    {
        [Test]
        public void Render_BuildsOneCellTextureAndRendersConfiguredLocations()
        {
            var host = new GameObject("MapSnapshotRenderer.TestHost");
            var locationPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var renderer = host.AddComponent<MapSnapshotRenderer>();
                renderer.TilePalette = CreateTilePalette();
                renderer.LocationPalette = CreateLocationPalette(locationPrefab);

                renderer.Render(CreateCompleteSnapshot());

                Assert.That(renderer.RenderedTexture.width, Is.EqualTo(2));
                Assert.That(renderer.RenderedTexture.height, Is.EqualTo(2));
                Assert.That((Color32)renderer.RenderedTexture.GetPixel(0, 0), Is.EqualTo(new Color32(26, 179, 51, 255)));
                Assert.That((Color32)renderer.RenderedTexture.GetPixel(1, 0), Is.EqualTo(new Color32(26, 51, 204, 255)));
                Assert.That((Color32)renderer.RenderedTexture.GetPixel(0, 1), Is.EqualTo(new Color32(26, 51, 204, 255)));
                Assert.That((Color32)renderer.RenderedTexture.GetPixel(1, 1), Is.EqualTo(new Color32(26, 179, 51, 255)));
                Assert.That(renderer.RenderedTexture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(renderer.RenderedTexture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));

                Assert.That(renderer.GeneratedRoot.transform.Cast<Transform>().Count(child => child.name.StartsWith("Display_")), Is.EqualTo(1));
                var display = renderer.GeneratedRoot.transform.Find("Display_Cells").gameObject;
                Assert.That(display.activeSelf, Is.True);
                Assert.That(display.GetComponent<Renderer>().sharedMaterial.mainTexture, Is.SameAs(renderer.RenderedTexture));
                var displayMesh = display.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(Vector3.Dot(display.transform.TransformDirection(displayMesh.normals[0]), Vector3.up),
                    Is.EqualTo(1f).Within(0.0001f), "The terrain Quad must face the overhead camera.");
                Assert.That(renderer.GeneratedRoot.transform.Cast<Transform>().Count(child => child.name.StartsWith("Location_")), Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(locationPrefab);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Render_WhenLocationPaletteIsEmpty_UsesFlatQuadFallbackMarker()
        {
            var host = new GameObject("MapSnapshotRenderer.TestHost");
            try
            {
                var renderer = host.AddComponent<MapSnapshotRenderer>();

                renderer.Render(CreateCompleteSnapshot());

                var location = renderer.GeneratedRoot.transform.Find("Location_33333333-3333-3333-3333-333333333333");
                AssertFlatQuadMarker(location);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Render_ReplacesTheEntireGeneratedRootOnRerender()
        {
            var host = new GameObject("MapSnapshotRenderer.TestHost");
            try
            {
                var renderer = host.AddComponent<MapSnapshotRenderer>();

                renderer.Render(CreateSimpleSnapshot());
                var previousRoot = renderer.GeneratedRoot;
                var previousTexture = renderer.RenderedTexture;
                var previousMaterial = previousRoot.GetComponentInChildren<Renderer>().sharedMaterial;
                renderer.Render(CreateSimpleSnapshot());

                Assert.That(previousRoot == null, Is.True);
                Assert.That(previousTexture == null, Is.True);
                Assert.That(previousMaterial == null, Is.True);
                Assert.That(host.transform.childCount, Is.EqualTo(1));
                Assert.That(renderer.GeneratedRoot, Is.Not.Null);
                Assert.That(renderer.RenderedTexture, Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Render_WhenValidationFails_PreservesPreviouslyRenderedResources()
        {
            var host = new GameObject("MapSnapshotRenderer.TestHost");
            try
            {
                var renderer = host.AddComponent<MapSnapshotRenderer>();
                renderer.Render(CreateSimpleSnapshot());
                var previousRoot = renderer.GeneratedRoot;
                var previousTexture = renderer.RenderedTexture;
                var previousMaterial = previousRoot.GetComponentInChildren<Renderer>().sharedMaterial;
                renderer.CellSize = 0f;

                Assert.Throws<ArgumentOutOfRangeException>(() => renderer.Render(CreateSimpleSnapshot()));

                Assert.That(renderer.GeneratedRoot, Is.SameAs(previousRoot));
                Assert.That(renderer.RenderedTexture, Is.SameAs(previousTexture));
                Assert.That(previousRoot == null, Is.False);
                Assert.That(previousTexture == null, Is.False);
                Assert.That(previousMaterial == null, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Render_WhenDisplayBuildFailsAfterResourceAllocation_RetiresFreshResourcesAndPreservesPreviousRender()
        {
            var host = new GameObject("MapSnapshotRenderer.TestHost");
            var renderer = host.AddComponent<MapSnapshotRenderer>();
            try
            {
                renderer.Render(CreateSimpleSnapshot());
                var previousRoot = renderer.GeneratedRoot;
                var previousTexture = renderer.RenderedTexture;
                var previousMaterial = previousRoot.GetComponentInChildren<Renderer>().sharedMaterial;
                GameObject freshRoot = null;
                Texture2D freshTexture = null;
                Material freshMaterial = null;

                renderer.AfterDisplayResourcesCreated = new Action(() =>
                {
                    var freshDisplay = host.transform.Cast<Transform>()
                        .Single(child => child.gameObject != previousRoot)
                        .GetComponentInChildren<Renderer>();
                    freshRoot = freshDisplay.transform.parent.gameObject;
                    freshMaterial = freshDisplay.sharedMaterial;
                    freshTexture = freshMaterial.mainTexture as Texture2D;
                    throw new InvalidOperationException("Controlled post-display render failure.");
                });

                Assert.Throws<InvalidOperationException>(() => renderer.Render(CreateSimpleSnapshot()));

                Assert.That(freshRoot == null, Is.True);
                Assert.That(freshTexture == null, Is.True);
                Assert.That(freshMaterial == null, Is.True);
                Assert.That(renderer.GeneratedRoot, Is.SameAs(previousRoot));
                Assert.That(renderer.RenderedTexture, Is.SameAs(previousTexture));
                Assert.That(previousRoot == null, Is.False);
                Assert.That(previousTexture == null, Is.False);
                Assert.That(previousMaterial == null, Is.False);
            }
            finally
            {
                renderer.AfterDisplayResourcesCreated = null;
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static BiomeTilePalette CreateTilePalette()
        {
            var palette = ScriptableObject.CreateInstance<BiomeTilePalette>();
            palette.Entries = new[]
            {
                new BiomeTilePalette.Entry { BiomeId = "grass", Color = new Color(0.1f, 0.7f, 0.2f, 1f) },
                new BiomeTilePalette.Entry { BiomeId = "coast", Color = new Color(0.1f, 0.2f, 0.8f, 1f) }
            };
            return palette;
        }

        private static LocationPrefabPalette CreateLocationPalette(GameObject prefab)
        {
            var palette = ScriptableObject.CreateInstance<LocationPrefabPalette>();
            palette.Entries = new[] { new LocationPrefabPalette.Entry { BiomeId = "grass", Prefab = prefab } };
            return palette;
        }

        private static void AssertFlatQuadMarker(Transform marker)
        {
            Assert.That(marker, Is.Not.Null);
            var mesh = marker.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.name, Is.EqualTo("Quad"));
            Assert.That(Vector3.Dot(marker.TransformDirection(mesh.normals[0]), Vector3.up),
                Is.EqualTo(1f).Within(0.0001f));
        }

        private static MapSnapshot CreateSimpleSnapshot() => new MapSnapshot(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            new MapDimensions(1, 1),
            1,
            "v1",
            new[] { new BiomeId("grass") },
            Array.Empty<LocationPlacement>(),
            Array.Empty<Road>());

        private static MapSnapshot CreateCompleteSnapshot() => new MapSnapshot(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            new MapDimensions(2, 2),
            1,
            "v1",
            new[] { new BiomeId("grass"), new BiomeId("coast") },
            new[]
            {
                new MapCell(new GridPosition(0, 0), 0.2f, new BiomeId("grass")),
                new MapCell(new GridPosition(1, 0), 0.3f, new BiomeId("coast")),
                new MapCell(new GridPosition(0, 1), 0.4f, new BiomeId("coast")),
                new MapCell(new GridPosition(1, 1), 0.5f, new BiomeId("grass"))
            },
            new[]
            {
                new LocationPlacement(Guid.Parse("33333333-3333-3333-3333-333333333333"), new GridPosition(1, 1),
                    new BiomeId("grass"))
            },
            Array.Empty<Road>());
    }
}
