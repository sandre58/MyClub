// -----------------------------------------------------------------------
// <copyright file="FinishMatchRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for finishing a match. Maps to Domain <see cref="MatchResult"/> without duplicating invariants.
/// </summary>
/// <param name="Type">How the result was obtained.</param>
/// <param name="HomeGoals">Play goals for home (≥ 0).</param>
/// <param name="AwayGoals">Play goals for away (≥ 0).</param>
/// <param name="ExtraTimePlayed">Whether extra time was played.</param>
/// <param name="PenaltyShootoutHomeGoals">Optional shootout home kicks; both shootout fields required together.</param>
/// <param name="PenaltyShootoutAwayGoals">Optional shootout away kicks; both shootout fields required together.</param>
public sealed record FinishMatchRequest(
    ResultType Type,
    int HomeGoals,
    int AwayGoals,
    bool ExtraTimePlayed = false,
    int? PenaltyShootoutHomeGoals = null,
    int? PenaltyShootoutAwayGoals = null)
{
    /// <summary>
    /// Builds the Domain <see cref="MatchResult"/>; Domain validates structural invariants.
    /// </summary>
    /// <returns>The Domain match result.</returns>
    /// <exception cref="DomainException">Thrown when shootout fields are partially specified or Domain rejects the result.</exception>
    public MatchResult ToDomain()
    {
        PenaltyShootoutScore? shootout = null;
        if (PenaltyShootoutHomeGoals is { } homeShootout && PenaltyShootoutAwayGoals is { } awayShootout)
        {
            shootout = new PenaltyShootoutScore(homeShootout, awayShootout);
        }
        else if (PenaltyShootoutHomeGoals is not null || PenaltyShootoutAwayGoals is not null)
        {
            throw new DomainException(
                "Penalty shootout requires both home and away kick counts.",
                MatchErrorCodes.InvalidResult);
        }

        return new MatchResult(Type, new Score(HomeGoals, AwayGoals), ExtraTimePlayed, shootout);
    }
}
