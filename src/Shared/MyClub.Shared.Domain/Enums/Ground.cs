// -----------------------------------------------------------------------
// <copyright file="Ground.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Shared.Domain.Enums;

/// <summary>
/// Enumeration representing the type of playing surface or ground at a stadium.
/// This is used to categorize stadiums based on their field conditions and playing characteristics.
/// </summary>
public enum Ground
{
    /// <summary>
    /// Natural grass playing surface.
    /// This is the traditional playing surface for most outdoor sports.
    /// </summary>
    Grass,

    /// <summary>
    /// Artificial grass or synthetic turf playing surface.
    /// Modern synthetic surfaces that simulate natural grass but provide more durability and weather resistance.
    /// </summary>
    ArtificialGrass,

    /// <summary>
    /// Sand playing surface.
    /// Typically used for beach sports or specialized training facilities.
    /// </summary>
    Sand,

    /// <summary>
    /// Indoor playing surface.
    /// Typically refers to indoor courts or facilities with specialized flooring for indoor sports.
    /// </summary>
    Indoor
}
