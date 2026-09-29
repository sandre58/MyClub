// -----------------------------------------------------------------------
// <copyright file="Regulation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Competition regulation value object: entry, match, standing defaults, and disciplinary rules.
/// Immutable; replace as a whole on change.
/// </summary>
/// <remarks>
/// <see cref="StandingRules"/> on Competition are <strong>standing defaults</strong>
/// for classifying stages — not a competition-wide standing consumed at runtime.
/// </remarks>
public sealed record Regulation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Regulation"/> class.
    /// </summary>
    /// <param name="entryRules">Registration constraints.</param>
    /// <param name="matchRules">How an individual match is played.</param>
    /// <param name="standingRules">How standings are calculated.</param>
    /// <param name="disciplinaryRules">
    /// Disciplinary types allowed for the competition (AllowedTypes catalogue only).
    /// When omitted, defaults to <see cref="DisciplinaryRules.None"/> (no events authorized).
    /// </param>
    public Regulation(
        EntryRules entryRules,
        MatchRules matchRules,
        StandingRules standingRules,
        DisciplinaryRules? disciplinaryRules = null)
    {
        ArgumentNullException.ThrowIfNull(entryRules);
        ArgumentNullException.ThrowIfNull(matchRules);
        ArgumentNullException.ThrowIfNull(standingRules);

        EntryRules = entryRules;
        MatchRules = matchRules;
        StandingRules = standingRules;
        DisciplinaryRules = disciplinaryRules ?? DisciplinaryRules.None;
    }

    /// <summary>
    /// Gets the entry rules.
    /// </summary>
    public EntryRules EntryRules { get; }

    /// <summary>
    /// Gets the match rules.
    /// </summary>
    public MatchRules MatchRules { get; }

    /// <summary>
    /// Gets the standing rules (standing defaults for classifying stages).
    /// Not consumed by runtime standing calculation; stages carry their own copy when classifying.
    /// </summary>
    public StandingRules StandingRules { get; }

    /// <summary>
    /// Gets the disciplinary rules (AllowedTypes catalogue only; no consequences).
    /// </summary>
    public DisciplinaryRules DisciplinaryRules { get; }

    /// <summary>
    /// Returns an independent copy (new nested value-object instances).
    /// </summary>
    /// <returns>A deep copy of this regulation.</returns>
    public Regulation Copy() =>
        new(
            new EntryRules(EntryRules.MinimumTeams, EntryRules.MaximumTeams),
            CloneMatchRules(MatchRules),
            CloneStandingRules(StandingRules),
            CloneDisciplinaryRules(DisciplinaryRules));

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

    private static StandingRules CloneStandingRules(StandingRules source) =>
        new(
            new PointsPolicy(source.Points.WinPoints, source.Points.DrawPoints, source.Points.LossPoints),
            [..source.RankingCriteria]);

    private static DisciplinaryRules CloneDisciplinaryRules(DisciplinaryRules source) =>
        new([..source.AllowedTypes]);
}
