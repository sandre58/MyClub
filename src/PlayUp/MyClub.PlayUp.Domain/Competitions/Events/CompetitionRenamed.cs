// -----------------------------------------------------------------------
// <copyright file="CompetitionRenamed.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions.Events;

/// <summary>
/// Raised when a competition is renamed.
/// </summary>
public sealed record CompetitionRenamed : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionRenamed"/> class.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="name">The new competition name.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public CompetitionRenamed(CompetitionId competitionId, string name, IClock clock)
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
    /// Gets the new competition name.
    /// </summary>
    public string Name { get; }
}
