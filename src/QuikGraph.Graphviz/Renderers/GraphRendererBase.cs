﻿using System;
using System.Drawing;
using JetBrains.Annotations;
using QuickGraph.Graphviz.Dot;
using QuikGraph;
using QuikGraph.Utils;

namespace QuickGraph.Graphviz
{
    /// <summary>
    /// Base class for Graph to DOT renderer.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TEdge">Edge type.</typeparam>
    public abstract class GraphRendererBase<TVertex, TEdge>
        where TEdge : IEdge<TVertex>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphvizAlgorithm{TVertex,TEdge}"/> class.
        /// </summary>
        /// <param name="graph">Graph to convert to DOT.</param>
        protected GraphRendererBase([NotNull] IEdgeListGraph<TVertex, TEdge> graph)
        {
            Graphviz = new GraphvizAlgorithm<TVertex, TEdge>(graph);
            InternalInitialize();
        }

        private void InternalInitialize()
        {
            Graphviz.CommonVertexFormat.Style = GraphvizVertexStyle.Filled;
            Graphviz.CommonVertexFormat.FillColor = Color.LightYellow;
            Graphviz.CommonVertexFormat.Font = new Font("Tahoma", 8.25F);
            Graphviz.CommonVertexFormat.Shape = GraphvizVertexShape.Box;

            Graphviz.CommonEdgeFormat.Font = new Font("Tahoma", 8.25F);
        }

        /// <summary>
        /// Initializes renderer for generation.
        /// </summary>
        protected virtual void Initialize()
        {
        }

        /// <summary>
        /// Cleans renderer after generation.
        /// </summary>
        protected virtual void Clean()
        {
        }

        /// <summary>
        /// Graph to DOT algorithm.
        /// </summary>
        [NotNull]
        public GraphvizAlgorithm<TVertex, TEdge> Graphviz { get; }

        /// <inheritdoc cref="GraphvizAlgorithm{TVertex,TEdge}.VisitedGraph"/>
        public IEdgeListGraph<TVertex, TEdge> VisitedGraph => Graphviz.VisitedGraph;

        /// <inheritdoc cref="GraphvizAlgorithm{TVertex,TEdge}.Generate(IDotEngine,string)"/>
        public string Generate([NotNull] IDotEngine dot, [NotNull] string outputFilePath)
        {
            using (GenerationScope())
            {
                return Graphviz.Generate(dot, outputFilePath);
            }

            #region Local function

            IDisposable GenerationScope()
            {
                Initialize();
                return DisposableHelpers.Finally(Clean);
            }

            #endregion
        }
    }
}