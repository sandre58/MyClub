// -----------------------------------------------------------------------
// <copyright file="QualificationApplier.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stage;
using MyClub.PlayUp.Domain.Standing;

namespace MyClub.PlayUp.Domain.Qualification;

/// <summary>
/// Pure qualification helper: selects entries from a standing and maps a path to a slot instruction.
/// Does not mutate aggregates or calculate standings.
/// </summary>
public static class QualificationApplier
{
    /// <summary>
    /// Selects entries from a standing according to a selection rule.
    /// </summary>
    /// <param name="standing">Calculated standing view.</param>
    /// <param name="selection">Selection mode and bounds.</param>
    /// <returns>Ordered selected entry identities.</returns>
    public static IReadOnlyList<EntryId> SelectEntries(Standing.Standing standing, QualificationSelection selection)
    {
        ArgumentNullException.ThrowIfNull(standing);
        ArgumentNullException.ThrowIfNull(selection);

        var rows = standing.Rows;
        return rows.Count == 0
            ? []
            : selection.Mode switch
        {
            SelectionMode.Position => SelectPosition(rows, selection.Value),
            SelectionMode.Top => SelectTop(rows, selection.Value),
            SelectionMode.Bottom => SelectBottom(rows, selection.Value),
            SelectionMode.Range => SelectRange(rows, selection.Value, selection.EndValue!.Value),
            SelectionMode.Best or SelectionMode.Worst => throw new DomainException(
                $"Selection mode '{selection.Mode}' is not supported by QualificationApplier V1.",
                QualificationErrorCodes.SelectionNotSupported),
            _ => throw new DomainException(
                "Selection mode is unknown.",
                QualificationErrorCodes.SelectionNotSupported)
        };
    }

    /// <summary>
    /// Applies a qualification path to a standing, producing a single slot assignment instruction.
    /// </summary>
    /// <param name="path">Declarative qualification path.</param>
    /// <param name="standing">Calculated standing view.</param>
    /// <returns>One slot assignment instruction.</returns>
    /// <exception cref="DomainException">
    /// Selection is unsupported, unresolved, or yields more than one entry (V1: one path → one entry).
    /// </exception>
    public static SlotAssignmentInstruction Apply(QualificationPath path, Standing.Standing standing)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(standing);

        var selected = SelectEntries(standing, path.Selection);
        return selected.Count switch
        {
            0 => throw new DomainException("Qualification selection did not resolve an entry from the standing.",
                QualificationErrorCodes.SelectionUnresolved),
            > 1 => throw new DomainException(
                "Qualification path must resolve to exactly one entry in V1 (use one path per slot).",
                QualificationErrorCodes.PathMultiEntry),
            _ => new SlotAssignmentInstruction(path.Destination.StageId, path.Destination.SlotKey, selected[0])
        };
    }

    private static IReadOnlyList<EntryId> SelectPosition(IReadOnlyList<StandingRow> rows, int position)
    {
        var row = rows.FirstOrDefault(r => r.Position == position);
        return row is null ? [] : [row.EntryId];
    }

    private static EntryId[] SelectTop(IReadOnlyList<StandingRow> rows, int count) =>
        [..rows.Where(r => r.Position <= count).OrderBy(r => r.Position).Select(r => r.EntryId)];

    private static EntryId[] SelectBottom(IReadOnlyList<StandingRow> rows, int count)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var threshold = rows.Count - count + 1;
        return [..rows.Where(r => r.Position >= threshold).OrderBy(r => r.Position).Select(r => r.EntryId)];
    }

    private static EntryId[] SelectRange(IReadOnlyList<StandingRow> rows, int from, int to) =>
    [..rows.Where(r => r.Position >= from && r.Position <= to).OrderBy(r => r.Position).Select(r => r.EntryId)];
}
