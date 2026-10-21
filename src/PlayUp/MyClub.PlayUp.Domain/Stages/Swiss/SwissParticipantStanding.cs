// -----------------------------------------------------------------------
// <copyright file="SwissParticipantStanding.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Explicit standing snapshot input for <see cref="SwissPairingEngine"/> (no repository).
/// </summary>
public sealed record SwissParticipantStanding
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SwissParticipantStanding"/> class.
    /// </summary>
    /// <param name="entryId">Participant identity.</param>
    /// <param name="points">Current points.</param>
    /// <param name="position">1-based ranking position (tie-break already applied).</param>
    public SwissParticipantStanding(EntryId entryId, int points, int position)
    {
        if (position < 1)
        {
            throw new DomainException(
                "Swiss standing position must be at least 1.",
                StageErrorCodes.SwissPairingInvalid);
        }

        EntryId = entryId;
        Points = points;
        Position = position;
    }

    /// <summary>
    /// Gets the participant identity.
    /// </summary>
    public EntryId EntryId { get; }

    /// <summary>
    /// Gets current points.
    /// </summary>
    public int Points { get; }

    /// <summary>
    /// Gets the 1-based ranking position.
    /// </summary>
    public int Position { get; }
}
