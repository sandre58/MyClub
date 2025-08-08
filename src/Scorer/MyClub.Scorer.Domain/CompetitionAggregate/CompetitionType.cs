// -----------------------------------------------------------------------
// <copyright file="CompetitionType.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Scorer.Domain.CompetitionAggregate;

/// <summary>
/// Defines the types of competitions supported in the Scorer domain.
/// Each type represents a different format of sporting competition with specific characteristics and rules.
/// </summary>
public enum CompetitionType
{
    /// <summary>
    /// Represents a league competition format.
    /// Leagues are characterized by round-robin play where all teams play against each other,
    /// with standings calculated based on points, goal difference, and other tiebreaker criteria.
    /// </summary>
    League,

    /// <summary>
    /// Represents a cup competition format.
    /// Cups use knockout elimination where teams progress through rounds,
    /// with losers being eliminated until only one winner remains.
    /// </summary>
    Cup,

    /// <summary>
    /// Represents a tournament competition format.
    /// Tournaments combine multiple phases such as group stages followed by knockout rounds,
    /// allowing for complex competition structures with various progression rules.
    /// </summary>
    Tournament
}
