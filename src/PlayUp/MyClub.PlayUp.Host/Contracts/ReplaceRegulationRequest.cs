// -----------------------------------------------------------------------
// <copyright file="ReplaceRegulationRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing competition regulation (Slice 2).
/// Ranking criteria stay on the bootstrap baseline (Points, GD, GF, H2H).
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
    int ForfeitLoserGoals = 0);
