// -----------------------------------------------------------------------
// <copyright file="HomeAndAwayFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;

namespace MyClub.Scorer.Domain.RoundAggregate.Format;

/// <summary>
/// Represents a home-and-away two-legged round format where teams play twice
/// (once at each team's venue) with the aggregate score determining the winner.
/// This format reduces the impact of home advantage and provides a more balanced assessment of team strength.
/// </summary>
/// <param name="RegulationTime">The period format for regular playing time in each match.</param>
/// <param name="ExtraTime">The period format for extra time periods. Null if no extra time is used.</param>
/// <param name="UseAwayGoals">Whether the away goals rule is applied as a tiebreaker.</param>
/// <param name="NumberOfPenaltyShootouts">The number of penalty shootout attempts per team. Null if shootouts are not used.</param>
public record HomeAndAwayFormat(PeriodFormat RegulationTime, PeriodFormat? ExtraTime = null, bool UseAwayGoals = false, int? NumberOfPenaltyShootouts = null) : RoundFormat(RegulationTime, ExtraTime, NumberOfPenaltyShootouts)
{
    /// <summary>
    /// Gets the round format type, which is always <see cref="RoundFormatType.HomeAndAway"/> for this implementation.
    /// </summary>
    public override RoundFormatType Type => RoundFormatType.HomeAndAway;

    /// <summary>
    /// Determines whether this round format allows individual matches to end in a draw.
    /// </summary>
    /// <returns>
    /// Always returns true because in home-and-away format, individual matches can end in draws
    /// since the aggregate score across both legs determines the overall winner.
    /// </returns>
    public override bool AllowDraw() => true;
}
