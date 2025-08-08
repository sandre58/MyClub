// -----------------------------------------------------------------------
// <copyright file="IPerson.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Shared.Domain.Persons;

/// <summary>
/// Interface representing a person in the sports management system.
/// Defines the contract for person entities (players, staff, coaches, etc.) across different modules,
/// providing essential personal information and behavior for comparison and similarity checks.
/// </summary>
public interface IPerson : ISimilar<IPerson>, IComparable<IPerson>
{
    /// <summary>
    /// Gets the first name of the person.
    /// </summary>
    string FirstName { get; }

    /// <summary>
    /// Gets the last name (family name) of the person.
    /// </summary>
    string LastName { get; }

    /// <summary>
    /// Gets the gender of the person.
    /// This is typically used for categorization in sports contexts (men's/women's teams, etc.).
    /// </summary>
    GenderType Gender { get; }

    /// <summary>
    /// Gets the person's photo as a byte array.
    /// This can be null if no photo has been assigned to the person.
    /// </summary>
    byte[]? Photo { get; }

    /// <summary>
    /// Gets the country that this person is from or represents.
    /// This can be null for persons that don't have a specific country association.
    /// </summary>
    Country? Country { get; }
}
