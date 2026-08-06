// -----------------------------------------------------------------------
// <copyright file="CompetitionCreated.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competition.Events;

/// <summary>
/// Raised when a competition is created.
/// </summary>
public sealed record CompetitionCreated : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionCreated"/> class.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="name">The competition name.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public CompetitionCreated(CompetitionId competitionId, string name, IClock clock)
        : base(clock)
    {
        CompetitionId = competitionId;
        Name = name;
    }

    /// <summary>
    /// Gets the competition identity.
    /// </summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>
    /// Gets the competition name.
    /// </summary>
    public string Name { get; }
}
