// -----------------------------------------------------------------------
// <copyright file="Standing.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Standing;

/// <summary>
/// Calculated standing view (never an entity; not persisted in V1).
/// </summary>
public sealed class Standing
{
    private readonly StandingRow[] _rows;

    /// <summary>
    /// Initializes a new instance of the <see cref="Standing"/> class.
    /// </summary>
    /// <param name="rows">Ordered rows (position ascending).</param>
    public Standing(IReadOnlyList<StandingRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        _rows = [..rows];
    }

    /// <summary>
    /// Gets the ordered standing rows.
    /// </summary>
    public IReadOnlyList<StandingRow> Rows => _rows;

    /// <summary>
    /// Finds the row for an entry, if present.
    /// </summary>
    /// <param name="entryId">Entry identity.</param>
    /// <returns>The row, or <see langword="null"/>.</returns>
    public StandingRow? Find(Common.EntryId entryId) =>
        _rows.FirstOrDefault(r => r.EntryId.Equals(entryId));

    /// <summary>
    /// Gets the entry at a 1-based position, if present.
    /// </summary>
    /// <param name="position">1-based position.</param>
    /// <returns>The entry identity, or <see langword="null"/>.</returns>
    public Common.EntryId? EntryAt(int position) =>
        _rows.FirstOrDefault(r => r.Position == position)?.EntryId;
}
