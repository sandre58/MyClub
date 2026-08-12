// -----------------------------------------------------------------------
// <copyright file="CompetitionResumed.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions.Events;

/// <summary>
/// Raised when a suspended competition is resumed.
/// </summary>
public sealed record CompetitionResumed : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionResumed"/> class.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public CompetitionResumed(CompetitionId competitionId, IClock clock)
        : base(clock) => CompetitionId = competitionId;

    /// <summary>
    /// Gets the competition identity.
    /// </summary>
    public CompetitionId CompetitionId { get; }
}
