// -----------------------------------------------------------------------
// <copyright file="PenaltyShootoutPolicy.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Penalty shootout parameters when present on <see cref="MatchRules"/>.
/// Presence means a shootout is enabled; there is no <c>Enabled</c> flag.
/// </summary>
public sealed record PenaltyShootoutPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PenaltyShootoutPolicy"/> class.
    /// </summary>
    /// <param name="initialKicksPerTeam">Initial kicks per team before sudden death (≥ 1).</param>
    public PenaltyShootoutPolicy(int initialKicksPerTeam)
    {
        if (initialKicksPerTeam < 1)
        {
            throw new DomainException(
                "Initial kicks per team must be at least 1.",
                RulesErrorCodes.PenaltyShootoutPolicyInvalid);
        }

        InitialKicksPerTeam = initialKicksPerTeam;
    }

    /// <summary>
    /// Gets the number of initial kicks per team before sudden death.
    /// </summary>
    public int InitialKicksPerTeam { get; }
}
