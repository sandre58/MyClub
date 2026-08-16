// -----------------------------------------------------------------------
// <copyright file="CreateCompetition.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: create a new Competition (Draft) from a display name.
/// </summary>
/// <remarks>
/// Domain owns identity, name, regulation copy, and <see cref="CompetitionStatus.Draft"/>.
/// Persistence: caller invokes this use case, <c>ICompetitionRepository.Add</c>, then SaveChanges once.
/// Regulation defaults to <see cref="BootstrapRegulation.Standard"/> until Configure (Slice 2).
/// </remarks>
public static class CreateCompetition
{
    /// <summary>
    /// Creates a Competition with bootstrap regulation.
    /// </summary>
    /// <param name="name">Display name (validated by Domain <see cref="CompetitionName"/>).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>The new Competition aggregate (not yet persisted).</returns>
    public static Competition Execute(string name, IClock clock) =>
        Execute(name, BootstrapRegulation.Standard(), clock);

    /// <summary>
    /// Creates a Competition with an explicit regulation (tests / future Configure).
    /// </summary>
    /// <param name="name">Display name.</param>
    /// <param name="regulation">Regulation to copy onto the Competition.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>The new Competition aggregate (not yet persisted).</returns>
    public static Competition Execute(string name, Regulation regulation, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(regulation);
        ArgumentNullException.ThrowIfNull(clock);

        return Competition.Create(new CompetitionName(name), regulation, clock);
    }
}
