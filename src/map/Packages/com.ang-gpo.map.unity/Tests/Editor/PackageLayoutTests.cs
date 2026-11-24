using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Map.Unity.EditorTests
{
    public sealed class PackageLayoutTests
    {
        private static string PackageRoot => Path.GetFullPath(Path.Combine("Packages", "com.ang-gpo.map.unity"));
        private static string RuntimeAsmdefPath => Path.Combine(PackageRoot, "Runtime", "Map.Unity.Runtime.asmdef");
        private static string PluginsRoot => Path.Combine(PackageRoot, "Runtime", "Plugins");

        [Test]
        public void RuntimeAssemblyReferencesMapCoreNotLegacyScripts()
        {
            var asmdef = File.ReadAllText(RuntimeAsmdefPath);
            StringAssert.Contains("\"overrideReferences\": true", asmdef);
            StringAssert.Contains("Map.Core", asmdef);
            StringAssert.DoesNotContain("Assembly-CSharp", asmdef);
            StringAssert.DoesNotContain("Assets/Scripts", asmdef);
            StringAssert.Contains("Map.Core.dll", asmdef);
            StringAssert.Contains("BaseClasses.dll", asmdef);
            StringAssert.Contains("MessagePack.dll", asmdef);
            StringAssert.Contains("MessagePack.Annotations.dll", asmdef);
            StringAssert.Contains("Microsoft.NET.StringTools.dll", asmdef);
            StringAssert.Contains("System.Collections.Immutable.dll", asmdef);
            StringAssert.Contains("Microsoft.Bcl.AsyncInterfaces.dll", asmdef);
            StringAssert.Contains("System.Buffers.dll", asmdef);
            StringAssert.Contains("System.Memory.dll", asmdef);
            StringAssert.Contains("System.Runtime.CompilerServices.Unsafe.dll", asmdef);
            StringAssert.Contains("System.Text.Encodings.Web.dll", asmdef);
            StringAssert.Contains("System.Text.Json.dll", asmdef);
            StringAssert.Contains("System.Threading.Tasks.Extensions.dll", asmdef);
        }

        [Test]
        public void PackageCarriesThePrecompiledCoreAndMessagePackRuntimeDependencies()
        {
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "Map.Core.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "BaseClasses.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "MessagePack.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "MessagePack.Annotations.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "Microsoft.NET.StringTools.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "Microsoft.NET.StringTools.dll.meta")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "System.Collections.Immutable.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "System.Collections.Immutable.dll.meta")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "Microsoft.Bcl.AsyncInterfaces.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "Microsoft.Bcl.AsyncInterfaces.dll.meta")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "System.Buffers.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "System.Buffers.dll.meta")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "System.Memory.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "System.Memory.dll.meta")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "System.Runtime.CompilerServices.Unsafe.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "System.Runtime.CompilerServices.Unsafe.dll.meta")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "System.Text.Encodings.Web.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "System.Text.Encodings.Web.dll.meta")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "System.Text.Json.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "System.Text.Json.dll.meta")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "System.Threading.Tasks.Extensions.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(PluginsRoot, "System.Threading.Tasks.Extensions.dll.meta")), Is.True);
        }

        [Test]
        public void PackageContainsRuntimeAndReadme()
        {
            Assert.That(File.Exists(RuntimeAsmdefPath), Is.True);
            Assert.That(File.Exists(Path.Combine(PackageRoot, "Runtime", "MapSnapshotRenderer.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(PackageRoot, "README.md")), Is.True);
        }

        [Test]
        public void RendererDoesNotInferGridBiomes()
        {
            var renderer = File.ReadAllText(Path.Combine(PackageRoot, "Runtime", "MapSnapshotRenderer.cs"));
            StringAssert.DoesNotContain("snapshot.Biomes[", renderer);
            StringAssert.Contains("snapshot.Cells[index].Biome", renderer);
            StringAssert.Contains("FindColor", renderer);
            StringAssert.Contains("placement.Biome", renderer);
            StringAssert.DoesNotContain("Random.", renderer);
            StringAssert.DoesNotContain("Noise.", renderer);
        }

        [Test]
        public void PixelDemoPalette_MapsEveryCanonicalBiomeToTheLegacyColor()
        {
            var palette = AssetDatabase.LoadAssetAtPath<BiomeTilePalette>(
                "Assets/MapGeneratorDemo/BiomeTilePalette.asset");

            Assert.That(palette, Is.Not.Null);
            var colors = palette.Entries.ToDictionary(entry => entry.BiomeId, entry => entry.Color);
            Assert.That(colors, Has.Count.EqualTo(10));
            Assert.That(colors["Water"], Is.EqualTo(new Color(0.1f, 0.1f, 0.8f, 1f)));
            Assert.That(colors["Deep Water"], Is.EqualTo(new Color(0f, 0f, 0.5f, 1f)));
            Assert.That(colors["Sand"], Is.EqualTo(new Color(0.93f, 0.79f, 0.69f, 1f)));
            Assert.That(colors["Grassland"], Is.EqualTo(new Color(0.5f, 0.8f, 0.2f, 1f)));
            Assert.That(colors["Forest"], Is.EqualTo(new Color(0.1f, 0.5f, 0.1f, 1f)));
            Assert.That(colors["Jungle"], Is.EqualTo(new Color(0f, 0.4f, 0f, 1f)));
            Assert.That(colors["MountainBase"], Is.EqualTo(new Color(0.5f, 0.5f, 0.5f, 1f)));
            Assert.That(colors["MountainMid"], Is.EqualTo(new Color(0.6f, 0.6f, 0.6f, 1f)));
            Assert.That(colors["MountainHigh"], Is.EqualTo(new Color(0.8f, 0.8f, 0.8f, 1f)));
            Assert.That(colors["MountainPeak"], Is.EqualTo(Color.white));
        }
    }
}
