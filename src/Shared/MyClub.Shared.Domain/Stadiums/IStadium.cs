// -----------------------------------------------------------------------
// <copyright file="IStadium.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.ValueObjects;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Shared.Domain.Stadiums;

public interface IStadium : ISimilar<IStadium>, IComparable<IStadium>
{
    DisplayName DisplayName { get; }

    Ground Ground { get; }

    Address? Address { get; }
}
