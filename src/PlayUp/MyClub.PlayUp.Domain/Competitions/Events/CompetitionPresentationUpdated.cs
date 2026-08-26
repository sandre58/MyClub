// -----------------------------------------------------------------------
// <copyright file="CompetitionPresentationUpdated.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions.Events;

/// <summary>
/// Raised when competition presentation metadata is updated.
/// </summary>
public sealed record CompetitionPresentationUpdated : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionPresentationUpdated"/> class.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="shortName">The short name, if any.</param>
    /// <param name="logoMediaId">The logo Media Guid, if any.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public CompetitionPresentationUpdated(
        CompetitionId competitionId,
        string? shortName,
        Guid? logoMediaId,
        IClock clock)
        : base(clock)
    {
        CompetitionId = competitionId;
        ShortName = shortName;
        LogoMediaId = logoMediaId;
    }

    /// <summary>
    /// Gets the competition identity.
    /// </summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>
    /// Gets the short name, if any.
    /// </summary>
    public string? ShortName { get; }

    /// <summary>
    /// Gets the logo Media Guid, if any.
    /// </summary>
    public Guid? LogoMediaId { get; }
}
