// -----------------------------------------------------------------------
// <copyright file="MaterializeMatchesResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Outcome of <see cref="MaterializeMatches"/>.
/// </summary>
/// <param name="CreatedMatches">Newly created Match aggregates (caller must Add + SaveChanges).</param>
/// <param name="AttachedMatchIds">All match identities attached after the operation.</param>
/// <param name="AlreadyComplete">True when expected matches were already present (idempotent).</param>
public sealed record MaterializeMatchesResult(
    IReadOnlyList<Match> CreatedMatches,
    IReadOnlyList<MatchId> AttachedMatchIds,
    bool AlreadyComplete);
