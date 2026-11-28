// -----------------------------------------------------------------------
// <copyright file="MatchResultEntry.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Proposed match result as entered on finish/correct routes (HTTP body shape, without a confrontation tie flag).
/// </summary>
public sealed record MatchResultEntry(
    ResultType Type,
    int HomeGoals,
    int AwayGoals,
    bool ExtraTimePlayed = false,
    int? PenaltyShootoutHomeGoals = null,
    int? PenaltyShootoutAwayGoals = null);
