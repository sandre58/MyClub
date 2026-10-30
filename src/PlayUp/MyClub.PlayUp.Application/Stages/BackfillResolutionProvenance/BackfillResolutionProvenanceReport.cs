// -----------------------------------------------------------------------
// <copyright file="BackfillResolutionProvenanceReport.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Outcome counters for a single competition resolution-provenance backfill run.
/// </summary>
/// <param name="CompetitionId">Competition that was processed.</param>
/// <param name="BackfilledExact">Witnesses written with a verified Qual/Prog/Draw/Direct path.</param>
/// <param name="NonVerifiee">Witnesses written as explicit non-verified.</param>
/// <param name="Indeterminate">Qual/Prog replays that did not resolve to exactly one entry.</param>
/// <param name="SkippedAlreadyPresent">Rows skipped because a non-placeholder provenance was already stored.</param>
/// <param name="Categories">Breakdown of witnesses written by provenance kind.</param>
public sealed record BackfillResolutionProvenanceReport(
    CompetitionId CompetitionId,
    int BackfilledExact,
    int NonVerifiee,
    int Indeterminate,
    int SkippedAlreadyPresent,
    BackfillResolutionProvenanceCategoryCounts Categories);
