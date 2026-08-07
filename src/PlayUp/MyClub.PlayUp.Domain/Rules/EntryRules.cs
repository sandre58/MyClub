// -----------------------------------------------------------------------
// <copyright file="EntryRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Registration constraints for a competition (minimum and maximum teams).
/// </summary>
public sealed record EntryRules
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntryRules"/> class.
    /// </summary>
    /// <param name="minimumTeams">Minimum number of teams (≥ 1).</param>
    /// <param name="maximumTeams">Maximum number of teams (≥ <paramref name="minimumTeams"/>).</param>
    public EntryRules(int minimumTeams, int maximumTeams)
    {
        if (minimumTeams < 1)
        {
            throw new DomainException(
                "Minimum teams must be at least 1.",
                RulesErrorCodes.EntryRulesInvalid);
        }

        if (maximumTeams < minimumTeams)
        {
            throw new DomainException(
                "Maximum teams must be greater than or equal to minimum teams.",
                RulesErrorCodes.EntryRulesInvalid);
        }

        MinimumTeams = minimumTeams;
        MaximumTeams = maximumTeams;
    }

    /// <summary>
    /// Gets the minimum number of teams.
    /// </summary>
    public int MinimumTeams { get; }

    /// <summary>
    /// Gets the maximum number of teams.
    /// </summary>
    public int MaximumTeams { get; }
}
