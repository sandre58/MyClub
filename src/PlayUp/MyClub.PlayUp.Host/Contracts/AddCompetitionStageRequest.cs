// -----------------------------------------------------------------------
// <copyright file="AddCompetitionStageRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for adding a stage to a competition.
/// </summary>
/// <param name="Name">Stage display name.</param>
public sealed record AddCompetitionStageRequest(string Name);

/// <summary>
/// HTTP response after creating a stage.
/// </summary>
/// <param name="StageId">New stage identity.</param>
/// <param name="Name">Normalized stage name.</param>
public sealed record AddCompetitionStageResponse(Guid StageId, string Name);
