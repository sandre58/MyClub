// -----------------------------------------------------------------------
// <copyright file="SeedingRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Regulation parameters for seeding presence (count only).
/// Not an Entry→Seed map — concrete seed assignments belong to a Draw instance (SeedMap), not here.
/// Distinct from Standing.Position / ranking.
/// </summary>
public sealed record SeedingRules
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeedingRules"/> class.
    /// </summary>
    /// <param name="numberOfSeeds">Number of seeds (≥ 0).</param>
    public SeedingRules(int numberOfSeeds)
    {
        if (numberOfSeeds < 0)
        {
            throw new DomainException(
                "Number of seeds cannot be negative.",
                RulesErrorCodes.DrawRulesInvalid);
        }

        NumberOfSeeds = numberOfSeeds;
    }

    /// <summary>
    /// Gets the number of seeds.
    /// </summary>
    public int NumberOfSeeds { get; }
}
