// -----------------------------------------------------------------------
// <copyright file="SingleFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;

namespace MyClub.Scorer.Domain.RoundAggregate.Format;

/// <summary>
/// Represents a single-match elimination round format where teams play one decisive match
/// to determine which team advances to the next round.
/// This is the most common and straightforward knockout tournament format.
/// </summary>
/// <param name="RegulationTime">The period format for regular playing time in the match.</param>
/// <param name="ExtraTime">The period format for extra time periods. Null if no extra time is used.</param>
/// <param name="NumberOfPenaltyShootouts">The number of penalty shootout attempts per team. Null if shootouts are not used.</param>
public record SingleFormat(PeriodFormat RegulationTime, PeriodFormat? ExtraTime = null, int? NumberOfPenaltyShootouts = null) : RoundFormat(RegulationTime, ExtraTime, NumberOfPenaltyShootouts)
{
    /// <summary>
    /// Gets the round format type, which is always <see cref="RoundFormatType.Single"/> for this implementation.
    /// </summary>
    public override RoundFormatType Type => RoundFormatType.Single;

    /// <summary>
    /// Determines whether this round format allows matches to end in a draw.
    /// </summary>
    /// <returns>
    /// Always returns false because single-elimination format requires each match to produce a winner
    /// to maintain the knockout bracket progression.
    /// </returns>
    public override bool AllowDraw() => false;
}
