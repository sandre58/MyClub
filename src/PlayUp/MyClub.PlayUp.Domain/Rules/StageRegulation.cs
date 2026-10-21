// -----------------------------------------------------------------------
// <copyright file="StageRegulation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Stage regulation value object: materialized phase rules (independent from <see cref="Regulation"/>).
/// Active families: match, optional standing (A5 — required when the phase classifies), optional draw /
/// qualification / progression / placement-award rules and default <see cref="TieFormat"/>.
/// </summary>
/// <remarks>
/// <see cref="StandingRules"/> is nullable for the two valid A5 states only: classifying → non-null;
/// non-classifying (Cup/KO) → null. Null is not a free-form optional everywhere — topology invariants live on <c>Stage</c>.
/// </remarks>
public sealed record StageRegulation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageRegulation"/> class.
    /// </summary>
    /// <param name="matchRules">How an individual match is played in this stage.</param>
    /// <param name="standingRules">
    /// How standings are calculated when this stage classifies; <see langword="null"/> for non-classifying phases.
    /// </param>
    /// <param name="tieFormat">Default tie format copied to rounds on add; <see langword="null"/> when absent.</param>
    /// <param name="drawRules">Draw parameters when the stage uses a draw; <see langword="null"/> when absent.</param>
    /// <param name="qualificationRules">Ranking-based routing when participants leave this stage; <see langword="null"/> when absent.</param>
    /// <param name="progressionRules">Fixture/Tie outcome routing to slots; <see langword="null"/> when absent.</param>
    /// <param name="placementAwardRules">Fixture/Tie outcome awards of final ranks; <see langword="null"/> when absent.</param>
    public StageRegulation(
        MatchRules matchRules,
        StandingRules? standingRules = null,
        TieFormat? tieFormat = null,
        DrawRules? drawRules = null,
        QualificationRules? qualificationRules = null,
        ProgressionRules? progressionRules = null,
        PlacementAwardRules? placementAwardRules = null)
    {
        ArgumentNullException.ThrowIfNull(matchRules);

        MatchRules = matchRules;
        StandingRules = standingRules;
        TieFormat = tieFormat;
        DrawRules = drawRules;
        QualificationRules = qualificationRules;
        ProgressionRules = progressionRules;
        PlacementAwardRules = placementAwardRules;
    }

    /// <summary>
    /// Gets the match rules.
    /// </summary>
    public MatchRules MatchRules { get; }

    /// <summary>
    /// Gets the standing rules when this stage classifies; otherwise <see langword="null"/>.
    /// Canonical runtime signal for standings applicability (do not re-derive “classifying” in reads).
    /// </summary>
    public StandingRules? StandingRules { get; }

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
    /// Gets the progression rules when present; otherwise <see langword="null"/>.
    /// </summary>
    public ProgressionRules? ProgressionRules { get; }

    /// <summary>
    /// Gets the placement award rules when present; otherwise <see langword="null"/>.
    /// </summary>
    public PlacementAwardRules? PlacementAwardRules { get; }

    /// <summary>
    /// Materializes an independent stage regulation from a competition regulation.
    /// Copies match rules; seeds standing from competition defaults only when <paramref name="isClassifyingPhase"/> is <see langword="true"/>.
    /// </summary>
    /// <param name="competitionRegulation">The source competition regulation.</param>
    /// <param name="isClassifyingPhase">
    /// When <see langword="true"/>, copies competition standing defaults (A4 seed).
    /// When <see langword="false"/> (Cup/KO), standing remains absent.
    /// </param>
    /// <returns>A new stage regulation with no shared nested references.</returns>
    public static StageRegulation MaterializeFrom(Regulation competitionRegulation, bool isClassifyingPhase = true)
    {
        ArgumentNullException.ThrowIfNull(competitionRegulation);

        return new StageRegulation(
            CloneMatchRules(competitionRegulation.MatchRules),
            isClassifyingPhase ? CloneStandingRules(competitionRegulation.StandingRules) : null);
    }

    /// <summary>
    /// Returns a deep copy of this regulation (new nested value-object instances).
    /// </summary>
    /// <returns>An independent copy.</returns>
    public StageRegulation Copy() =>
        new(
            CloneMatchRules(MatchRules),
            CloneStandingRulesOrNull(StandingRules),
            TieFormat?.Copy(),
            DrawRules?.Copy(),
            QualificationRules?.Copy(),
            ProgressionRules?.Copy(),
            PlacementAwardRules?.Copy());

    /// <summary>
    /// Returns a copy with replaced match rules (new nested instances for match only).
    /// </summary>
    /// <param name="matchRules">The new match rules.</param>
    /// <returns>A new stage regulation.</returns>
    public StageRegulation WithMatchRules(MatchRules matchRules)
    {
        ArgumentNullException.ThrowIfNull(matchRules);
        return new StageRegulation(
            CloneMatchRules(matchRules),
            CloneStandingRulesOrNull(StandingRules),
            TieFormat?.Copy(),
            DrawRules?.Copy(),
            QualificationRules?.Copy(),
            ProgressionRules?.Copy(),
            PlacementAwardRules?.Copy());
    }

    /// <summary>
    /// Returns a copy with replaced standing rules (new nested instances for standing only).
    /// </summary>
    /// <param name="standingRules">The new standing rules, or <see langword="null"/> to clear.</param>
    /// <returns>A new stage regulation.</returns>
    public StageRegulation WithStandingRules(StandingRules? standingRules) =>
        new(
            CloneMatchRules(MatchRules),
            CloneStandingRulesOrNull(standingRules),
            TieFormat?.Copy(),
            DrawRules?.Copy(),
            QualificationRules?.Copy(),
            ProgressionRules?.Copy(),
            PlacementAwardRules?.Copy());

    /// <summary>
    /// Returns a copy with replaced default tie format.
    /// </summary>
    /// <param name="tieFormat">The new default tie format, or <see langword="null"/>.</param>
    /// <returns>A new stage regulation.</returns>
    public StageRegulation WithTieFormat(TieFormat? tieFormat) =>
        new(
            CloneMatchRules(MatchRules),
            CloneStandingRulesOrNull(StandingRules),
            tieFormat?.Copy(),
            DrawRules?.Copy(),
            QualificationRules?.Copy(),
            ProgressionRules?.Copy(),
            PlacementAwardRules?.Copy());

    /// <summary>
    /// Returns a copy with replaced draw rules.
    /// </summary>
    /// <param name="drawRules">The new draw rules, or <see langword="null"/>.</param>
    /// <returns>A new stage regulation.</returns>
    public StageRegulation WithDrawRules(DrawRules? drawRules) =>
        new(
            CloneMatchRules(MatchRules),
            CloneStandingRulesOrNull(StandingRules),
            TieFormat?.Copy(),
            drawRules?.Copy(),
            QualificationRules?.Copy(),
            ProgressionRules?.Copy(),
            PlacementAwardRules?.Copy());

    /// <summary>
    /// Returns a copy with replaced qualification rules.
    /// </summary>
    /// <param name="qualificationRules">The new qualification rules, or <see langword="null"/>.</param>
    /// <returns>A new stage regulation.</returns>
    public StageRegulation WithQualificationRules(QualificationRules? qualificationRules) =>
        new(
            CloneMatchRules(MatchRules),
            CloneStandingRulesOrNull(StandingRules),
            TieFormat?.Copy(),
            DrawRules?.Copy(),
            qualificationRules?.Copy(),
            ProgressionRules?.Copy(),
            PlacementAwardRules?.Copy());

    /// <summary>
    /// Returns a copy with replaced progression rules.
    /// </summary>
    /// <param name="progressionRules">The new progression rules, or <see langword="null"/>.</param>
    /// <returns>A new stage regulation.</returns>
    public StageRegulation WithProgressionRules(ProgressionRules? progressionRules) =>
        new(
            CloneMatchRules(MatchRules),
            CloneStandingRulesOrNull(StandingRules),
            TieFormat?.Copy(),
            DrawRules?.Copy(),
            QualificationRules?.Copy(),
            progressionRules?.Copy(),
            PlacementAwardRules?.Copy());

    /// <summary>
    /// Returns a copy with replaced placement award rules.
    /// </summary>
    /// <param name="placementAwardRules">The new placement award rules, or <see langword="null"/>.</param>
    /// <returns>A new stage regulation.</returns>
    public StageRegulation WithPlacementAwardRules(PlacementAwardRules? placementAwardRules) =>
        new(
            CloneMatchRules(MatchRules),
            CloneStandingRulesOrNull(StandingRules),
            TieFormat?.Copy(),
            DrawRules?.Copy(),
            QualificationRules?.Copy(),
            ProgressionRules?.Copy(),
            placementAwardRules?.Copy());

    private static MatchRules CloneMatchRules(MatchRules source)
    {
        var duration = new MatchDuration(
            source.Duration.DurationPerPeriod,
            source.Duration.NumberOfPeriods,
            source.Duration.HalfTimeDuration);
        var administrative = new AdministrativeResultPolicy(
            source.AdministrativeResultPolicy.ForfeitWinnerGoals,
            source.AdministrativeResultPolicy.ForfeitLoserGoals);
        var extraTime = source.ExtraTimePolicy is { } et
            ? new ExtraTimePolicy(et.DurationPerPeriod, et.NumberOfPeriods)
            : null;
        var shootout = source.PenaltyShootoutPolicy is { } ps
            ? new PenaltyShootoutPolicy(ps.InitialKicksPerTeam)
            : null;

        return new MatchRules(duration, administrative, extraTime, shootout);
    }

    private static StandingRules? CloneStandingRulesOrNull(StandingRules? source) =>
        source is null ? null : CloneStandingRules(source);

    private static StandingRules CloneStandingRules(StandingRules source) =>
        new(
            new PointsPolicy(source.Points.WinPoints, source.Points.DrawPoints, source.Points.LossPoints),
            [.. source.RankingCriteria]);
}
