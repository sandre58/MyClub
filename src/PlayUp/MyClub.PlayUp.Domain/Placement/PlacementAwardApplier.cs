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
/// Pure placement helper: maps an award path and fixture outcome to a final-rank instruction.
/// Distinct from <see cref="Progression.ProgressionApplier"/> (slot routing).
/// </summary>
public static class PlacementAwardApplier
{
    /// <summary>
    /// Applies a single placement award path to a decided fixture outcome.
    /// </summary>
    /// <param name="path">Declarative award path (source fixture, outcome, rank).</param>
    /// <param name="fixtureId">Fixture identity supplied by Application (must match <see cref="PlacementAwardPath.SourceFixtureId"/>).</param>
    /// <param name="outcome">Decided winner/loser of the confrontation.</param>
    /// <returns>Final placement instruction (no Stage mutation).</returns>
    [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Global", Justification = "False positive")]
    public static FinalPlacementInstruction Apply(
        PlacementAwardPath path,
        FixtureId fixtureId,
        FixtureOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(outcome);

        if (!path.SourceFixtureId.Equals(fixtureId))
        {
            throw new DomainException(
                "Placement award path source fixture does not match the supplied fixture identity.",
                StageErrorCodes.PlacementAwardApplyFixtureMismatch);
        }

        var entryId = path.Outcome == ProgressionOutcome.Winner
            ? outcome.WinnerEntryId
            : outcome.LoserEntryId;

        return new FinalPlacementInstruction(path.Rank, entryId);
    }

    /// <summary>
    /// Applies all award paths for a fixture, ordered by rank ascending.
    /// </summary>
    /// <param name="rules">Placement award rules.</param>
    /// <param name="fixtureId">Fixture identity.</param>
    /// <param name="outcome">Decided winner/loser of the confrontation.</param>
    /// <returns>Instructions for that fixture (empty when no path matches).</returns>
    public static IReadOnlyList<FinalPlacementInstruction> ApplyForFixture(
        PlacementAwardRules rules,
        FixtureId fixtureId,
        FixtureOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(outcome);

        return
        [
            .. rules.Paths
                .Where(path => path.SourceFixtureId.Equals(fixtureId))
                .Select(path => Apply(path, fixtureId, outcome))
                .OrderBy(instruction => instruction.Rank)
        ];
    }
}
