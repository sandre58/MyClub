// -----------------------------------------------------------------------
// <copyright file="PotRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Describes a pot-based draw organization (count only; no team-to-pot mapping).
/// </summary>
public sealed record PotRules
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PotRules"/> class.
    /// </summary>
    /// <param name="numberOfPots">Number of pots (≥ 2).</param>
    public PotRules(int numberOfPots)
    {
        if (numberOfPots < 2)
        {
            throw new DomainException(
                "Number of pots must be at least 2.",
                RulesErrorCodes.DrawRulesInvalid);
        }

        NumberOfPots = numberOfPots;
    }

    /// <summary>
    /// Gets the number of pots.
    /// </summary>
    public int NumberOfPots { get; }
}
