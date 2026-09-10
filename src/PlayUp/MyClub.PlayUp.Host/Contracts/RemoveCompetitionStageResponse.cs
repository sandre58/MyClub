// -----------------------------------------------------------------------
// <copyright file="RemoveCompetitionStageResponse.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP response after removing a competition stage (impact + refreshed Organisation view).
/// </summary>
/// <param name="RemovedStageId">Removed stage identity.</param>
/// <param name="ScrubbedQualificationPaths">Peer qualification paths cleared.</param>
/// <param name="ScrubbedProgressionPaths">Peer progression paths cleared.</param>
/// <param name="Organisation">Refreshed Organisation / Structure hub view.</param>
public sealed record RemoveCompetitionStageResponse(
    Guid RemovedStageId,
    int ScrubbedQualificationPaths,
    int ScrubbedProgressionPaths,
    OrganisationViewDto Organisation);
