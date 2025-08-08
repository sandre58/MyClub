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

/// <summary>
/// Interface representing a stadium in the sports management system.
/// Defines the contract for stadium entities across different modules, providing
/// essential stadium information and behavior for comparison and similarity checks.
/// </summary>
public interface IStadium : ISimilar<IStadium>, IComparable<IStadium>
{
    /// <summary>
    /// Gets the display name of the stadium, including both full name and short name.
    /// This is used for various display purposes throughout the application.
    /// </summary>
    DisplayName DisplayName { get; }

    /// <summary>
    /// Gets the type of playing surface or ground at this stadium.
    /// This indicates whether the stadium has grass, artificial grass, sand, or is an indoor facility.
    /// </summary>
    Ground Ground { get; }

    /// <summary>
    /// Gets the physical address of the stadium.
    /// This can be null if the address information is not available or has not been set.
    /// </summary>
    Address? Address { get; }
}
