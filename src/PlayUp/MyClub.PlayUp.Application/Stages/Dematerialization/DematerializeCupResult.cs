// -----------------------------------------------------------------------
// <copyright file="DematerializeCupResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Outcome of <see cref="DematerializeCup"/>.
/// </summary>
/// <param name="RemovedMatchCount">Matches removed from persistence (0 when idempotent).</param>
public sealed record DematerializeCupResult(int RemovedMatchCount);
