// -----------------------------------------------------------------------
// <copyright file="DematerializeSwissLastRoundResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Outcome of <see cref="DematerializeSwissLastRound"/>.
/// </summary>
/// <param name="RemovedMatchCount">Matches deleted from persistence (0 when nothing to remove).</param>
/// <param name="RemovedRoundNumber">Swiss round removed; null when idempotent no-op.</param>
public sealed record DematerializeSwissLastRoundResult(int RemovedMatchCount, int? RemovedRoundNumber);
