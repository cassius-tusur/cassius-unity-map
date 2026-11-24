using System;
using Map.Core.Model;
using Map.Core.Validation;
using UnityEngine;

namespace Map.Unity
{
    /// <summary>Renders an already validated snapshot. Generation and persistence belong to the host.</summary>
    /// <seealso cref="MapSnapshot"/>
    public sealed class MapSnapshotRenderer : MonoBehaviour
    {
        [SerializeField] private BiomeTilePalette tilePalette;
        [SerializeField] private LocationPrefabPalette locationPalette;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float featureHeight = 0.5f;
        [SerializeField] private float roadHeight = 0.05f;
        [SerializeField] private float roadWidth = 0.4f;
        [SerializeField] private float fallbackMarkerSize = 2.5f;
        [SerializeField, Min(1f)] private float visualScaleReferenceSize = 64f;

        private GameObject generatedRoot;
        private Texture2D renderedTexture;
        private Material renderedMaterial;
        private Material renderedRoadMaterial;

        public BiomeTilePalette TilePalette { get => tilePalette; set => tilePalette = value; }
        public LocationPrefabPalette LocationPalette { get => locationPalette; set => locationPalette = value; }
        public float CellSize { get => cellSize; set => cellSize = value; }
        public float FeatureHeight { get => featureHeight; set => featureHeight = value; }
        public float RoadHeight { get => roadHeight; set => roadHeight = value; }
        public GameObject GeneratedRoot => generatedRoot;
        public Texture2D RenderedTexture => renderedTexture;

        // Kept internal so EditMode tests can exercise rollback after display-resource allocation.
        internal Action AfterDisplayResourcesCreated { get; set; }

        /// <summary>
        /// Renders the supplied value.
        /// </summary>
        /// <param name="snapshot">The map snapshot to process.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is outside the supported range.</exception>
        public void Render(MapSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));
            MapSnapshotValidator.Validate(snapshot);

            // Build off to the side so a failed visual asset cannot destroy the currently visible map.
            var freshRoot = new GameObject("MapSnapshotRenderer.Generated");
            freshRoot.transform.SetParent(transform, false);
            var freshDisplay = new DisplayResources();
            try
            {
                RenderDisplay(snapshot, freshRoot.transform, freshDisplay);
                AfterDisplayResourcesCreated?.Invoke();
                var visualScale = CalculateVisualScale(snapshot.Dimensions);
                RenderLocations(snapshot, freshRoot.transform, visualScale);
                RenderRoads(snapshot, freshRoot.transform, freshDisplay, visualScale);
            }
            catch
            {
                RetireGeneratedResources(freshRoot, freshDisplay.Texture, freshDisplay.Material, freshDisplay.RoadMaterial);
                throw;
            }

