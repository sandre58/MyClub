// -----------------------------------------------------------------------
// <copyright file="RoundStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Scorer.Domain.Primitives;
using MyClub.Shared.Domain.ValueObjects;

namespace MyClub.Scorer.Domain.RoundAggregate;

public class RoundStage : MatchScope<RoundStageId>
{
    // <remarks>Used by EF Core</remarks>
    private RoundStage()
        : base() => DisplayName = null!;

    private RoundStage(RoundStageId id, DateTime date, string name, string? shortName = null)
        : base(id, date) => DisplayName = new(name, shortName);

    public static RoundStage Create(DateTime date, string name, string? shortName = null) => new(RoundStageId.New(), date, name, shortName);

    public DisplayName DisplayName { get; }

    public override string ToString() => DisplayName;
}
