// -----------------------------------------------------------------------
// <copyright file="DrawMode.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// How pairings are generated for a draw (parameters only; no execution).
/// </summary>
public enum DrawMode
{
    /// <summary>
    /// Pairings are generated randomly (subject to constraints).
    /// </summary>
    Random = 0,

    /// <summary>
    /// Pairings follow a predefined scheme.
    /// </summary>
    Predefined = 1
}
