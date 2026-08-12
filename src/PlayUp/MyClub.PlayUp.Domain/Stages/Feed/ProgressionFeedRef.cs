// -----------------------------------------------------------------------
// <copyright file="ProgressionFeedRef.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Unique progression feed identity for diagnostics.
/// </summary>
public sealed record ProgressionFeedRef(
    StageId SourceStageId,
    FixtureId SourceFixtureId,
    ProgressionOutcome Outcome);
