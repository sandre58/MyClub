// -----------------------------------------------------------------------
// <copyright file="MatchDetailDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Match detail for Start/Finish context and match hub reads (no Winner).
/// </summary>
/// <param name="MatchId">Match identity.</param>
/// <param name="CompetitionId">Owning competition.</param>
/// <param name="StageId">Owning stage.</param>
/// <param name="Status">Match lifecycle status.</param>
/// <param name="Home">Home side.</param>
/// <param name="Away">Away side.</param>
/// <param name="Result">Full result when finished; otherwise <see langword="null"/>.</param>
/// <param name="FixtureId">Owning fixture when attached.</param>
/// <param name="LegIndex">Leg index when attached.</param>
/// <param name="ScheduledAt">Optional calendar start from Stage placement.</param>
/// <param name="ResourceId">Optional scheduling resource from Stage placement.</param>
/// <param name="HasObservedLive">Whether MyClub opened Live for this match.</param>
/// <param name="RunningScore">Observed Live score when present; otherwise <see langword="null"/>.</param>
/// <param name="DeclaredParticipations">Declared composition sheet lines.</param>
/// <param name="RecordedGoals">Nominative goal attributions.</param>
/// <param name="RecordedSubstitutions">Ordered substitution facts.</param>
/// <param name="RecordedDisciplinaryEvents">Disciplinary facts.</param>
public sealed record MatchDetailDto(
    Guid MatchId,
    Guid CompetitionId,
    Guid StageId,
    MatchStatus Status,
    EntrySideDto Home,
    EntrySideDto Away,
    MatchResultDto? Result,
    Guid? FixtureId,
    int? LegIndex,
    DateTimeOffset? ScheduledAt = null,
    Guid? ResourceId = null,
    bool HasObservedLive = false,
    MatchScoreDto? RunningScore = null,
    IReadOnlyList<DeclaredParticipationDto>? DeclaredParticipations = null,
    IReadOnlyList<RecordedGoalDto>? RecordedGoals = null,
    IReadOnlyList<RecordedSubstitutionDto>? RecordedSubstitutions = null,
    IReadOnlyList<RecordedDisciplinaryEventDto>? RecordedDisciplinaryEvents = null);
