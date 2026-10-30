// -----------------------------------------------------------------------
// <copyright file="PathOccupantSelector.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Domain.Qualification;

/// <summary>
/// Shared pure selector for qualification and progression paths (Apply, rattrapage, future detector).
/// </summary>
public static class PathOccupantSelector
{
    /// <summary>
    /// Selects the occupant for a qualification path from a standing view.
    /// </summary>
    public static PathOccupantSelectionResult SelectQualification(QualificationPath path, Standing standing)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(standing);

        IReadOnlyList<EntryId> selected;
        try
        {
            selected = QualificationApplier.SelectEntries(standing, path.Selection);
        }
        catch (DomainException)
        {
            return PathOccupantSelectionResult.Indeterminate();
        }

        return selected.Count switch
        {
            0 => PathOccupantSelectionResult.Indeterminate(),
            > 1 => PathOccupantSelectionResult.Indeterminate(),
            _ => ResolveQualificationCondition(path, standing, selected[0])
        };
    }

    /// <summary>
    /// Selects the occupant for a progression path from a decided fixture outcome.
    /// </summary>
    public static PathOccupantSelectionResult SelectProgression(ProgressionPath path, FixtureOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(outcome);

        var entryId = path.Outcome == ProgressionOutcome.Winner
            ? outcome.WinnerEntryId
            : outcome.LoserEntryId;
        return PathOccupantSelectionResult.Resolved(entryId);
    }

    private static PathOccupantSelectionResult ResolveQualificationCondition(
        QualificationPath path,
        Standing standing,
        EntryId entryId)
    {
        if (path.Condition is null)
        {
            return PathOccupantSelectionResult.Resolved(entryId);
        }

        var row = standing.Find(entryId);
        return row is null
            ? PathOccupantSelectionResult.Indeterminate()
            : path.Condition.IsSatisfiedBy(row)
                ? PathOccupantSelectionResult.Resolved(entryId)
                : PathOccupantSelectionResult.Skipped();
    }
}
