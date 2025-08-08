// -----------------------------------------------------------------------
// <copyright file="TeamId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Shared.Domain.Teams;

/// <summary>
/// Strongly-typed identifier for team entities.
/// Provides type safety and prevents accidental mixing of team identifiers with other entity identifiers.
/// Also includes conversion methods to and from team references for polymorphic team handling.
/// </summary>
/// <param name="Value">The underlying GUID value for this team identifier.</param>
public sealed record TeamId(Guid Value) : EntityId<TeamId>(Value)
{
    /// <summary>
    /// Implicitly converts a TeamId to a ConcreteTeamReference.
    /// This allows teams to be referenced polymorphically in collections that may contain both concrete and virtual teams.
    /// </summary>
    /// <param name="id">The team identifier to convert.</param>
    /// <returns>A ConcreteTeamReference wrapping the team identifier.</returns>
    public static implicit operator ConcreteTeamReference(TeamId id) => ToConcreteTeamReference(id);

    /// <summary>
    /// Implicitly converts a ConcreteTeamReference back to a TeamId.
    /// This allows extraction of the underlying team identifier from a concrete team reference.
    /// </summary>
    /// <param name="value">The concrete team reference to convert.</param>
    /// <returns>The underlying team identifier.</returns>
    public static implicit operator TeamId(ConcreteTeamReference value) => ToTeamId(value);

    /// <summary>
    /// Converts a TeamId to a ConcreteTeamReference.
    /// </summary>
    /// <param name="id">The team identifier to convert.</param>
    /// <returns>A ConcreteTeamReference wrapping the team identifier.</returns>
    public static ConcreteTeamReference ToConcreteTeamReference(TeamId id) => new(id);

    /// <summary>
    /// Extracts the TeamId from a ConcreteTeamReference.
    /// </summary>
    /// <param name="value">The concrete team reference containing the team identifier.</param>
    /// <returns>The underlying team identifier.</returns>
    public static TeamId ToTeamId(ConcreteTeamReference value) => value.Id;

    /// <summary>
    /// Converts this TeamId to a polymorphic TeamReference.
    /// This is useful when you need to store team references that could be either concrete teams or virtual teams.
    /// </summary>
    /// <returns>A TeamReference that represents this concrete team.</returns>
    public TeamReference ToReference() => ToConcreteTeamReference(this);
}
