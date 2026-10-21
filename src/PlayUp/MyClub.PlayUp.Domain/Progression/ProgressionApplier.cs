// -----------------------------------------------------------------------
// <copyright file="ProgressionApplier.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Domain.Progression;

/// <summary>
/// Pure progression helper: maps a path configuration and fixture outcome to an instruction
/// (population entry or slot placement).
/// </summary>
public static class ProgressionApplier
{
    /// <summary>
    /// Applies a single progression path to a decided fixture outcome.
    /// </summary>
    /// <param name="path">Declarative progression path (structural source key, outcome, destination).</param>
    /// <param name="sourcePairKey">
    /// Resolved structural key for the execution event (Cup = <c>Fixture.BracketPairKey</c>).
    /// Must match <see cref="ProgressionPath.SourcePairKey"/> — FixtureId is never Path identity.
    /// </param>
    /// <param name="outcome">Decided winner/loser of the confrontation.</param>
    /// <returns>Progression instruction (no Stage mutation).</returns>
    [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Global", Justification = "False positive")]
    public static ProgressionInstruction Apply(
        ProgressionPath path,
        string sourcePairKey,
        FixtureOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(outcome);

        var key = BracketPair.NormalizePairKey(sourcePairKey);
        if (!string.Equals(path.SourcePairKey, key, StringComparison.Ordinal))
        {
            throw new DomainException(
                "Progression path source pair key does not match the supplied confrontation key.",
                StageErrorCodes.ProgressionApplyFixtureMismatch);
        }

        var entryId = path.Outcome == ProgressionOutcome.Winner
            ? outcome.WinnerEntryId
            : outcome.LoserEntryId;

        return new ProgressionInstruction(
            path.Destination.StageId,
            entryId,
            path.Destination.SlotKey,
            path.Destination.GroupId,
            path.Destination.Form);
    }
}
