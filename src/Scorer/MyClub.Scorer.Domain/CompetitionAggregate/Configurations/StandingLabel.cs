// -----------------------------------------------------------------------
// <copyright file="StandingLabel.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Shared.Domain.ValueObjects;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Configurations;

/// <summary>
/// Represents a label that categorizes specific positions or ranges of positions in competition standings.
/// Standing labels provide semantic meaning to table positions, such as "Champion", "European Qualification",
/// "Relegation Zone", etc., and are used to visually and functionally distinguish different ranking outcomes.
/// </summary>
/// <param name="Ranks">The range of ranks (positions) that this label applies to.</param>
/// <param name="Color">Optional color code for visual representation of this label category.</param>
/// <param name="Name">The full name of the standing label.</param>
/// <param name="ShortName">The abbreviated name for the standing label.</param>
/// <param name="Description">Optional detailed description of what this standing label represents.</param>
/// <param name="Order">Optional sort order for displaying multiple labels.</param>
public record StandingLabel(Interval<int> Ranks, string? Color, string Name, string ShortName, string? Description = null, int? Order = null)
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private StandingLabel()
        : this(null!, null, null!, null!) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StandingLabel"/> class with a specified range of ranks.
    /// This convenience constructor creates a label that applies to exactly one position.
    /// </summary>
    /// <param name="rank">The specific rank (position) that this label applies to.</param>
    /// <param name="color">Optional color code for visual representation.</param>
    /// <param name="name">The full name of the standing label.</param>
    /// <param name="shortName">The abbreviated name for the standing label.</param>
    /// <param name="description">Optional detailed description of what this label represents.</param>
    /// <param name="order">Optional sort order for displaying multiple labels.</param>
    public StandingLabel(int rank, string? color, string name, string shortName, string? description = null, int? order = null)
        : this(new Interval<int>(rank, rank), color, name, shortName, description, order) { }

    /// <summary>
    /// Gets the reference object containing the label's name, short name, description, and order.
    /// This provides a standardized way to access label metadata.
    /// </summary>
    public Reference Reference { get; } = new(Name, ShortName, Description, Order);

    /// <summary>
    /// Gets the optional color code associated with this standing label.
    /// Colors are typically used for visual representation in standings tables and UI elements.
    /// </summary>
    public string? Color { get; } = Color;

    /// <summary>
    /// Determines whether the specified rank falls within this label's range.
    /// </summary>
    /// <param name="rank">The rank (position) to test.</param>
    /// <returns>True if the rank falls within this label's range; false otherwise.</returns>
    public bool Contains(int rank) => Ranks.Contains(rank);
}
