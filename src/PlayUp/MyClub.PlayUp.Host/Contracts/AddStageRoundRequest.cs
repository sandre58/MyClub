// -----------------------------------------------------------------------
// <copyright file="AddStageRoundRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for adding a round to a stage.
/// </summary>
/// <param name="Name">Round display name.</param>
/// <param name="NumberOfLegs">Optional 1 or 2; omit to use stage regulation default.</param>
/// <param name="AggregateScoring">Optional; defaults to true when NumberOfLegs is 2.</param>
public sealed record AddStageRoundRequest(
    string Name,
    int? NumberOfLegs = null,
    bool? AggregateScoring = null);

/// <summary>
/// HTTP response after creating a round.
/// </summary>
/// <param name="RoundId">New round identity.</param>
/// <param name="Name">Normalized round name.</param>
public sealed record AddStageRoundResponse(Guid RoundId, string Name);
