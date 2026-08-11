// -----------------------------------------------------------------------
// <copyright file="FixtureOutcomeSnapshotAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Match;
using MyClub.PlayUp.Domain.Stage;

namespace MyClub.PlayUp.Application.Stage;

/// <summary>
/// Builds a <see cref="FixtureOutcomeSnapshot"/> from an already-loaded match (data mapping only).
/// </summary>
/// <remarks>
/// Does not decide finished status, winner/loser, draw, or fixture applicability — Domain and the use case own those.
/// </remarks>
public static class FixtureOutcomeSnapshotAssembler
{
    /// <summary>
    /// Maps match state into an immutable domain snapshot.
    /// </summary>
    /// <param name="fixtureId">Fixture identity supplied by the use case.</param>
    /// <param name="match">Already-loaded match aggregate.</param>
    /// <returns>A data-only snapshot for <see cref="FixtureOutcomeResolver"/>.</returns>
    public static FixtureOutcomeSnapshot Assemble(FixtureId fixtureId, Match match)
    {
        ArgumentNullException.ThrowIfNull(match);

        return new FixtureOutcomeSnapshot(
            fixtureId,
            match.Id,
            match.HomeEntryId,
            match.AwayEntryId,
            match.Status,
            match.Result?.Score,
            match.Result?.PenaltyShootoutScore);
    }
}
