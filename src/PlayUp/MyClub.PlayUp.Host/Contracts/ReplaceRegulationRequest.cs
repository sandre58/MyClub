// -----------------------------------------------------------------------
// <copyright file="ReplaceRegulationRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing the full competition regulation (defaults), then propagating bound stage parts.
/// <see cref="AllowedTypes"/>: omit/<see langword="null"/> = preserve existing
/// <see cref="DisciplinaryRules"/>; empty = <see cref="DisciplinaryRules.None"/>;
/// non-empty = replace catalogue.
/// Extra time / shootout: omit or null = disabled; send values when enabled.
/// </summary>
public sealed record ReplaceRegulationRequest(
    int MinimumTeams,
    int MaximumTeams,
    int DurationPerPeriod,
    int NumberOfPeriods,
    int HalfTimeDuration,
    int WinPoints,
    int DrawPoints,
    int LossPoints,
    int ForfeitWinnerGoals = 3,
    int ForfeitLoserGoals = 0,
    IReadOnlyList<DisciplinaryType>? AllowedTypes = null,
    IReadOnlyList<RankingCriterion>? RankingCriteria = null,
    bool HasExtraTime = false,
    int? ExtraTimeDurationPerPeriod = null,
    int? ExtraTimeNumberOfPeriods = null,
    bool HasPenaltyShootout = false,
    int? PenaltyInitialKicksPerTeam = null);
