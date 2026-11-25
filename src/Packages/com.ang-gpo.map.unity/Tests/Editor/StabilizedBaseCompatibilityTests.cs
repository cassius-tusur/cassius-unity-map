using System;
using System.Linq;
using BaseClasses.Enum;
using BaseClasses.Model;
using BaseClasses.Services;
using BaseClasses.Services.Binds;
using Map.Core.Model;
using Map.Core.Params;
using NUnit.Framework;

namespace Map.Unity.EditorTests
{
    public class StabilizedBaseCompatibilityTests
    {
        [Test]
        public void MessagePackRoundTrip_PreservesParentRelations()
        {
            var plot = new Plot();
            var parent = new Element(ElemType.Character, "parent");
            var child = new Element(ElemType.Character, "child");
            plot.Add(parent); plot.Add(child);
            plot.Bind(parent, child, BaseRelationKeys.Parent, true);
            var serializer = new PlotMessagePackSerializer();
            var restored = serializer.Deserialize(serializer.Serialize(plot));
            Assert.That(restored.Elements.Count, Is.EqualTo(2));
            Assert.That(restored.Relations.Count, Is.EqualTo(2));
        }

        [Test]
        public void MessagePackRoundTrip_PreservesMapParameters()
        {
            var plot = new Plot();
            var location = new Element(ElemType.Location, "location");
            plot.Add(location);
            var keys = MapParams.For(Guid.NewGuid());
            location.Params.Add(keys.Position, new GridPosition(2, 3));
            location.Params.Add(keys.Biome, new BiomeId("Forest"));
            var serializer = new PlotMessagePackSerializer();
            var restored = serializer.Deserialize(serializer.Serialize(plot));
            var element = restored.Elements.Single();
            Assert.That(element.Params[keys.Position], Is.EqualTo(new GridPosition(2, 3)));
            Assert.That(element.Params[keys.Biome], Is.EqualTo(new BiomeId("Forest")));
        }
    }
}
