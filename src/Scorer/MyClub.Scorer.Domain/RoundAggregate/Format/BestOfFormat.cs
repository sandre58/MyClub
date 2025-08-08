// -----------------------------------------------------------------------
// <copyright file="BestOfFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;

namespace MyClub.Scorer.Domain.RoundAggregate.Format;

/// <summary>
/// Represents a best-of-X series round format where teams compete in multiple matches
/// until one team wins a predetermined number of games to advance.
/// This format is commonly used in playoff systems and provides the most comprehensive
/// test of team strength over multiple encounters.
/// </summary>
/// <param name="MaxGames">The maximum number of games that can be played in the series.</param>
/// <param name="InvertTeamsByStage">Array indicating whether to invert home/away teams for each stage in the series.</param>
/// <param name="RegulationTime">The period format for regular playing time in each match.</param>
/// <param name="ExtraTime">The period format for extra time periods. Null if no extra time is used.</param>
/// <param name="NumberOfPenaltyShootouts">The number of penalty shootout attempts per team. Null if shootouts are not used.</param>
public record BestOfFormat(int MaxGames, bool[] InvertTeamsByStage, PeriodFormat RegulationTime, PeriodFormat? ExtraTime = null, int? NumberOfPenaltyShootouts = null) : RoundFormat(RegulationTime, ExtraTime, NumberOfPenaltyShootouts)
{
    /// <summary>
    /// Gets the round format type, which is always <see cref="RoundFormatType.BestOf"/> for this implementation.
    /// </summary>
    public override RoundFormatType Type => RoundFormatType.BestOf;

    /// <summary>
    /// Determines whether this round format allows matches to end in a draw.
    /// </summary>
    /// <returns>
    /// Always returns false because best-of series require each individual match to have a winner
    /// to properly track series progress toward the required number of wins.
    /// </returns>
    public override bool AllowDraw() => false;
}
