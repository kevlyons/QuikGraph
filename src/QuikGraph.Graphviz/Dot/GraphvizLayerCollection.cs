using System;
using System.Collections.ObjectModel;
using System.IO;
using JetBrains.Annotations;

namespace QuikGraph.Graphviz.Dot
{
    /// <summary>
    /// Graphviz layer collection.
    /// </summary>
    public class GraphvizLayerCollection : Collection<GraphvizLayer>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphvizLayerCollection"/> class.
        /// </summary>
        public GraphvizLayerCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphvizLayerCollection"/> class.
        /// </summary>
        /// <param name="list">The list that is wrapped by the new collection.</param>
        public GraphvizLayerCollection([NotNull, ItemNotNull] GraphvizLayer[] list)
            : base(list)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphvizLayerCollection"/> class.
        /// </summary>
        /// <param name="list">The list that is wrapped by the new collection.</param>
        public GraphvizLayerCollection([NotNull, ItemNotNull] GraphvizLayerCollection list)
            : base(list)
        {
        }

        [NotNull]
        private string _separators = ":";

        /// <summary>
        /// Allowed collection item separators.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:layersep">See more</see>
        /// </summary>
        [NotNull]
        public string Separators
        {
            get => _separators;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException($"{nameof(Separators)} cannot be null or empty.", nameof(value));

                _separators = value;
            }
        }

        /// <summary>
        /// Converts this collection to DOT.
        /// </summary>
        /// <returns>Collection as DOT.</returns>
        [Pure]
        [NotNull]
        public string ToDot()
        {
            if (Count == 0)
                return string.Empty;

            using (var writer = new StringWriter())
            {
                writer.Write("layers=\"");
                bool flag = false;
                foreach (GraphvizLayer layer in this)
                {
                    if (flag)
                    {
                        writer.Write(Separators);
                    }
                    else
                    {
                        flag = true;
                    }
                    writer.Write(layer.Name);
                }
                writer.WriteLine("\";");
                writer.WriteLine($"layersep=\"{Separators}\"");
                
                return writer.ToString();
            }
        }
    }
}