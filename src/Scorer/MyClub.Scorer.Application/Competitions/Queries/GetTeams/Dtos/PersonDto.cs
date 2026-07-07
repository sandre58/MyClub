// -----------------------------------------------------------------------
// <copyright file="PersonDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Application.Competitions.Queries.GetTeams.Dtos;

/// <summary>
/// Data Transfer Object for a person (player, manager, etc.) in a team.
/// Encapsulates all relevant personal information for queries and API responses.
/// </summary>
public class PersonDto
{
    /// <summary>
    /// Gets unique identifier of the person.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets first name of the person.
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Gets last name (family name) of the person.
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Gets country of origin or representation.
    /// </summary>
    public Country? Country { get; init; }

    /// <summary>
    /// Gets photo of the person as a byte array (optional).
    /// </summary>
    public byte[]? Photo { get; init; }

    /// <summary>
    /// Gets gender of the person (male/female/other).
    /// </summary>
    public GenderType Gender { get; init; }

    /// <summary>
    /// Gets sports license or registration number (optional).
    /// </summary>
    public string? LicenseNumber { get; init; }

    /// <summary>
    /// Gets email address (optional).
    /// </summary>
    public string? Email { get; init; }
}

/// <summary>
/// Data Transfer Object for a manager (coach, staff) in a team.
/// Inherits all properties from <see cref="PersonDto"/>.
/// </summary>
public sealed class ManagerDto : PersonDto;

/// <summary>
/// Data Transfer Object for a player in a team.
/// Inherits all properties from <see cref="PersonDto"/>.
/// </summary>
public sealed class PlayerDto : PersonDto;
