// -----------------------------------------------------------------------
// <copyright file="ITeam.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Domain.ValueObjects;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Shared.Domain.Teams;

/// <summary>
/// Interface representing a team in the sports management system.
/// Defines the contract for team entities across different modules, providing
/// essential team information and behavior for comparison and similarity checks.
/// </summary>
public interface ITeam : ISimilar<ITeam>, IComparable<ITeam>
{
    /// <summary>
    /// Gets the display name of the team, including both full name and short name.
    /// This is used for various display purposes throughout the application.
    /// </summary>
    DisplayName DisplayName { get; }

    /// <summary>
    /// Gets the team's logo as a byte array.
    /// This can be null if no logo has been assigned to the team.
    /// </summary>
    byte[]? Logo { get; }

    /// <summary>
    /// Gets the country that this team represents or is based in.
    /// This can be null for teams that don't have a specific country association.
    /// </summary>
    Country? Country { get; }

    /// <summary>
    /// Gets the primary home color of the team (typically for jerseys/uniforms).
    /// This is usually represented as a hex color code or color name.
    /// </summary>
    string? HomeColor { get; }

    /// <summary>
    /// Gets the away color of the team (typically for away jerseys/uniforms).
    /// This is usually represented as a hex color code or color name.
    /// </summary>
    string? AwayColor { get; }

    /// <summary>
    /// Gets the identifier of the team's home stadium.
    /// This can be null if the team doesn't have a designated home stadium.
    /// </summary>
    StadiumId? StadiumId { get; }
}
