// -----------------------------------------------------------------------
// <copyright file="SetRunningScoreRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing the observed Live running score.
/// </summary>
/// <param name="HomeGoals">Current home goals (≥ 0).</param>
/// <param name="AwayGoals">Current away goals (≥ 0).</param>
public sealed record SetRunningScoreRequest(int HomeGoals, int AwayGoals);
