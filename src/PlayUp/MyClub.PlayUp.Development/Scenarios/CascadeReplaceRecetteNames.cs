// -----------------------------------------------------------------------
// <copyright file="CascadeReplaceRecetteNames.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Stable names for the cascade-replacement visual recette.
/// </summary>
public static class CascadeReplaceRecetteNames
{
    /// <summary>Scenario id for <c>--scenarios</c>.</summary>
    public const string ScenarioId = "cascade-replace-recette";

    /// <summary>One stale slot and an unplayed downstream match.</summary>
    public const string Simple = "RECETTE T4 — Cascade simple";

    /// <summary>A played downstream match whose score the confirmation drops.</summary>
    public const string Played = "RECETTE T4 — Match aval joué";

    /// <summary>Group qualification feeding a middle group, then a final slot.</summary>
    public const string Transitive = "RECETTE T4 — Transitif 3 niveaux";

    /// <summary>The destination phase is completed.</summary>
    public const string Completed = "RECETTE T4 — Refus phase terminée";

    /// <summary>The pointed occupant is a direct assignment.</summary>
    public const string Direct = "RECETTE T4 — Refus affectation directe";
}
