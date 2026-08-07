// -----------------------------------------------------------------------
// <copyright file="CompetitionRegulationReplaced.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competition.Events;

/// <summary>
/// Raised when a competition regulation is replaced.
/// </summary>
public sealed record CompetitionRegulationReplaced : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionRegulationReplaced"/> class.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public CompetitionRegulationReplaced(CompetitionId competitionId, IClock clock)
        : base(clock) => CompetitionId = competitionId;

    /// <summary>
    /// Gets the competition identity.
    /// </summary>
    public CompetitionId CompetitionId { get; }
}
