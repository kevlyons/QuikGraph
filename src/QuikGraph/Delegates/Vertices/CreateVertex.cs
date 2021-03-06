#if SUPPORTS_SERIALIZATION
using System;
#endif
using JetBrains.Annotations;

namespace QuikGraph
{
    /// <summary>
    /// Delegate to create a vertex in a graph.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TEdge">Edge type.</typeparam>
    /// <param name="graph">Graph in with adding the vertex.</param>
    /// <returns>The created vertex.</returns>
#if SUPPORTS_SERIALIZATION
    [Serializable]
#endif
    public delegate TVertex CreateVertexDelegate<TVertex, TEdge>([NotNull] IVertexListGraph<TVertex, TEdge> graph)
        where TEdge : IEdge<TVertex>;
}
