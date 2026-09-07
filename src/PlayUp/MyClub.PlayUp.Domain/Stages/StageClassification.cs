// -----------------------------------------------------------------------
// <copyright file="StageClassification.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Domain predicate for A5: whether a stage topology produces or uses a standing as phase business data.
/// Used only to build/validate standing presence — runtime/reads use <c>Stage.Regulation.StandingRules != null</c>.
/// </summary>
/// <remarks>
/// Not derived from <c>QualificationRules</c> alone. Groups without qualification still classify.
/// Cup/KO (rounds, not Swiss) does not classify. Unstructured stages are neither classifying nor knockout.
/// </remarks>
public static class StageClassification
{
    /// <summary>
    /// Returns whether the stage topology is classifying (Championship / Groups / Swiss shape).
    /// </summary>
    /// <param name="stage">The stage.</param>
    /// <returns><see langword="true"/> when the phase must have standing rules.</returns>
    public static bool IsClassifyingPhase(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (stage.IsSwiss)
        {
            return true;
        }

        if (stage.Groups.Count > 0)
        {
            return true;
        }

        // Championship: matchdays without knockout rounds.
        return stage.Matchdays.Count > 0 && stage.Rounds.Count == 0;
    }

    /// <summary>
    /// Returns whether the stage topology is non-classifying knockout (Cup V1: rounds, not Swiss).
    /// </summary>
    /// <param name="stage">The stage.</param>
    /// <returns><see langword="true"/> when standing rules must be absent.</returns>
    public static bool IsNonClassifyingPhase(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        return stage.Rounds.Count > 0 && !stage.IsSwiss;
    }
}
