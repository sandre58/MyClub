// -----------------------------------------------------------------------
// <copyright file="RoundFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;

namespace MyClub.Scorer.Domain.RoundAggregate.Format;

/// <summary>
/// Abstract base class for defining different round formats in knockout competitions.
/// A round format determines how matches are organized and played within a tournament round,
/// including the number of matches, timing rules, and progression criteria.
/// </summary>
/// <param name="RegulationTime">The period format for regular playing time in each match.</param>
/// <param name="ExtraTime">The period format for extra time periods. Null if no extra time is used.</param>
/// <param name="NumberOfPenaltyShootouts">The number of penalty shootout attempts per team. Null if shootouts are not used.</param>
public abstract record RoundFormat(PeriodFormat RegulationTime, PeriodFormat? ExtraTime = null, int? NumberOfPenaltyShootouts = null)
{
    /// <summary>
    /// Gets the specific type of round format.
    /// This determines the concrete implementation and behavior of the round format.
    /// </summary>
    public abstract RoundFormatType Type { get; }

    /// <summary>
    /// Determines whether this round format allows matches to end in a draw.
    /// </summary>
    /// <returns>
    /// True if draws are allowed in this format; false if all matches must have a decisive result.
    /// </returns>
    public abstract bool AllowDraw();
}
