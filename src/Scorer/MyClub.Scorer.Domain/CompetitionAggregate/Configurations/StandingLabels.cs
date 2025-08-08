// -----------------------------------------------------------------------
// <copyright file="StandingLabels.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Configurations;

/// <summary>
/// Represents a collection of standing labels that categorize different positions in competition standings.
/// This collection provides a fluent interface for building comprehensive standing label systems
/// and enables quick lookup of labels based on team rankings.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "It's a special collection.")]
public class StandingLabels : IReadOnlyCollection<StandingLabel>
{
    private readonly List<StandingLabel> _statuses = [];

    /// <summary>
    /// Gets the number of standing labels in this collection.
    /// </summary>
    public int Count => _statuses.Count;

    /// <summary>
    /// Adds a standing label for a specific rank (position) to the collection.
    /// This method provides a fluent interface for building label configurations.
    /// </summary>
    /// <param name="rank">The specific rank (position) that this label applies to.</param>
    /// <param name="color">Optional color code for visual representation of this label category.</param>
    /// <param name="name">The full name of the standing label.</param>
    /// <param name="shortName">The abbreviated name for the standing label.</param>
    /// <param name="description">Optional detailed description of what this label represents.</param>
    /// <param name="order">Optional sort order for displaying multiple labels.</param>
    /// <returns>The current StandingLabels instance for method chaining.</returns>
    public StandingLabels Add(int rank, string? color, string name, string shortName, string? description = null, int? order = null)
        => Add(new Interval<int>(rank, rank), color, name, shortName, description, order);

    /// <summary>
    /// Adds a standing label for a range of ranks (positions) to the collection.
    /// This method allows categorizing multiple consecutive positions under the same label.
    /// </summary>
    /// <param name="ranks">The range of ranks (positions) that this label applies to.</param>
    /// <param name="color">Optional color code for visual representation of this label category.</param>
    /// <param name="name">The full name of the standing label.</param>
    /// <param name="shortName">The abbreviated name for the standing label.</param>
    /// <param name="description">Optional detailed description of what this label represents.</param>
    /// <param name="order">Optional sort order for displaying multiple labels.</param>
    /// <returns>The current StandingLabels instance for method chaining.</returns>
    public StandingLabels Add(Interval<int> ranks, string? color, string name, string shortName, string? description = null, int? order = null)
    {
        _statuses.Add(new(ranks, color, name, shortName, description, order));
        return this;
    }

    /// <summary>
    /// Retrieves the standing label that applies to the specified rank (position).
    /// This method enables automatic categorization of teams based on their current standings position.
    /// </summary>
    /// <param name="rank">The rank (position) to find a label for.</param>
    /// <returns>
    /// The <see cref="StandingLabel"/> that contains the specified rank, or null if no label applies to that position.
    /// </returns>
    public StandingLabel? GetStatus(int rank) => _statuses.FirstOrDefault(s => s.Contains(rank));

    /// <summary>
    /// Returns an enumerator that iterates through the standing labels in this collection.
    /// </summary>
    /// <returns>An enumerator for the standing labels.</returns>
    public IEnumerator<StandingLabel> GetEnumerator() => _statuses.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the standing labels in this collection.
    /// </summary>
    /// <returns>An enumerator for the standing labels.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
