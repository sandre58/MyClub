// -----------------------------------------------------------------------
// <copyright file="DematerializeChampionshipOrGroupsResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Outcome of <see cref="DematerializeChampionshipOrGroups"/>.
/// </summary>
/// <param name="RemovedMatchCount">Matches deleted from persistence (0 when already dematerialized).</param>
public sealed record DematerializeChampionshipOrGroupsResult(int RemovedMatchCount);
