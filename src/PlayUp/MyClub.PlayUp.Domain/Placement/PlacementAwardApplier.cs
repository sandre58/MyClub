// -----------------------------------------------------------------------
// <copyright file="PlacementAwardApplier.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Domain.Placement;

/// <summary>
/// Pure placement helper: maps an award path and confrontation outcome to a final-rank instruction.
/// Distinct from <see cref="Progression.ProgressionApplier"/> (slot routing).
/// </summary>
public static class PlacementAwardApplier
{
    /// <summary>
    /// Applies a single placement award path to a decided confrontation outcome.
    /// </summary>
    /// <param name="path">Declarative award path (source pair key, outcome, rank).</param>
    /// <param name="sourcePairKey">
    /// Structural key supplied by Application (must match <see cref="PlacementAwardPath.SourcePairKey"/>).
    /// </param>
    /// <param name="outcome">Decided winner/loser of the confrontation.</param>
    /// <returns>Final placement instruction (no Stage mutation).</returns>
    [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Global", Justification = "False positive")]
    public static FinalPlacementInstruction Apply(
        PlacementAwardPath path,
        string sourcePairKey,
        FixtureOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(outcome);

        if (!string.Equals(path.SourcePairKey, BracketPair.NormalizePairKey(sourcePairKey), StringComparison.Ordinal))
        {
            throw new DomainException(
                "Placement award path source does not match the supplied confrontation key.",
                StageErrorCodes.PlacementAwardApplyFixtureMismatch);
        }

        var entryId = path.Outcome == ProgressionOutcome.Winner
            ? outcome.WinnerEntryId
            : outcome.LoserEntryId;

        return new FinalPlacementInstruction(path.Rank, entryId);
    }

    /// <summary>
    /// Applies all award paths for a structural source key, ordered by rank ascending.
    /// </summary>
    /// <param name="rules">Placement award rules.</param>
    /// <param name="sourcePairKey">Structural confrontation key.</param>
    /// <param name="outcome">Decided winner/loser of the confrontation.</param>
    /// <returns>Instructions for that source (empty when no path matches).</returns>
    public static IReadOnlyList<FinalPlacementInstruction> ApplyForSource(
        PlacementAwardRules rules,
        string sourcePairKey,
        FixtureOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(outcome);

        var key = BracketPair.NormalizePairKey(sourcePairKey);
        return
        [
            .. rules.Paths
                .Where(path => string.Equals(path.SourcePairKey, key, StringComparison.Ordinal))
                .Select(path => Apply(path, key, outcome))
                .OrderBy(instruction => instruction.Rank)
        ];
    }
}
