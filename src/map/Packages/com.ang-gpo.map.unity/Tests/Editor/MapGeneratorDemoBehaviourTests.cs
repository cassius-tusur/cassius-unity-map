using System;
using System.IO;
using System.Linq;
using Map.Core.Model;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Map.Unity.EditorTests
{
    public sealed class MapGeneratorDemoBehaviourTests
    {
        [Test]
        public void GenerateAndSaveSample_UsesConfiguredPixelProfileForSnapshotAndTextureDimensions()
        {
            var snapshotPath = "acceptance/" + Guid.NewGuid().ToString("N") + ".msgpack";
            var hostObject = new GameObject("MapGeneratorDemoBehaviour.Profile");
            try
            {
                var host = hostObject.AddComponent<MapGeneratorDemoBehaviour>();
                host.SnapshotPath = snapshotPath;
                host.PixelProfile = CreateProfile(13, 7);

                host.GenerateAndSaveSample();

                Assert.That(host.LastSnapshot.Dimensions.Width, Is.EqualTo(13));
                Assert.That(host.LastSnapshot.Dimensions.Height, Is.EqualTo(7));
                Assert.That(host.LastSnapshot.Cells.Count, Is.EqualTo(91));
                Assert.That(host.Renderer.RenderedTexture.width, Is.EqualTo(13));
                Assert.That(host.Renderer.RenderedTexture.height, Is.EqualTo(7));
            }
            finally
            {
                DeletePersistedSnapshot(snapshotPath);
                UnityEngine.Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void GenerateAndSaveSample_FramesTheMainCameraAroundTheConfiguredMap()
        {
            var snapshotPath = "acceptance/" + Guid.NewGuid().ToString("N") + ".msgpack";
            var hostObject = new GameObject("MapGeneratorDemoBehaviour.CameraProfile");
            var cameraObject = new GameObject("MapGeneratorDemoBehaviour.TestCamera");
            var previousMainCamera = Camera.main;
            var previousMainCameraEnabled = previousMainCamera != null && previousMainCamera.enabled;
            try
            {
                if (previousMainCamera != null) previousMainCamera.enabled = false;
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 1f;
                camera.transform.SetPositionAndRotation(new Vector3(100f, 12f, 100f), Quaternion.Euler(90f, 0f, 0f));

                var host = hostObject.AddComponent<MapGeneratorDemoBehaviour>();
                host.SnapshotPath = snapshotPath;
                host.PixelProfile = CreateProfile(13, 7);

                host.GenerateAndSaveSample();

                Assert.That(camera.transform.position.x, Is.EqualTo(6f).Within(0.0001f));
                Assert.That(camera.transform.position.z, Is.EqualTo(3f).Within(0.0001f));
                Assert.That(camera.orthographicSize, Is.GreaterThanOrEqualTo(3.5f));
                Assert.That(IsInsideViewport(camera.WorldToViewportPoint(new Vector3(-0.5f, 0f, -0.5f))), Is.True);
                Assert.That(IsInsideViewport(camera.WorldToViewportPoint(new Vector3(12.5f, 0f, 6.5f))), Is.True);
            }
            finally
            {
                if (previousMainCamera != null) previousMainCamera.enabled = previousMainCameraEnabled;
                DeletePersistedSnapshot(snapshotPath);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void LoadAndRender_UsesPersistedSnapshotDimensionsWhenConfiguredProfileDiffers()
        {
            var snapshotPath = "acceptance/" + Guid.NewGuid().ToString("N") + ".msgpack";
            var sourceObject = new GameObject("MapGeneratorDemoBehaviour.Source");
            var loadedObject = new GameObject("MapGeneratorDemoBehaviour.Loaded");
            try
            {
                var source = sourceObject.AddComponent<MapGeneratorDemoBehaviour>();
                source.SnapshotPath = snapshotPath;
                source.PixelProfile = CreateProfile(13, 7);
                source.GenerateAndSaveSample();

                var generated = source.LastSnapshot;
                Assert.That(generated, Is.Not.Null);
                Assert.That(File.Exists(source.ResolvedSnapshotPath), Is.True);
                Assert.That(source.Renderer.GeneratedRoot, Is.Not.Null);

                var loaded = loadedObject.AddComponent<MapGeneratorDemoBehaviour>();
                loaded.SnapshotPath = snapshotPath;
                loaded.PixelProfile = CreateProfile(4, 3);
                loaded.LoadAndRender();

                Assert.That(loaded.GenerationCount, Is.EqualTo(0),
                    "Loading a persisted snapshot must not generate a new map.");
                Assert.That(loaded.LastSnapshot.MapId, Is.EqualTo(generated.MapId));
                Assert.That(loaded.LastSnapshot.Dimensions.Width, Is.EqualTo(13));
                Assert.That(loaded.LastSnapshot.Dimensions.Height, Is.EqualTo(7));
                Assert.That(loaded.LastSnapshot.Cells.Count, Is.EqualTo(generated.Cells.Count));
                Assert.That(loaded.LastSnapshot.Placements.Count, Is.EqualTo(generated.Placements.Count));
                Assert.That(loaded.LastSnapshot.Roads.Count, Is.EqualTo(generated.Roads.Count));
                Assert.That(loaded.Renderer.GeneratedRoot, Is.Not.Null);
                Assert.That(loaded.Renderer.GeneratedRoot.GetComponentsInChildren<TextMesh>().Select(label => label.text),
                    Is.EquivalentTo(LabelsFor(generated)));
            }
            finally
            {
                DeletePersistedSnapshot(snapshotPath);
                UnityEngine.Object.DestroyImmediate(sourceObject);
                UnityEngine.Object.DestroyImmediate(loadedObject);
            }
        }

        [Test]
        public void DemoScene_GenerateAndLoadSmoke_RendersTheStoredPixelMapAtConfiguredDimensions()
        {
            const string scenePath = "Assets/Scenes/MapGeneratorDemo.unity";
            var snapshotPath = "acceptance/scene-smoke-" + Guid.NewGuid().ToString("N") + ".msgpack";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var source = UnityEngine.Object.FindObjectsByType<MapGeneratorDemoBehaviour>(FindObjectsSortMode.None).Single();
            GameObject loadedObject = null;
            try
            {
                Assert.That(source.PixelProfile.Width, Is.EqualTo(207));
                Assert.That(source.PixelProfile.Height, Is.EqualTo(200));
                source.SnapshotPath = snapshotPath;
                source.PixelProfile = CreateProfile(160, 120);

                source.GenerateAndSaveSample();

                var generated = source.LastSnapshot;
                var renderer = source.Renderer;
                Assert.That(generated.Dimensions.Width, Is.EqualTo(160));
                Assert.That(generated.Dimensions.Height, Is.EqualTo(120));
                Assert.That(renderer.RenderedTexture.width, Is.EqualTo(160));
                Assert.That(renderer.RenderedTexture.height, Is.EqualTo(120));
                Assert.That(renderer.RenderedTexture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(renderer.RenderedTexture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(renderer.RenderedTexture.GetPixels().Any(color => color == Color.magenta), Is.False);
                Assert.That(renderer.GeneratedRoot.transform.Find("Display_Cells"), Is.Not.Null);

                var generatedTransforms = renderer.GeneratedRoot.GetComponentsInChildren<Transform>();
                Assert.That(generatedTransforms.Count(transform => transform.name.StartsWith("Location_")),
                    Is.EqualTo(generated.Placements.Count));
                Assert.That(renderer.GeneratedRoot.GetComponentsInChildren<TextMesh>().Select(label => label.text),
                    Is.EquivalentTo(LabelsFor(generated)));
                var roads = renderer.GeneratedRoot.GetComponentsInChildren<LineRenderer>();
                Assert.That(roads, Is.Not.Empty);
                Assert.That(roads.All(road => road.sharedMaterial != null), Is.True);
                foreach (var road in roads) AssertRoadColor(road.sharedMaterial.color);

                loadedObject = new GameObject("MapGeneratorDemoBehaviour.SceneSmokeLoad");
                var loaded = loadedObject.AddComponent<MapGeneratorDemoBehaviour>();
                loaded.SnapshotPath = snapshotPath;
                loaded.PixelProfile = CreateProfile(4, 3);
                CopyRendererPalettes(renderer, loaded.Renderer);

                loaded.LoadAndRender();

                Assert.That(loaded.GenerationCount, Is.EqualTo(0));
                Assert.That(loaded.LastSnapshot.MapId, Is.EqualTo(generated.MapId));
                Assert.That(loaded.LastSnapshot.Dimensions, Is.EqualTo(generated.Dimensions));
                Assert.That(loaded.Renderer.RenderedTexture.width, Is.EqualTo(160));
                Assert.That(loaded.Renderer.RenderedTexture.height, Is.EqualTo(120));
                Assert.That(loaded.Renderer.RenderedTexture.GetPixels(), Is.EqualTo(renderer.RenderedTexture.GetPixels()));
                Assert.That(loaded.Renderer.GeneratedRoot.GetComponentsInChildren<TextMesh>().Select(label => label.text),
                    Is.EquivalentTo(LabelsFor(generated)));
            }
            finally
            {
                DeletePersistedSnapshot(snapshotPath);
                if (loadedObject != null) UnityEngine.Object.DestroyImmediate(loadedObject);
            }
        }

        private static PixelMapProfile CreateProfile(int width, int height)
        {
            var profile = PixelMapProfile.CreateDefault();
            profile.Width = width;
            profile.Height = height;
            return profile;
        }

        private static bool IsInsideViewport(Vector3 point) =>
            point.x >= 0f && point.x <= 1f && point.y >= 0f && point.y <= 1f && point.z > 0f;

        private static void CopyRendererPalettes(MapSnapshotRenderer source, MapSnapshotRenderer destination)
        {
            destination.TilePalette = source.TilePalette;
            destination.LocationPalette = source.LocationPalette;
        }

        private static string[] LabelsFor(MapSnapshot snapshot) => snapshot.Placements
            .Select(placement => placement.Label)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .ToArray();

        private static void AssertRoadColor(Color actual)
        {
            Assert.That(actual.r, Is.EqualTo(0.78f).Within(1f / 255f));
            Assert.That(actual.g, Is.EqualTo(0.08f).Within(1f / 255f));
            Assert.That(actual.b, Is.EqualTo(0.08f).Within(1f / 255f));
            Assert.That(actual.a, Is.EqualTo(1f).Within(1f / 255f));
        }

        private static void DeletePersistedSnapshot(string snapshotPath)
        {
            var persistedPath = Path.Combine(Application.persistentDataPath, snapshotPath);
            if (File.Exists(persistedPath)) File.Delete(persistedPath);
            var directory = Path.GetDirectoryName(persistedPath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory) &&
                Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                Directory.Delete(directory);
        }
    }
}
