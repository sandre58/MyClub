// -----------------------------------------------------------------------
// <copyright file="StageRoundTieFormatReplaced.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages.Events;

/// <summary>
/// Raised when a round's tie format is replaced.
/// </summary>
public sealed record StageRoundTieFormatReplaced : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageRoundTieFormatReplaced"/> class.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="roundId">The round identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public StageRoundTieFormatReplaced(StageId stageId, RoundId roundId, IClock clock)
        : base(clock)
    {
        StageId = stageId;
        RoundId = roundId;
    }

    /// <summary>
    /// Gets the stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the round identity.
    /// </summary>
    public RoundId RoundId { get; }
}
