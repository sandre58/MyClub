// -----------------------------------------------------------------------
// <copyright file="ProgressionApplier.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Domain.Progression;

/// <summary>
/// Pure progression helper: maps a path configuration and fixture outcome to an occupant instruction.
/// </summary>
public static class ProgressionApplier
{
    /// <summary>
    /// Applies a single progression path to a decided fixture outcome.
    /// </summary>
    /// <param name="path">Declarative progression path (source fixture, outcome, destination).</param>
    /// <param name="fixtureId">Fixture identity supplied by Application (must match <see cref="ProgressionPath.SourceFixtureId"/>).</param>
    /// <param name="outcome">Decided winner/loser of the confrontation.</param>
    /// <returns>Slot assignment instruction (no Stage mutation).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="outcome"/> is <see langword="null"/>.</exception>
    /// <exception cref="DomainException"><paramref name="fixtureId"/> does not match the path source fixture.</exception>
    public static SlotAssignmentInstruction Apply(ProgressionPath path, FixtureId fixtureId, FixtureOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(outcome);

        if (!path.SourceFixtureId.Equals(fixtureId))
        {
            throw new DomainException(
                "Progression path source fixture does not match the supplied fixture identity.",
                StageErrorCodes.ProgressionApplyFixtureMismatch);
        }

        var entryId = path.Outcome == ProgressionOutcome.Winner
            ? outcome.WinnerEntryId
            : outcome.LoserEntryId;

        return new SlotAssignmentInstruction(
            path.Destination.StageId,
            path.Destination.SlotKey,
            entryId);
    }
}
