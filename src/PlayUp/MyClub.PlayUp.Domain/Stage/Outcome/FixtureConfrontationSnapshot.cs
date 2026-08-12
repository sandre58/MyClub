// -----------------------------------------------------------------------
// <copyright file="FixtureConfrontationSnapshot.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Immutable multi-leg (or single-leg) fixture data for outcome resolution (no sports validation in the constructor).
/// </summary>
/// <remarks>
/// Application assembles legs from <see cref="Fixture.Attachments"/> and loaded matches.
/// Resolution rules (aggregate by EntryId, away goals, last-leg TAB, I10) live in <see cref="FixtureOutcomeResolver"/>.
/// </remarks>
public sealed record FixtureConfrontationSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureConfrontationSnapshot"/> class.
    /// </summary>
    /// <param name="fixtureId">Fixture identity.</param>
    /// <param name="legs">Leg snapshots (any order; LegIndex carries semantics).</param>
    public FixtureConfrontationSnapshot(FixtureId fixtureId, IReadOnlyList<FixtureLegSnapshot> legs)
    {
        ArgumentNullException.ThrowIfNull(legs);
        FixtureId = fixtureId;
        Legs = legs;
    }

    /// <summary>Gets the fixture identity.</summary>
    public FixtureId FixtureId { get; }

    /// <summary>Gets the leg snapshots.</summary>
    public IReadOnlyList<FixtureLegSnapshot> Legs { get; }
}
