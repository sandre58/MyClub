// -----------------------------------------------------------------------
// <copyright file="MatchResultDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Finished-match result as a product read (no Winner).
/// </summary>
/// <param name="Type">How the result was obtained.</param>
/// <param name="HomeGoals">Play goals for home.</param>
/// <param name="AwayGoals">Play goals for away.</param>
/// <param name="ExtraTimePlayed">Whether extra time was played.</param>
/// <param name="Shootout">Shootout kicks when taken; otherwise <see langword="null"/>.</param>
public sealed record MatchResultDto(
    ResultType Type,
    int HomeGoals,
    int AwayGoals,
    bool ExtraTimePlayed,
    MatchScoreDto? Shootout);
