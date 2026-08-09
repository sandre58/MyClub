// -----------------------------------------------------------------------
// <copyright file="FixtureOutcome.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Decided winner and loser of a fixture confrontation (single-leg V1).
/// </summary>
public sealed record FixtureOutcome
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureOutcome"/> class.
    /// </summary>
    /// <param name="winnerEntryId">Winning entry.</param>
    /// <param name="loserEntryId">Losing entry.</param>
    public FixtureOutcome(EntryId winnerEntryId, EntryId loserEntryId)
    {
        WinnerEntryId = winnerEntryId;
        LoserEntryId = loserEntryId;
    }

    /// <summary>
    /// Gets the winner entry identity.
    /// </summary>
    public EntryId WinnerEntryId { get; }

    /// <summary>
    /// Gets the loser entry identity.
    /// </summary>
    public EntryId LoserEntryId { get; }
}
