// -----------------------------------------------------------------------
// <copyright file="CompetitionSuspended.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competition.Events;

/// <summary>
/// Raised when a competition is suspended.
/// </summary>
public sealed record CompetitionSuspended : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionSuspended"/> class.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public CompetitionSuspended(CompetitionId competitionId, IClock clock)
        : base(clock) => CompetitionId = competitionId;

    /// <summary>
    /// Gets the competition identity.
    /// </summary>
    public CompetitionId CompetitionId { get; }
}
