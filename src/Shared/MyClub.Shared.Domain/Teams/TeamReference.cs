// -----------------------------------------------------------------------
// <copyright file="TeamReference.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace MyClub.Shared.Domain.Teams;

/// <summary>
/// Abstract base class for polymorphic team references.
/// This allows the system to handle both concrete teams (with actual TeamIds) and virtual teams
/// (such as "Winner of Match A", "3rd place of Group B", etc.) in a unified way.
/// This is particularly useful in tournament and competition scenarios where teams may not be determined until later.
/// </summary>
[SuppressMessage("Major Code Smell", "S2094:Classes should not be empty", Justification = "Abstract base class for polymorphic team references - intentionally empty to allow different implementations (ConcreteTeamReference, VirtualTeamReference, etc.)")]
public abstract record TeamReference;

/// <summary>
/// Represents a reference to a concrete, existing team with a specific TeamId.
/// This is used when referring to teams that are already known and have been created in the system.
/// </summary>
/// <param name="Id">The identifier of the concrete team being referenced.</param>
public sealed record ConcreteTeamReference(TeamId Id) : TeamReference
{
    /// <summary>
    /// Implicitly converts a TeamId to a ConcreteTeamReference.
    /// This provides convenience when working with team identifiers in contexts that expect team references.
    /// </summary>
    /// <param name="id">The team identifier to convert.</param>
    /// <returns>A ConcreteTeamReference wrapping the team identifier.</returns>
    public static implicit operator ConcreteTeamReference(TeamId id) => ToConcreteTeamReference(id);

    /// <summary>
    /// Implicitly converts a ConcreteTeamReference back to a TeamId.
    /// This allows easy extraction of the underlying team identifier.
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
}
