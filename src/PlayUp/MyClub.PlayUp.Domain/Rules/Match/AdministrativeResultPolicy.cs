// -----------------------------------------------------------------------
// <copyright file="AdministrativeResultPolicy.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Default administrative score applied for forfeit, abandon, and similar organizer results.
/// </summary>
public sealed record AdministrativeResultPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdministrativeResultPolicy"/> class.
    /// </summary>
    /// <param name="forfeitWinnerGoals">Goals awarded to the winning side (≥ 0).</param>
    /// <param name="forfeitLoserGoals">Goals awarded to the losing side (≥ 0).</param>
    public AdministrativeResultPolicy(int forfeitWinnerGoals, int forfeitLoserGoals)
    {
        if (forfeitWinnerGoals < 0 || forfeitLoserGoals < 0)
        {
            throw new DomainException(
                "Administrative forfeit goals cannot be negative.",
                RulesErrorCodes.AdministrativeResultPolicyInvalid);
        }

        ForfeitWinnerGoals = forfeitWinnerGoals;
        ForfeitLoserGoals = forfeitLoserGoals;
    }

    /// <summary>
    /// Gets the goals awarded to the winning side on forfeit (and related admin defaults).
    /// </summary>
    public int ForfeitWinnerGoals { get; }

    /// <summary>
    /// Gets the goals awarded to the losing side on forfeit (and related admin defaults).
    /// </summary>
    public int ForfeitLoserGoals { get; }
}
