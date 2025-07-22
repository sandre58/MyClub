// -----------------------------------------------------------------------
// <copyright file="IPerson.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Shared.Domain.Persons;

public interface IPerson : ISimilar<IPerson>, IComparable<IPerson>
{
    string FirstName { get; }

    string LastName { get; }

    GenderType Gender { get; }

    byte[]? Photo { get; }

    Country? Country { get; }
}
