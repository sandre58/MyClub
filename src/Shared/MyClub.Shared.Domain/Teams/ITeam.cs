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

public interface ITeam : ISimilar<ITeam>, IComparable<ITeam>
{
    DisplayName DisplayName { get; }

    byte[]? Logo { get; }

    Country? Country { get; }

    string? HomeColor { get; }

    string? AwayColor { get; }

    StadiumId? StadiumId { get; }
}
