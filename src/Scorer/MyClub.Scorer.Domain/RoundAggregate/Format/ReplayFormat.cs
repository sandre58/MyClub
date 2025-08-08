// -----------------------------------------------------------------------
// <copyright file="ReplayFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;

namespace MyClub.Scorer.Domain.RoundAggregate.Format;

/// <summary>
/// Represents a replay round format where teams play a single match, and if it ends in a draw,
/// a replay match is scheduled at the other team's venue to determine the winner.
/// This traditional format maintains single-match efficiency while ensuring competitive resolution.
/// </summary>
/// <param name="RegulationTime">The period format for regular playing time in each match.</param>
/// <param name="ExtraTime">The period format for extra time periods. Null if no extra time is used.</param>
/// <param name="NumberOfPenaltyShootouts">The number of penalty shootout attempts per team. Null if shootouts are not used.</param>
public record ReplayFormat(PeriodFormat RegulationTime, PeriodFormat? ExtraTime = null, int? NumberOfPenaltyShootouts = null) : RoundFormat(RegulationTime, ExtraTime, NumberOfPenaltyShootouts)
{
    /// <summary>
    /// Gets the round format type, which is always <see cref="RoundFormatType.Replay"/> for this implementation.
    /// </summary>
    public override RoundFormatType Type => RoundFormatType.Replay;

    /// <summary>
    /// Determines whether this round format allows matches to end in a draw.
    /// </summary>
    /// <returns>
    /// Always returns true because the replay format specifically accommodates draws
    /// by scheduling replay matches when the initial match ends without a winner.
    /// </returns>
    public override bool AllowDraw() => true;
}
