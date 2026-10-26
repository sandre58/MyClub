// -----------------------------------------------------------------------
// <copyright file="SetCompetitionStageOrderRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for reordering competition stages.
/// </summary>
/// <param name="StageIds">Permutation of the competition's current stage ids. Order is the only payload.</param>
public sealed record SetCompetitionStageOrderRequest(IReadOnlyList<Guid> StageIds);
