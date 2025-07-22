// -----------------------------------------------------------------------
// <copyright file="Matchday.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Scorer.Domain.Primitives;
using MyClub.Shared.Domain.ValueObjects;

namespace MyClub.Scorer.Domain.MatchdayAggregate;

public class Matchday : MatchScope<MatchdayId>
{
    // <remarks>Used by EF Core</remarks>
    private Matchday()
        : base() => DisplayName = null!;

    private Matchday(MatchdayId id, DateTime date, string name, string? shortName = null)
        : base(id, date) => DisplayName = new(name, shortName);

    public static Matchday Create(DateTime date, string name, string? shortName = null) => new(MatchdayId.New(), date, name, shortName);

    public DisplayName DisplayName { get; }

    public override string ToString() => DisplayName;
}