            var previousRoot = generatedRoot;
            var previousTexture = renderedTexture;
            var previousMaterial = renderedMaterial;
            var previousRoadMaterial = renderedRoadMaterial;
            generatedRoot = freshRoot;
            renderedTexture = freshDisplay.Texture;
            renderedMaterial = freshDisplay.Material;
            renderedRoadMaterial = freshDisplay.RoadMaterial;
            RetireGeneratedResources(previousRoot, previousTexture, previousMaterial, previousRoadMaterial);
        }

        /// <summary>
        /// Releases generated Unity resources when the renderer is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            var previousRoot = generatedRoot;
            var previousTexture = renderedTexture;
            var previousMaterial = renderedMaterial;
            var previousRoadMaterial = renderedRoadMaterial;
            generatedRoot = null;
            renderedTexture = null;
            renderedMaterial = null;
            renderedRoadMaterial = null;
            RetireGeneratedResources(previousRoot, previousTexture, previousMaterial, previousRoadMaterial);
        }

        /// <summary>
        /// Renders display.
        /// </summary>
        /// <param name="snapshot">The map snapshot to process.</param>
        /// <param name="parent">The parent element.</param>
        /// <param name="displayResources">The display resources value.</param>
        private void RenderDisplay(MapSnapshot snapshot, Transform parent, DisplayResources displayResources)
        {
            displayResources.Texture = new Texture2D(snapshot.Dimensions.Width, snapshot.Dimensions.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "MapSnapshotRenderer.Cells"
            };
            var colors = new Color[snapshot.Cells.Count];
            for (var index = 0; index < snapshot.Cells.Count; index++)
                colors[index] = tilePalette == null ? Color.magenta : tilePalette.FindColor(snapshot.Cells[index].Biome);
            displayResources.Texture.SetPixels(colors);
            displayResources.Texture.Apply(false, false);

            var display = GameObject.CreatePrimitive(PrimitiveType.Quad);
            display.name = "Display_Cells";
            display.transform.SetParent(parent, false);
            display.transform.localPosition = new Vector3((snapshot.Dimensions.Width - 1) * cellSize * 0.5f, 0f,
                (snapshot.Dimensions.Height - 1) * cellSize * 0.5f);
            display.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            display.transform.localScale = new Vector3(snapshot.Dimensions.Width * cellSize,
                snapshot.Dimensions.Height * cellSize, 1f);

            var surface = display.GetComponent<Renderer>();
            displayResources.Material = new Material(FindTextureShader());
            displayResources.Material.mainTexture = displayResources.Texture;
            surface.sharedMaterial = displayResources.Material;
        }

        /// <summary>
        /// Renders locations.
        /// </summary>
        /// <param name="snapshot">The map snapshot to process.</param>
        /// <param name="parent">The parent element.</param>
        /// <param name="visualScale">The visual scale value.</param>
        private void RenderLocations(MapSnapshot snapshot, Transform parent, float visualScale)
        {
            foreach (var placement in snapshot.Placements)
            {
                var prefab = locationPalette == null ? null : locationPalette.FindPrefab(placement.Biome);
                var feature = prefab == null ? CreateFlatFallbackMarker() : Instantiate(prefab);
                feature.name = "Location_" + placement.ElementId.ToString("D");
                feature.transform.SetParent(parent, false);
                feature.transform.localScale *= visualScale;
                feature.transform.localPosition = GridToWorld(placement.Position) + Vector3.up * featureHeight;
                RenderLocationLabel(feature.transform, placement.ElementId, placement.Label);
            }
        }

        /// <summary>
        /// Renders location label.
        /// </summary>
        /// <param name="feature">The feature value.</param>
        /// <param name="locationId">The identifier of the location.</param>
        /// <param name="text">The text value.</param>
        private void RenderLocationLabel(Transform feature, Guid locationId, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var labelObject = new GameObject("Label_" + locationId.ToString("D"));
            labelObject.transform.SetParent(feature, false);
            labelObject.transform.localPosition = Vector3.up * featureHeight;
            var label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.black;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 32;
            label.characterSize = 0.12f;
            label.font.material.mainTexture.filterMode = FilterMode.Point;
        }

        /// <summary>
        /// Renders roads.
        /// </summary>
        /// <param name="snapshot">The map snapshot to process.</param>
        /// <param name="parent">The parent element.</param>
        /// <param name="displayResources">The display resources value.</param>
        /// <param name="visualScale">The visual scale value.</param>
        private void RenderRoads(MapSnapshot snapshot, Transform parent, DisplayResources displayResources, float visualScale)
        {
            if (snapshot.Roads.Count == 0) return;

            displayResources.RoadMaterial = new Material(FindColorShader()) { color = new Color(0.78f, 0.08f, 0.08f, 1f) };
            foreach (var road in snapshot.Roads)
            {
                var lineObject = new GameObject("Road_" + road.RoadId.ToString("D"));
                lineObject.transform.SetParent(parent, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = road.GridPath.Count;
                line.sharedMaterial = displayResources.RoadMaterial;
                line.startColor = displayResources.RoadMaterial.color;
                line.endColor = displayResources.RoadMaterial.color;
                line.startWidth = roadWidth * visualScale;
                line.endWidth = roadWidth * visualScale;
                for (var i = 0; i < road.GridPath.Count; i++)
                    line.SetPosition(i, GridToWorld(road.GridPath[i]) + Vector3.up * roadHeight);
            }
        }

        /// <summary>
        /// Converts a grid position to a Unity world-space position.
        /// </summary>
        /// <param name="position">The grid position to process.</param>
        /// <returns>The corresponding Unity world-space position.</returns>
        private Vector3 GridToWorld(GridPosition position) =>
            new Vector3(position.X * cellSize, 0f, position.Y * cellSize);

        /// <summary>
        /// Calculates the scale used to fit the map into the configured display area.
        /// </summary>
        /// <param name="dimensions">The map dimensions.</param>
        /// <returns>The computed value.</returns>
        private float CalculateVisualScale(MapDimensions dimensions)
        {
            var mapSize = Mathf.Max(dimensions.Width, dimensions.Height);
            return Mathf.Max(1f, mapSize / Mathf.Max(1f, visualScaleReferenceSize));
        }

        /// <summary>
        /// Creates a flat marker used when no location prefab is available.
        /// </summary>
        /// <returns>The generated fallback marker.</returns>
        private GameObject CreateFlatFallbackMarker()
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            marker.transform.localScale = new Vector3(fallbackMarkerSize, fallbackMarkerSize, 1f);
            return marker;
        }

        /// <summary>
        /// Finds texture shader.
        /// </summary>
        /// <returns>The texture shader.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the operation cannot be completed in the current state.</exception>
        private static Shader FindTextureShader()
        {
            var shader = Shader.Find("Unlit/Texture");
            if (shader == null) throw new InvalidOperationException("Unity could not find a texture display shader.");
            return shader;
        }

        /// <summary>
        /// Finds color shader.
        /// </summary>
        /// <returns>The color shader.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the operation cannot be completed in the current state.</exception>
        private static Shader FindColorShader()
        {
            var shader = Shader.Find("Unlit/Color");
            if (shader == null) throw new InvalidOperationException("Unity could not find a road display shader.");
            return shader;
        }

        /// <summary>
        /// Destroys a generated Unity object using the appropriate editor or runtime API.
        /// </summary>
        /// <param name="generatedObject">The generated object value.</param>
        private static void Retire(UnityEngine.Object generatedObject)
        {
            if (generatedObject == null) return;
            if (Application.isPlaying)
            {
                if (generatedObject is GameObject root) root.SetActive(false);
                Destroy(generatedObject);
            }
            else DestroyImmediate(generatedObject);
        }

        /// <summary>
        /// Releases all resources generated for the previous render.
        /// </summary>
        /// <param name="root">The root location for the operation.</param>
        /// <param name="texture">The texture value.</param>
        /// <param name="material">The material value.</param>
        /// <param name="roadMaterial">The road material value.</param>
        private static void RetireGeneratedResources(GameObject root, Texture2D texture, Material material, Material roadMaterial)
        {
            Retire(roadMaterial);
            Retire(material);
            Retire(root);
            Retire(texture);
        }

        /// <summary>
        /// Owns the Unity objects created while rendering a map snapshot.
        /// </summary>
        private sealed class DisplayResources
        {
            public Texture2D Texture;
            public Material Material;
            public Material RoadMaterial;
        }
    }
}
