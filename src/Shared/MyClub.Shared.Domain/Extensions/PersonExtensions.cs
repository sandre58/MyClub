// -----------------------------------------------------------------------
// <copyright file="PersonExtensions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Domain.Persons;

namespace MyClub.Shared.Domain.Extensions;

public static class PersonExtensions
{
    public static string GetInverseName(this IPerson person) => string.Join(" ", person.LastName, person.FirstName);

    public static string GetFullName(this IPerson person) => string.Join(" ", person.FirstName, person.LastName);
}
