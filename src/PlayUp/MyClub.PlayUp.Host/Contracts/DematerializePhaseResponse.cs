// -----------------------------------------------------------------------
// <copyright file="DematerializePhaseResponse.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP response for phase dematerialization.
/// </summary>
/// <param name="RemovedMatchCount">Matches removed from persistence.</param>
/// <param name="RemovedRoundNumber">Swiss only: removed matchday number when applicable.</param>
public sealed record DematerializePhaseResponse(int RemovedMatchCount, int? RemovedRoundNumber);
