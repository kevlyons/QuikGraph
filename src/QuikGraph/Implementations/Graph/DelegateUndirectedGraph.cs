﻿#if SUPPORTS_SERIALIZATION
using System;
#endif
using System.Collections.Generic;
#if SUPPORTS_CONTRACTS
using System.Diagnostics.Contracts;
#endif
using System.Linq;

namespace QuikGraph
{
    /// <summary>
    /// A functional implicit undirected graph
    /// </summary>
    /// <typeparam name="TVertex">type of the vertices</typeparam>
    /// <typeparam name="TEdge">type of the edges</typeparam>
#if SUPPORTS_SERIALIZATION
    [Serializable]
#endif
    public class DelegateUndirectedGraph<TVertex, TEdge>
        : DelegateImplicitUndirectedGraph<TVertex, TEdge>
        , IUndirectedGraph<TVertex, TEdge>
        where TEdge : IEdge<TVertex>
    {
        readonly IEnumerable<TVertex> vertices;
        int _vertexCount = -1;
        int _edgeCount = -1;

        public DelegateUndirectedGraph(
             IEnumerable<TVertex> vertices,
             TryFunc<TVertex, IEnumerable<TEdge>> tryGetAdjacentEdges,
             bool allowParallelEdges)
            : base(tryGetAdjacentEdges, allowParallelEdges)
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(vertices != null);
            Contract.Requires(Enumerable.All(vertices, v =>
            {
                IEnumerable<TEdge> edges;
                return tryGetAdjacentEdges(v, out edges);
            }));
#endif

            this.vertices = vertices;
        }

        public bool IsVerticesEmpty
        {
            get
            {
                // shortcut
                if (this._vertexCount > -1)
                    return this._vertexCount == 0;
                // count
                foreach (var vertex in this.vertices)
                    return false;
                return true;
            }
        }

        public int VertexCount
        {
            get
            {
                if (this._vertexCount < 0)
                    this._vertexCount = Enumerable.Count(this.vertices);
                return this._vertexCount;
            }
        }

        public virtual IEnumerable<TVertex> Vertices
        {
            get { return this.vertices; }
        }

        public bool IsEdgesEmpty
        {
            get
            {
                if (this._vertexCount == 0 ||
                    this._edgeCount == 0)
                    return true; // no vertices or no edges.

                foreach (var vertex in this.vertices)
                    foreach (var edge in this.AdjacentEdges(vertex))
                        return false;
                return true;
            }
        }

        public int EdgeCount
        {
            get
            {
                if (this._edgeCount < 0)
                    this._edgeCount = Enumerable.Count(this.Edges);
                return this._edgeCount;
            }
        }

        public virtual IEnumerable<TEdge> Edges
        {
            get
            {
                foreach (var vertex in this.vertices)
                    foreach (var edge in this.AdjacentEdges(vertex))
                        if (edge.Source.Equals(vertex))
                            yield return edge;
            }
        }

        public bool ContainsEdge(TEdge edge)
        {
            IEnumerable<TEdge> edges;
            if (this.TryGetAdjacentEdges(edge.Source, out edges))
                foreach (var e in edges)
                    if (e.Equals(edge))
                        return true;
            return false;
        }
    }
}