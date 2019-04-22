﻿#if SUPPORTS_SERIALIZATION
using System;
#endif
using System.Collections.Generic;
#if SUPPORTS_CONTRACTS
using System.Diagnostics.Contracts;
#endif
using JetBrains.Annotations;

namespace QuikGraph.Predicates
{
    /// <summary>
    /// Predicate that tests if an edge's reverse is residual.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TEdge">Edge type.</typeparam>
#if SUPPORTS_SERIALIZATION
    [Serializable]
#endif
    public sealed class ReversedResidualEdgePredicate<TVertex, TEdge>
        where TEdge : IEdge<TVertex>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReversedResidualEdgePredicate{TVertex,TEdge}"/> class.
        /// </summary>
        /// <param name="residualCapacities">Residual capacities per edge.</param>
        /// <param name="reversedEdges">Map of edges and their reversed edges.</param>
        public ReversedResidualEdgePredicate(
            [NotNull] IDictionary<TEdge, double> residualCapacities,
            [NotNull] IDictionary<TEdge, TEdge> reversedEdges)
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(residualCapacities != null);
            Contract.Requires(reversedEdges != null);
#endif
            ResidualCapacities = residualCapacities;
            ReversedEdges = reversedEdges;
        }

        /// <summary>
        /// Residual capacities map.
        /// </summary>
        [NotNull]
        public IDictionary<TEdge, double> ResidualCapacities { get; }

        /// <summary>
        /// Reversed edges map.
        /// </summary>
        [NotNull]
        public IDictionary<TEdge, TEdge> ReversedEdges { get; }

        /// <summary>
        /// Checks if the given <paramref name="edge"/> reverse is residual.
        /// </summary>
        /// <remarks>Check if the implemented predicate is matched.</remarks>
        /// <param name="edge">Edge to check.</param>
        /// <returns>True if the reversed edge is residual, false otherwise.</returns>
#if SUPPORTS_CONTRACTS
        [System.Diagnostics.Contracts.Pure]
#endif
        [JetBrains.Annotations.Pure]
        public bool Test([NotNull] TEdge edge)
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(edge != null);
#endif
            return 0 < ResidualCapacities[ReversedEdges[edge]];
        }
    }
}