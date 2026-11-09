// -----------------------------------------------------------------------
// <copyright file="DeferredParticipantsRecetteNames.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Stable names for the deferred-participant visual recette.
/// </summary>
public static class DeferredParticipantsRecetteNames
{
    /// <summary>Scenario id for <c>--scenarios</c>.</summary>
    public const string ScenarioId = "deferred-participants-recette";

    /// <summary>Semi-final materialized before either side is known.</summary>
    public const string Planned = "RECETTE T5 — Demies planifiées";

    /// <summary>One side filled by Apply, the other still unknown.</summary>
    public const string OneSide = "RECETTE T5 — Un côté résolu";

    /// <summary>Both sides filled by two successive Applies.</summary>
    public const string BothSides = "RECETTE T5 — Les deux côtés résolus";

    /// <summary>Group standing next to a cup match that still has an unknown side.</summary>
    public const string PoolStanding = "RECETTE T5 — Poules sans match différé";
}
