// -----------------------------------------------------------------------
// <copyright file="AddDeclaredParticipationRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for adding a player to the match composition sheet.
/// </summary>
/// <param name="MemberId">Declared member identity on the owning entry roster.</param>
/// <param name="Side">Match side for this participation.</param>
/// <param name="CompositionStatus">Starter or bench status.</param>
/// <param name="JerseyNumber">Optional jersey number for this match.</param>
public sealed record AddDeclaredParticipationRequest(
    Guid MemberId,
    Side Side,
    CompositionStatus CompositionStatus,
    int? JerseyNumber = null);
