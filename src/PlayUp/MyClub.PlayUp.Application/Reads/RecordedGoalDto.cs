// -----------------------------------------------------------------------
// <copyright file="RecordedGoalDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Nominative goal attribution on a match (distinct from running and official score).
/// </summary>
public sealed record RecordedGoalDto(
    Guid GoalId,
    Guid ScorerMemberId,
    string? ScorerDisplayName,
    Side CreditedSide,
    Guid? AssisterMemberId,
    string? AssisterDisplayName,
    bool IsOwnGoal);
