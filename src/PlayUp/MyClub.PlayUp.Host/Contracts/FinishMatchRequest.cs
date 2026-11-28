// -----------------------------------------------------------------------
// <copyright file="FinishMatchRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for finishing a match. Mapped to Application <c>MatchResultEntry</c> at the Host boundary.
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
    int? PenaltyShootoutAwayGoals = null);
