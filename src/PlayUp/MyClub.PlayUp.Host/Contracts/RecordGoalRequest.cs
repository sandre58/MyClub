// -----------------------------------------------------------------------
// <copyright file="RecordGoalRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for recording or correcting a nominative goal attribution.
/// </summary>
/// <param name="ScorerMemberId">Scorer member identity on the match sheet.</param>
/// <param name="CreditedSide">Side credited with the goal.</param>
/// <param name="AssisterMemberId">Optional assister member identity.</param>
public sealed record RecordGoalRequest(
    Guid ScorerMemberId,
    Side CreditedSide,
    Guid? AssisterMemberId = null);
