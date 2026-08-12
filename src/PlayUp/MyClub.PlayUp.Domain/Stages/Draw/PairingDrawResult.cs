// -----------------------------------------------------------------------
// <copyright file="PairingDrawResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Entry ↔ Entry opposition proposal from a Draw (does not create Match; no Home/Away).
/// Ubiquitous language: opposition proposal — not a generic Assignment.
/// </summary>
public sealed record PairingDrawResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PairingDrawResult"/> class.
    /// </summary>
    /// <param name="entryA">First entry identity.</param>
    /// <param name="entryB">Second entry identity.</param>
    public PairingDrawResult(EntryId entryA, EntryId entryB)
    {
        if (entryA.Equals(entryB))
        {
            throw new DomainException(
                "A pairing cannot contain the same entry twice.",
                StageErrorCodes.DrawInputsInvalid);
        }

        EntryA = entryA;
        EntryB = entryB;
    }

    /// <summary>
    /// Gets the first entry identity.
    /// </summary>
    public EntryId EntryA { get; }

    /// <summary>
    /// Gets the second entry identity.
    /// </summary>
    public EntryId EntryB { get; }
}
