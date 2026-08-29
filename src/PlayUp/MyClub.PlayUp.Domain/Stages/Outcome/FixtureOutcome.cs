// -----------------------------------------------------------------------
// <copyright file="FixtureOutcome.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Decided winner and loser of a fixture confrontation (single-leg or multi-leg).
/// </summary>
/// <remarks>
/// Derived from <see cref="FixtureOutcomeResolver"/> using <see cref="Rules.TieFormat"/> and
/// confrontation legs (EntryId referential). Does not carry a <see cref="FixtureId"/>.
/// The fixture identity is supplied by Application (and checked by
/// <see cref="Progression.ProgressionApplier"/> / <see cref="Placement.PlacementAwardApplier"/>)
/// from the surrounding orchestration context.
/// Undecided draws throw <c>Stage.FixtureOutcomeUndecided</c> — this type has no resolution status.
/// </remarks>
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
