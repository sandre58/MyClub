// -----------------------------------------------------------------------
// <copyright file="ITeamsContainer.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Scorer.Domain.Primitives;

/// <summary>
/// Defines a contract for entities that contain and manage a collection of teams.
/// This interface provides a common abstraction for different types of competitions,
/// stages, rounds, and other domain objects that organize teams.
/// </summary>
public interface ITeamsContainer
{
    /// <summary>
    /// Gets the read-only collection of team references contained in this entity.
    /// Team references can be either concrete teams or virtual placeholders that will be resolved
    /// based on the results of other matches or competitions.
    /// </summary>
    /// <value>
    /// A read-only collection of <see cref="TeamReference"/> objects representing the teams
    /// or team placeholders managed by this container.
    /// </value>
    IReadOnlyCollection<TeamReference> Teams { get; }
}
