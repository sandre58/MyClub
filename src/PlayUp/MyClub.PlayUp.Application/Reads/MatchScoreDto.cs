// -----------------------------------------------------------------------
// <copyright file="MatchScoreDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Play score for a finished match (shootout excluded).
/// </summary>
/// <param name="HomeGoals">Home goals.</param>
/// <param name="AwayGoals">Away goals.</param>
public sealed record MatchScoreDto(int HomeGoals, int AwayGoals);
