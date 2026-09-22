// -----------------------------------------------------------------------
// <copyright file="AffectationAuthoringHistoricalBackfill.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// One-shot historical heuristic for S1 AffectationAuthoring backfill (not a durable Domain rule).
/// </summary>
public static class AffectationAuthoringHistoricalBackfill
{
    /// <summary>
    /// Returns whether a stage should receive Composition → Affectation copy during historical migration.
    /// True when no other stage lists <paramref name="stageId"/> as a Qual/Prog <c>DestinationStageId</c>.
    /// </summary>
    public static bool ShouldCopyCompositionToAffectation(
        Guid stageId,
        IReadOnlyList<(Guid StageId, string RegulationJson)> otherStages)
    {
        ArgumentNullException.ThrowIfNull(otherStages);
        var needle = $"\"DestinationStageId\":\"{stageId}\"";
        foreach (var (otherId, regulationJson) in otherStages)
        {
            if (otherId == stageId)
            {
                continue;
            }

            if (regulationJson.Contains(needle, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
