// -----------------------------------------------------------------------
// <copyright file="CompetitionCompleted.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions.Events;

/// <summary>
/// Raised when a competition is completed.
/// </summary>
public sealed record CompetitionCompleted : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionCompleted"/> class.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="mode">How the competition was completed.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public CompetitionCompleted(CompetitionId competitionId, CompletionMode mode, IClock clock)
        : base(clock)
    {
        CompetitionId = competitionId;
        Mode = mode;
    }

    /// <summary>
    /// Gets the competition identity.
    /// </summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>
    /// Gets how the competition was completed.
    /// </summary>
    public CompletionMode Mode { get; }
}
