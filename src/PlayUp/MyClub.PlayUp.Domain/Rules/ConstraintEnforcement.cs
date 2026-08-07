// -----------------------------------------------------------------------
// <copyright file="ConstraintEnforcement.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// How strictly a draw constraint must be respected by a future generator.
/// </summary>
public enum ConstraintEnforcement
{
    /// <summary>
    /// The generator should try to respect the constraint but may deviate.
    /// Default V1 value.
    /// </summary>
    Preferred = 0,

    /// <summary>
    /// An invalid draw that violates the constraint is forbidden.
    /// </summary>
    Required = 1
}
