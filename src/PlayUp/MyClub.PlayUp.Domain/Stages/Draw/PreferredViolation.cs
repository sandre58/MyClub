// -----------------------------------------------------------------------
// <copyright file="PreferredViolation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// One Preferred constraint violation on a pairing (soft cost unit = 1).
/// </summary>
public sealed record PreferredViolation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PreferredViolation"/> class.
    /// </summary>
    /// <param name="constraintType">Preferred constraint that was violated.</param>
    /// <param name="entryA">First entry of the violating pair.</param>
    /// <param name="entryB">Second entry of the violating pair.</param>
    public PreferredViolation(DrawConstraintType constraintType, EntryId entryA, EntryId entryB)
    {
        if (!Enum.IsDefined(constraintType))
        {
            throw new DomainException(
                "Preferred violation constraint type is unknown.",
                StageErrorCodes.DrawGenerationInvalid);
        }

        if (entryA.Equals(entryB))
        {
            throw new DomainException(
                "A preferred violation cannot reference the same entry twice.",
                StageErrorCodes.DrawGenerationInvalid);
        }

        ConstraintType = constraintType;
        EntryA = entryA;
        EntryB = entryB;
    }

    /// <summary>
    /// Gets the Preferred constraint type that was violated.
    /// </summary>
    public DrawConstraintType ConstraintType { get; }

    /// <summary>
    /// Gets the first entry of the violating pair.
    /// </summary>
    public EntryId EntryA { get; }

    /// <summary>
    /// Gets the second entry of the violating pair.
    /// </summary>
    public EntryId EntryB { get; }
}
