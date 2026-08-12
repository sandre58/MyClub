// -----------------------------------------------------------------------
// <copyright file="CompetitionStarted.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions.Events;

/// <summary>
/// Raised when a competition starts (status becomes Running).
/// </summary>
public sealed record CompetitionStarted : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionStarted"/> class.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public CompetitionStarted(CompetitionId competitionId, IClock clock)
        : base(clock) => CompetitionId = competitionId;

    /// <summary>
    /// Gets the competition identity.
    /// </summary>
    public CompetitionId CompetitionId { get; }
}
