// -----------------------------------------------------------------------

// <copyright file="DematerializePhaseResult.cs" company="Stéphane ANDRE">

// Copyright (c) Stéphane ANDRE. All rights reserved.

// </copyright>

// -----------------------------------------------------------------------



namespace MyClub.PlayUp.Application.Stages;



/// <summary>

/// Outcome of format-routed phase dematerialization.

/// </summary>

/// <param name="RemovedMatchCount">Matches removed from persistence (0 when idempotent).</param>

/// <param name="RemovedRoundNumber">Swiss only: matchday number removed; null for other formats.</param>

public sealed record DematerializePhaseResult(int RemovedMatchCount, int? RemovedRoundNumber);

