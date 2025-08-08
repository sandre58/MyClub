// -----------------------------------------------------------------------
// <copyright file="PersonExtensions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Domain.Persons;

namespace MyClub.Shared.Domain.Extensions;

/// <summary>
/// Extension methods for IPerson interface to provide common name formatting operations.
/// These methods help standardize how person names are displayed across the application.
/// </summary>
public static class PersonExtensions
{
    /// <summary>
    /// Gets the person's name in inverse format (Last Name, First Name).
    /// This format is commonly used for sorting and formal displays.
    /// </summary>
    /// <param name="person">The person to get the inverse name for.</param>
    /// <returns>A string in the format "LastName FirstName".</returns>
    public static string GetInverseName(this IPerson person) => string.Join(" ", person.LastName, person.FirstName);

    /// <summary>
    /// Gets the person's name in full format (First Name Last Name).
    /// This format is commonly used for casual displays and user interfaces.
    /// </summary>
    /// <param name="person">The person to get the full name for.</param>
    /// <returns>A string in the format "FirstName LastName".</returns>
    public static string GetFullName(this IPerson person) => string.Join(" ", person.FirstName, person.LastName);
}
