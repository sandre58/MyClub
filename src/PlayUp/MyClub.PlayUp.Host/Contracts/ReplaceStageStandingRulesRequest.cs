// -----------------------------------------------------------------------
// <copyright file="ReplaceStageStandingRulesRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing StandingRules on a classifying stage (allowed after Start).
/// </summary>
/// <param name="WinPoints">Points for a win.</param>
/// <param name="DrawPoints">Points for a draw.</param>
/// <param name="LossPoints">Points for a loss.</param>
/// <param name="RankingCriteria">Ordered ranking criteria (non-empty, no duplicates).</param>
public sealed record ReplaceStageStandingRulesRequest(
    int WinPoints,
    int DrawPoints,
    int LossPoints,
    IReadOnlyList<RankingCriterion> RankingCriteria);
