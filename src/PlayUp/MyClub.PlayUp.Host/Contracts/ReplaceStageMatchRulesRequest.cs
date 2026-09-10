// -----------------------------------------------------------------------
// <copyright file="ReplaceStageMatchRulesRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for specializing MatchRules on a stage (unbinds changed heritable parts).
/// Extra time / shootout: omit or false = disabled; send values when enabled.
/// </summary>
public sealed record ReplaceStageMatchRulesRequest(
    int DurationPerPeriod,
    int NumberOfPeriods,
    int HalfTimeDuration,
    int ForfeitWinnerGoals = 3,
    int ForfeitLoserGoals = 0,
    bool HasExtraTime = false,
    int? ExtraTimeDurationPerPeriod = null,
    int? ExtraTimeNumberOfPeriods = null,
    bool HasPenaltyShootout = false,
    int? PenaltyInitialKicksPerTeam = null);
