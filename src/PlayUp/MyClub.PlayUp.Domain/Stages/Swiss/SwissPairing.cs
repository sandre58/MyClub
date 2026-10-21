// -----------------------------------------------------------------------
// <copyright file="SwissPairing.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// One Swiss round opposition with Home/Away already assigned (deterministic policy).
/// </summary>
public sealed record SwissPairing
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SwissPairing"/> class.
    /// </summary>
    /// <param name="homeEntryId">Home entry.</param>
    /// <param name="awayEntryId">Away entry.</param>
    public SwissPairing(EntryId homeEntryId, EntryId awayEntryId)
    {
        if (homeEntryId.Equals(awayEntryId))
        {
            throw new DomainException(
                "A Swiss pairing cannot contain the same entry twice.",
                StageErrorCodes.SwissPairingInvalid);
        }

        HomeEntryId = homeEntryId;
        AwayEntryId = awayEntryId;
    }

    /// <summary>
    /// Gets the home entry.
    /// </summary>
    public EntryId HomeEntryId { get; }

    /// <summary>
    /// Gets the away entry.
    /// </summary>
    public EntryId AwayEntryId { get; }
}
