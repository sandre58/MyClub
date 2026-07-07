// -----------------------------------------------------------------------
// <copyright file="TeamDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using MyClub.Shared.Domain.Stadiums;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Application.Competitions.Queries.GetTeams.Dtos;

/// <summary>
/// Data Transfer Object representing a Team with all details for queries.
/// <para>
/// Includes all key team properties and collections of players and staff.
/// Used for transferring team data in queries and API responses.
/// </para>
/// <remarks>
/// - <see cref="Players"/> contains the roster as <see cref="PlayerDto"/> objects.
/// - <see cref="Staff"/> contains the staff as <see cref="ManagerDto"/> objects.
/// - All properties are read-only and set at construction.
/// </remarks>
/// </summary>
public sealed class TeamDto
{
    /// <summary>
    /// Gets unique identifier of the team.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets full name of the team.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets abbreviated or short name of the team.
    /// </summary>
    public string? ShortName { get; init; }

    /// <summary>
    /// Gets logo of the team as a byte array (optional).
    /// </summary>
    public byte[]? Logo { get; init; }

    /// <summary>
    /// Gets country the team represents or is based in.
    /// </summary>
    public Country? Country { get; init; }

    /// <summary>
    /// Gets identifier of the team's home stadium (optional).
    /// </summary>
    public StadiumId? StadiumId { get; init; }

    /// <summary>
    /// Gets primary home color of the team (hex code or color name).
    /// </summary>
    public string? HomeColor { get; init; }

    /// <summary>
    /// Gets away color of the team (hex code or color name).
    /// </summary>
    public string? AwayColor { get; init; }

    /// <summary>
    /// Gets roster of players in the team.
    /// </summary>
    public IReadOnlyCollection<PlayerDto> Players { get; init; } = [];

    /// <summary>
    /// Gets staff members (managers, coaches) in the team.
    /// </summary>
    public IReadOnlyCollection<ManagerDto> Staff { get; init; } = [];
}
