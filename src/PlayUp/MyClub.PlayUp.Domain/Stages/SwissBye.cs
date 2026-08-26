// -----------------------------------------------------------------------
// <copyright file="SwissBye.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Recorded Swiss bye for one round — pairing event, not a Fixture/Match.
/// </summary>
/// <remarks>
/// <see cref="RoundIndex"/> aligns with Matchday number (1-based Swiss round).
/// </remarks>
public sealed record SwissBye
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SwissBye"/> class.
    /// </summary>
    /// <param name="roundIndex">1-based Swiss round index (Matchday number).</param>
    /// <param name="entryId">Entry that receives the bye.</param>
    public SwissBye(int roundIndex, EntryId entryId)
    {
        if (roundIndex < 1)
        {
            throw new DomainException(
                "Swiss bye round index must be at least 1.",
                StageErrorCodes.SwissByeInvalid);
        }

        RoundIndex = roundIndex;
        EntryId = entryId;
    }

    /// <summary>
    /// Gets the 1-based Swiss round index.
    /// </summary>
    public int RoundIndex { get; }

    /// <summary>
    /// Gets the entry that received the bye.
    /// </summary>
    public EntryId EntryId { get; }
}
