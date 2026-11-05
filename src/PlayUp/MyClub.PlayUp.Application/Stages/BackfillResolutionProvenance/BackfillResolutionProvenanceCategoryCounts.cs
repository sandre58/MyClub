// -----------------------------------------------------------------------
// <copyright file="BackfillResolutionProvenanceCategoryCounts.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Witness counts written during a backfill run, broken down by provenance kind.
/// </summary>
/// <param name="Directe">Slots with verified direct-assignment provenance.</param>
/// <param name="Qualification">Qualification replay witnesses.</param>
/// <param name="Progression">Progression replay witnesses.</param>
/// <param name="Tirage">Published draw witnesses.</param>
/// <param name="NonVerifiee">Explicit non-verified witnesses.</param>
public sealed record BackfillResolutionProvenanceCategoryCounts(
    int Directe,
    int Qualification,
    int Progression,
    int Tirage,
    int NonVerifiee);
