// -----------------------------------------------------------------------
// <copyright file="FixtureOutcomeResolver.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Pure single-leg fixture outcome helper: derives Winner/Loser from a snapshot (no aggregate loading).
/// </summary>
/// <remarks>
/// Resolution uses play <see cref="Match.Score"/> and optional <see cref="Match.PenaltyShootoutScore"/> only.
/// Extra time played is never consulted.
/// </remarks>
public static class FixtureOutcomeResolver
{
    /// <summary>
    /// Resolves winner and loser from a finished single-leg snapshot.
    /// </summary>
    /// <param name="snapshot">Immutable outcome snapshot assembled by Application.</param>
    /// <returns>The decided fixture outcome.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is <see langword="null"/>.</exception>
    /// <exception cref="DomainException">Outcome undecided, not finished, or invalid.</exception>
    public static FixtureOutcome Resolve(FixtureOutcomeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return snapshot.HomeEntryId.Equals(snapshot.AwayEntryId)
            ? throw new DomainException(
                "Fixture outcome requires distinct home and away entries.",
                StageErrorCodes.FixtureOutcomeInvalid)
            : snapshot.Status != MatchStatus.Finished
            ? throw new DomainException(
                $"Fixture outcome requires a finished match (status was '{snapshot.Status}').",
                StageErrorCodes.FixtureOutcomeNotFinished)
            : snapshot.Score is not { } score
            ? throw new DomainException(
                "Fixture outcome requires a score when the match is finished.",
                StageErrorCodes.FixtureOutcomeInvalid)
            : score.HomeGoals != score.AwayGoals
            ? score.HomeGoals > score.AwayGoals
                ? new FixtureOutcome(snapshot.HomeEntryId, snapshot.AwayEntryId)
                : new FixtureOutcome(snapshot.AwayEntryId, snapshot.HomeEntryId)
            : snapshot.PenaltyShootoutScore is not { } shootout
                ? throw new DomainException(
                    "Fixture outcome is undecided when the score is a draw.",
                    StageErrorCodes.FixtureOutcomeUndecided)
                : shootout.HomeGoals == shootout.AwayGoals
                    ? throw new DomainException(
                        "Fixture outcome requires a decisive penalty shootout score.",
                        StageErrorCodes.FixtureOutcomeInvalid)
                    : shootout.HomeGoals > shootout.AwayGoals
                        ? new FixtureOutcome(snapshot.HomeEntryId, snapshot.AwayEntryId)
                        : new FixtureOutcome(snapshot.AwayEntryId, snapshot.HomeEntryId);
    }
}
