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
internal static class MatchResultFromEntry
{
    /// <summary>
    /// Validates entry shape (partial shootout, unknown type, etc.) before match load.
    /// </summary>
    public static MatchResultEntryShape ValidateShape(MatchResultEntry entry) => Parse(entry);

    /// <summary>
    /// Maps validated entry to a domain result under stage/match context.
    /// </summary>
    public static async Task<MatchResult> BuildAsync(
        Stage stage,
        Match match,
        MatchResultEntryShape shape,
        IMatchRepository matches,
        CancellationToken cancellationToken)
    {
        if (!shape.RequiresConfrontationTieEligibility)
        {
            return new MatchResult(shape.Type, shape.Score, shape.ExtraTimePlayed, shape.Shootout);
        }

        return await ConfrontationTiePenaltyShootoutEligibility
            .BuildFlaggedResultAsync(stage, match, shape, matches, cancellationToken)
            .ConfigureAwait(false);
    }

    private static MatchResultEntryShape Parse(MatchResultEntry entry)
    {
        if (!Enum.IsDefined(entry.Type))
        {
            throw new DomainException(
                $"Unknown result type '{entry.Type}'.",
                MatchErrorCodes.InvalidResult);
        }

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

        return new MatchResultEntryShape(
            entry.Type,
            new Score(entry.HomeGoals, entry.AwayGoals),
            entry.ExtraTimePlayed,
            shootout);
    }
}
