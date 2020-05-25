using System;
using NUnit.Framework;
using QuikGraph.Graphviz.Dot;

namespace QuikGraph.Graphviz.Tests
{
    /// <summary>
    /// Tests related to <see cref="GraphvizLayerCollection"/>.
    /// </summary>
    [TestFixture]
    internal class GraphvizLayerCollectionTests
    {
        [Test]
        public void Constructor()
        {
            var layerCollection = new GraphvizLayerCollection();
            CollectionAssert.IsEmpty(layerCollection);
            Assert.AreEqual(":", layerCollection.Separators);

            var layer1 = new GraphvizLayer("L1");
            layerCollection.Add(layer1);
            CollectionAssert.AreEqual(new[] { layer1 }, layerCollection);
            
            var layer2 = new GraphvizLayer("L2");
            var layerArray = new[] { layer1, layer2 };
            layerCollection = new GraphvizLayerCollection(layerArray);
            CollectionAssert.AreEqual(layerArray, layerCollection);
            Assert.AreEqual(":", layerCollection.Separators);

            var layer3 = new GraphvizLayer("L3");
            layerCollection.Add(layer3);
            CollectionAssert.AreEqual(new[] { layer1, layer2, layer3 }, layerCollection);

            var otherLayerCollection = new GraphvizLayerCollection(layerCollection);
            CollectionAssert.AreEqual(layerCollection, otherLayerCollection);
            Assert.AreEqual(":", otherLayerCollection.Separators);

            var layer4 = new GraphvizLayer("L4");
            otherLayerCollection.Add(layer4);
            CollectionAssert.AreEqual(new[] { layer1, layer2, layer3, layer4 }, otherLayerCollection);

        }

        [Test]
        public void Constructor_Throws()
        {
            // ReSharper disable ObjectCreationAsStatement
            // ReSharper disable AssignNullToNotNullAttribute
            Assert.Throws<ArgumentException>(() => new GraphvizLayerCollection((GraphvizLayer[])null));
            Assert.Throws<ArgumentException>(() => new GraphvizLayerCollection((GraphvizLayerCollection)null));
            // ReSharper restore AssignNullToNotNullAttribute
            // ReSharper restore ObjectCreationAsStatement
        }

        [Test]
        public void Separators()
        {
            var layerCollection = new GraphvizLayerCollection();
            if (layerCollection.Separators != ":")
                throw new InvalidOperationException("Collection has wong separators.");

            layerCollection.Separators = ":,-";
            Assert.AreEqual(":,-", layerCollection.Separators);
        }

        [Test]
        public void Separators_Throws()
        {
            var layerCollection = new GraphvizLayerCollection();
            // ReSharper disable ObjectCreationAsStatement
            // ReSharper disable once AssignNullToNotNullAttribute
            Assert.Throws<ArgumentException>(() => layerCollection.Separators = null);
            Assert.Throws<ArgumentException>(() => layerCollection.Separators = "");
            // ReSharper restore ObjectCreationAsStatement
        }

        [Test]
        public void ToDot()
        {
            var layerCollection = new GraphvizLayerCollection();
            Assert.AreEqual(string.Empty, layerCollection.ToDot());

            layerCollection = new GraphvizLayerCollection(new[]
            {
                new GraphvizLayer("L0")
            });
            Assert.AreEqual(
                "layers=\"L0\";" + Environment.NewLine +
                "layersep=\":\"" + Environment.NewLine,
                layerCollection.ToDot());

            layerCollection = new GraphvizLayerCollection(new[]
            {
                new GraphvizLayer("L1"), 
                new GraphvizLayer("L2"), 
                new GraphvizLayer("L3") 
            });
            Assert.AreEqual(
                "layers=\"L1:L2:L3\";" + Environment.NewLine +
                "layersep=\":\"" + Environment.NewLine,
                layerCollection.ToDot());

            layerCollection.Separators = " ";
            Assert.AreEqual(
                "layers=\"L1 L2 L3\";" + Environment.NewLine +
                "layersep=\" \"" + Environment.NewLine,
                layerCollection.ToDot());

            layerCollection.Separators = " -/";
            Assert.AreEqual(
                "layers=\"L1 -/L2 -/L3\";" + Environment.NewLine +
                "layersep=\" -/\"" + Environment.NewLine,
                layerCollection.ToDot());
        }
    }
}