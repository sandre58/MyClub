// -----------------------------------------------------------------------
// <copyright file="StructurePreparePlacesGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Prepare gate: operational completeness (composition pool, schematic occupancy) is Materialize-only (D3).
/// Integrity feeds and graph gates are handled by PrepareStage and lifecycle analyzer facts.
/// </summary>
internal static class StructurePreparePlacesGate
{
    internal static bool BlocksPrepare(StructureStageLifecycleInput _) => false;

    internal static void Ensure(Competition _, Stage __, IReadOnlyList<Stage> ___)
    {
    }
}
