// -----------------------------------------------------------------------
// <copyright file="StandingRankStatus.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Domain.ValueObjects;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Configurations;

public record StandingRankStatus(Interval<int> Ranks, string? Color, string Name, string ShortName, string? Description = null, int? Order = null)
{
    public StandingRankStatus(int rank, string? color, string name, string shortName, string? description = null, int? order = null)
        : this(new Interval<int>(rank, rank), color, name, shortName, description, order) { }

    public Reference Reference { get; } = new(Name, ShortName, Description, Order);

    public string? Color { get; } = Color;

    public bool Contains(int rank) => Ranks.Contains(rank);
}
