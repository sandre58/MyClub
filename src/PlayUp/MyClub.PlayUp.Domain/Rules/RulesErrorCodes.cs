// -----------------------------------------------------------------------
// <copyright file="RulesErrorCodes.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Stable machine-readable codes for Rules value-object invariant violations.
/// </summary>
public static class RulesErrorCodes
{
    /// <summary>
    /// Gets the code when entry team bounds are invalid.
    /// </summary>
    public const string EntryRulesInvalid = "Rules.EntryRulesInvalid";

    /// <summary>
    /// Gets the code when match duration parameters are invalid.
    /// </summary>
    public const string MatchDurationInvalid = "Rules.MatchDurationInvalid";

    /// <summary>
    /// Gets the code when an extra-time policy is invalid.
    /// </summary>
    public const string ExtraTimePolicyInvalid = "Rules.ExtraTimePolicyInvalid";

    /// <summary>
    /// Gets the code when a penalty shootout policy is invalid.
    /// </summary>
    public const string PenaltyShootoutPolicyInvalid = "Rules.PenaltyShootoutPolicyInvalid";

    /// <summary>
    /// Gets the code when an administrative result policy is invalid.
    /// </summary>
    public const string AdministrativeResultPolicyInvalid = "Rules.AdministrativeResultPolicyInvalid";

    /// <summary>
    /// Gets the code when standing ranking criteria are invalid.
    /// </summary>
    public const string RankingCriteriaInvalid = "Rules.RankingCriteriaInvalid";

    /// <summary>
    /// Gets the code when a tie format is invalid.
    /// </summary>
    public const string TieFormatInvalid = "Rules.TieFormatInvalid";

    /// <summary>
    /// Gets the code when draw rules are invalid.
    /// </summary>
    public const string DrawRulesInvalid = "Rules.DrawRulesInvalid";

    /// <summary>
    /// Gets the code when qualification rules are invalid.
    /// </summary>
    public const string QualificationRulesInvalid = "Rules.QualificationRulesInvalid";
}
