// -----------------------------------------------------------------------
// <copyright file="QualificationApplier.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Domain.Qualification;

/// <summary>
/// Pure qualification helper: selects entries from a standing and maps a path to a population instruction.
/// Does not mutate aggregates or calculate standings.
/// Best/Worst are obsolete aliases of Top/Bottom (normalized in <see cref="QualificationSelection"/>).
/// Optional <see cref="QualificationPath.Condition"/> gates a single selected row (skip when false).
/// </summary>
public static class QualificationApplier
{
    /// <summary>
    /// Selects entries from a standing according to a selection rule.
    /// </summary>
    /// <param name="standing">Calculated standing view.</param>
    /// <param name="selection">Selection mode and bounds.</param>
    /// <returns>Ordered selected entry identities.</returns>
    public static IReadOnlyList<EntryId> SelectEntries(Standing standing, QualificationSelection selection)
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
            _ => throw new DomainException(
                "Selection mode is unknown.",
                QualificationErrorCodes.SelectionNotSupported)
        };
    }

    /// <summary>
    /// Applies a qualification path to a standing, producing a population instruction or a skip.
    /// </summary>
    /// <param name="path">Declarative qualification path.</param>
    /// <param name="standing">Calculated standing view.</param>
    /// <returns>
    /// A population instruction when resolved; <see langword="null"/> when a condition gate skips.
    /// </returns>
    /// <exception cref="DomainException">
    /// Selection is unsupported, unresolved (0 entries), or yields more than one entry.
    /// </exception>
    public static QualificationInstruction? Apply(QualificationPath path, Standing standing)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(standing);

        var selected = SelectEntries(standing, path.Selection);
        return selected.Count switch
        {
            0 => throw new DomainException(
                "Qualification selection did not resolve an entry from the standing.",
                QualificationErrorCodes.SelectionUnresolved),
            > 1 => throw new DomainException(
                "Qualification path must resolve to exactly one entry.",
                QualificationErrorCodes.PathMultiEntry),
            _ => ResolveInstruction(path, standing, selected[0])
        };
    }

    private static QualificationInstruction? ResolveInstruction(
        QualificationPath path,
        Standing standing,
        EntryId entryId)
    {
        if (path.Condition is null)
        {
            return new QualificationInstruction(path.Destination.StageId, entryId);
        }

        var row = standing.Find(entryId)
                  ?? throw new DomainException(
                      "Selected entry was not found in the standing.",
                      QualificationErrorCodes.SelectionUnresolved);

        return path.Condition.IsSatisfiedBy(row)
            ? new QualificationInstruction(path.Destination.StageId, entryId)
            : null;
    }

    private static IReadOnlyList<EntryId> SelectPosition(IReadOnlyList<StandingRow> rows, int position)
    {
        var row = rows.FirstOrDefault(r => r.Position == position);
        return row is null ? [] : [row.EntryId];
    }

    private static IReadOnlyList<EntryId> SelectTop(IReadOnlyList<StandingRow> rows, int count) =>
        [.. rows.OrderBy(r => r.Position).Take(count).Select(r => r.EntryId)];

    private static IReadOnlyList<EntryId> SelectBottom(IReadOnlyList<StandingRow> rows, int count) =>
        [.. rows.OrderByDescending(r => r.Position).Take(count).OrderBy(r => r.Position).Select(r => r.EntryId)];

    private static IReadOnlyList<EntryId> SelectRange(
        IReadOnlyList<StandingRow> rows,
        int from,
        int to) =>
        [.. rows.Where(r => r.Position >= from && r.Position <= to)
            .OrderBy(r => r.Position)
            .Select(r => r.EntryId)];
}
