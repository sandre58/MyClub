// -----------------------------------------------------------------------
// <copyright file="HeritableRegulationPart.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Sub-VO grain that can follow Competition defaults on a Stage (<c>DefaultsBinding</c>).
/// </summary>
public enum HeritableRegulationPart
{
    /// <summary>MatchRules.Duration.</summary>
    MatchDuration = 0,

    /// <summary>MatchRules.ExtraTimePolicy (presence + values).</summary>
    ExtraTime = 1,

    /// <summary>MatchRules.PenaltyShootoutPolicy (presence + values).</summary>
    PenaltyShootout = 2,

    /// <summary>MatchRules.AdministrativeResultPolicy.</summary>
    AdministrativeResult = 3,

    /// <summary>StandingRules.Points.</summary>
    Points = 4,

    /// <summary>StandingRules.RankingCriteria.</summary>
    RankingCriteria = 5,
}
