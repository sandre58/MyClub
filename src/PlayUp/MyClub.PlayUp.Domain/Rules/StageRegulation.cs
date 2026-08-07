// -----------------------------------------------------------------------
// <copyright file="StageRegulation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Stage regulation value object: materialized phase rules (independent from <see cref="Regulation"/>).
/// Active families: match, standing, optional draw / qualification rules and default <see cref="TieFormat"/>.
/// </summary>
public sealed record StageRegulation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageRegulation"/> class.
    /// </summary>
    /// <param name="matchRules">How an individual match is played in this stage.</param>
    /// <param name="standingRules">How standings are calculated in this stage.</param>
    /// <param name="tieFormat">Default tie format copied to rounds on add; <see langword="null"/> when absent.</param>
    /// <param name="drawRules">Draw parameters when the stage uses a draw; <see langword="null"/> when absent.</param>
    /// <param name="qualificationRules">Routing rules when participants leave this stage; <see langword="null"/> when absent.</param>
    public StageRegulation(
        MatchRules matchRules,
        StandingRules standingRules,
        TieFormat? tieFormat = null,
        DrawRules? drawRules = null,
        QualificationRules? qualificationRules = null)
    {
        ArgumentNullException.ThrowIfNull(matchRules);
        ArgumentNullException.ThrowIfNull(standingRules);

        MatchRules = matchRules;
        StandingRules = standingRules;
        TieFormat = tieFormat;
        DrawRules = drawRules;
        QualificationRules = qualificationRules;
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
    /// Gets the default tie format for rounds when present; otherwise <see langword="null"/>.
    /// </summary>
    public TieFormat? TieFormat { get; }

    /// <summary>
    /// Gets the draw rules when present; otherwise <see langword="null"/>.
    /// </summary>
    public DrawRules? DrawRules { get; }

    /// <summary>
    /// Gets the qualification rules when present; otherwise <see langword="null"/>.
    /// </summary>
    public QualificationRules? QualificationRules { get; }

    /// <summary>
    /// Materializes an independent stage regulation from a competition regulation.
    /// Copies match and standing rules by value; optional families default to <see langword="null"/>.
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
        new(
            CloneMatchRules(MatchRules),
            CloneStandingRules(StandingRules),
            TieFormat?.Copy(),
            DrawRules?.Copy(),
            QualificationRules?.Copy());

    /// <summary>
    /// Returns a copy with replaced standing rules (new nested instances for standing only).
    /// </summary>
    /// <param name="standingRules">The new standing rules.</param>
    /// <returns>A new stage regulation.</returns>
    public StageRegulation WithStandingRules(StandingRules standingRules)
    {
        ArgumentNullException.ThrowIfNull(standingRules);
        return new StageRegulation(
            CloneMatchRules(MatchRules),
            CloneStandingRules(standingRules),
            TieFormat?.Copy(),
            DrawRules?.Copy(),
            QualificationRules?.Copy());
    }

    /// <summary>
    /// Returns a copy with replaced default tie format.
    /// </summary>
    /// <param name="tieFormat">The new default tie format, or <see langword="null"/>.</param>
    /// <returns>A new stage regulation.</returns>
    public StageRegulation WithTieFormat(TieFormat? tieFormat) =>
        new(
            CloneMatchRules(MatchRules),
            CloneStandingRules(StandingRules),
            tieFormat?.Copy(),
            DrawRules?.Copy(),
            QualificationRules?.Copy());

    /// <summary>
    /// Returns a copy with replaced draw rules.
    /// </summary>
    /// <param name="drawRules">The new draw rules, or <see langword="null"/>.</param>
    /// <returns>A new stage regulation.</returns>
    public StageRegulation WithDrawRules(DrawRules? drawRules) =>
        new(
            CloneMatchRules(MatchRules),
            CloneStandingRules(StandingRules),
            TieFormat?.Copy(),
            drawRules?.Copy(),
            QualificationRules?.Copy());

    /// <summary>
    /// Returns a copy with replaced qualification rules.
    /// </summary>
    /// <param name="qualificationRules">The new qualification rules, or <see langword="null"/>.</param>
    /// <returns>A new stage regulation.</returns>
    public StageRegulation WithQualificationRules(QualificationRules? qualificationRules) =>
        new(
            CloneMatchRules(MatchRules),
            CloneStandingRules(StandingRules),
            TieFormat?.Copy(),
            DrawRules?.Copy(),
            qualificationRules?.Copy());

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
