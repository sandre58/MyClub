// -----------------------------------------------------------------------
// <copyright file="MatchResultFromEntry.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Builds a domain <see cref="MatchResult"/> from route entry after confrontation tie eligibility.
/// </summary>
/// <remarks>Eligibility on finish/result routes is a precondition for the flagged factory.</remarks>
internal static class MatchResultFromEntry
{
    /// <summary>
    /// Maps <paramref name="entry"/> to a domain result, using the confrontation tie factory only when eligible.
    /// </summary>
    public static async Task<MatchResult> BuildAsync(
        Stage stage,
        Match match,
        MatchResultEntry entry,
        IMatchRepository matches,
        CancellationToken cancellationToken)
    {
        var (type, score, extraTimePlayed, shootout) = Parse(entry);

        if (shootout is null || score.HomeGoals == score.AwayGoals)
        {
            return new MatchResult(type, score, extraTimePlayed, shootout);
        }

        if (await ConfrontationTiePenaltyShootoutEligibility
                .IsEligibleAsync(stage, match, entry, matches, cancellationToken)
                .ConfigureAwait(false))
        {
            return MatchResult.CreateConfrontationTiePenaltyShootout(score, extraTimePlayed, shootout.Value);
        }

        return new MatchResult(type, score, extraTimePlayed, shootout);
    }

    private static (ResultType Type, Score Score, bool ExtraTimePlayed, PenaltyShootoutScore? Shootout) Parse(
        MatchResultEntry entry)
    {
        PenaltyShootoutScore? shootout = null;
        if (entry.PenaltyShootoutHomeGoals is { } homeShootout && entry.PenaltyShootoutAwayGoals is { } awayShootout)
        {
            shootout = new PenaltyShootoutScore(homeShootout, awayShootout);
        }
        else if (entry.PenaltyShootoutHomeGoals is not null || entry.PenaltyShootoutAwayGoals is not null)
        {
            throw new DomainException(
                "Penalty shootout requires both home and away kick counts.",
                MatchErrorCodes.InvalidResult);
        }

        return (entry.Type, new Score(entry.HomeGoals, entry.AwayGoals), entry.ExtraTimePlayed, shootout);
    }
}
