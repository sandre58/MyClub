// -----------------------------------------------------------------------
// <copyright file="StageRegulation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Stage regulation value object: materialized phase rules (independent from <see cref="Regulation"/>).
/// V1 active families: match and standing rules. Draw / qualification / tie-format arrive in later phases.
/// </summary>
public sealed record StageRegulation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageRegulation"/> class.
    /// </summary>
    /// <param name="matchRules">How an individual match is played in this stage.</param>
    /// <param name="standingRules">How standings are calculated in this stage.</param>
    public StageRegulation(MatchRules matchRules, StandingRules standingRules)
    {
        ArgumentNullException.ThrowIfNull(matchRules);
        ArgumentNullException.ThrowIfNull(standingRules);

        MatchRules = matchRules;
        StandingRules = standingRules;
    }

    /// <summary>
    /// Gets the match rules.
    /// </summary>
    public MatchRules MatchRules { get; }

    /// <summary>
    /// Gets the standing rules.
    /// </summary>
    public StandingRules StandingRules { get; }

    /// <summary>
    /// Materializes an independent stage regulation from a competition regulation.
    /// Copies match and standing rules by value (new instances); does not copy entry rules.
    /// </summary>
    /// <param name="competitionRegulation">The source competition regulation.</param>
    /// <returns>A new stage regulation with no shared nested references.</returns>
    public static StageRegulation MaterializeFrom(Regulation competitionRegulation)
    {
        ArgumentNullException.ThrowIfNull(competitionRegulation);

        return new StageRegulation(
            CloneMatchRules(competitionRegulation.MatchRules),
            CloneStandingRules(competitionRegulation.StandingRules));
    }

    /// <summary>
    /// Returns a deep copy of this regulation (new nested value-object instances).
    /// </summary>
    /// <returns>An independent copy.</returns>
    public StageRegulation Copy() =>
        new(CloneMatchRules(MatchRules), CloneStandingRules(StandingRules));

    /// <summary>
    /// Returns a copy with replaced standing rules (new nested instances for standing only).
    /// </summary>
    /// <param name="standingRules">The new standing rules.</param>
    /// <returns>A new stage regulation.</returns>
    public StageRegulation WithStandingRules(StandingRules standingRules)
    {
        ArgumentNullException.ThrowIfNull(standingRules);
        return new StageRegulation(CloneMatchRules(MatchRules), CloneStandingRules(standingRules));
    }

    private static MatchRules CloneMatchRules(MatchRules source)
    {
        var duration = new MatchDuration(
            source.Duration.DurationPerPeriod,
            source.Duration.NumberOfPeriods,
            source.Duration.HalfTimeDuration);
        var administrative = new AdministrativeResultPolicy(
            source.AdministrativeResultPolicy.ForfeitWinnerGoals,
            source.AdministrativeResultPolicy.ForfeitLoserGoals);
        ExtraTimePolicy? extraTime = source.ExtraTimePolicy is { } et
            ? new ExtraTimePolicy(et.DurationPerPeriod, et.NumberOfPeriods)
            : null;
        PenaltyShootoutPolicy? shootout = source.PenaltyShootoutPolicy is { } ps
            ? new PenaltyShootoutPolicy(ps.InitialKicksPerTeam)
            : null;

        return new MatchRules(duration, administrative, extraTime, shootout);
    }

    private static StandingRules CloneStandingRules(StandingRules source) =>
        new(
            new PointsPolicy(source.Points.WinPoints, source.Points.DrawPoints, source.Points.LossPoints),
            [..source.RankingCriteria]);
}
