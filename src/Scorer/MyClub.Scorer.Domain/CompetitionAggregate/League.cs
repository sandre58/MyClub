// -----------------------------------------------------------------------
// <copyright file="League.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Domain.Primitives;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Kernel.Results;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.CompetitionAggregate;

/// <summary>
/// Represents a league competition where teams play in a round-robin format with standings and matchdays.
/// A league is characterized by a regular season where all teams play against each other,
/// and their performance is tracked through a standings table with points, goal difference, and other statistics.
/// </summary>
public sealed class League : Competition, IChampionship
{
    private readonly List<MatchdayId> _matchdays = [];
    private readonly Dictionary<TeamId, int> _penaltyPoints = [];

    [UsedImplicitly(Reason = "Used by EF Core.")]
    private League()
    {
        StandingRules = null!;
        Labels = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="League"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the league.</param>
    /// <param name="name">The full name of the league.</param>
    /// <param name="shortName">The short name or abbreviation of the league.</param>
    /// <param name="format">The default match format configuration for this league.</param>
    /// <param name="rules">The default match rules configuration for this league.</param>
    /// <param name="standingRules">The rules used to calculate and order the standings table.</param>
    /// <param name="labels">The labels used to categorize teams in the standings (e.g., Champion, Relegated).</param>
    private League(CompetitionId id, string name, string? shortName, MatchFormat format, MatchRules rules, StandingRuleSet standingRules, StandingLabels labels)
        : base(id, name, shortName, format, rules)
    {
        StandingRules = standingRules;
        Labels = labels;
    }

    /// <summary>
    /// Creates a new league with the specified parameters.
    /// </summary>
    /// <param name="name">The full name of the league.</param>
    /// <param name="shortName">The short name or abbreviation of the league.</param>
    /// <param name="format">The default match format configuration for this league. Uses default if not specified.</param>
    /// <param name="rules">The default match rules configuration for this league. Uses default if not specified.</param>
    /// <param name="standingRules">The rules used to calculate standings. Uses default if not specified.</param>
    /// <param name="standingRankStatuses">The labels for different standing positions. Uses empty collection if not specified.</param>
    /// <returns>A new <see cref="League"/> instance.</returns>
    public static League Create(string name, string? shortName = null, MatchFormat? format = null, MatchRules? rules = null, StandingRuleSet? standingRules = null, StandingLabels? standingRankStatuses = null)
        => new(CompetitionId.New(), name, shortName, format ?? MatchFormat.Default, rules ?? MatchRules.Default, standingRules ?? StandingRuleSet.Default, standingRankStatuses ?? []);

    /// <summary>
    /// Gets the read-only collection of matchday identifiers that belong to this league.
    /// Matchdays represent the game weeks or rounds in the league schedule.
    /// </summary>
    public IReadOnlyCollection<MatchdayId> Matchdays => _matchdays.AsReadOnly();

    /// <summary>
    /// Gets or sets the rules used to calculate and order the standings table.
    /// This includes points awarded for wins/draws/losses, tiebreaker criteria, and other ranking rules.
    /// </summary>
    public StandingRuleSet StandingRules { get; set; }

    /// <summary>
    /// Gets or sets the labels used to categorize teams in the standings table.
    /// Examples include "Champion", "European Competition", "Relegation Zone", etc.
    /// </summary>
    public StandingLabels Labels { get; set; }

    /// <summary>
    /// Gets the read-only dictionary of penalty points applied to teams.
    /// Penalty points are typically deducted from a team's total for rule violations or administrative infractions.
    /// </summary>
    public IReadOnlyDictionary<TeamId, int> PenaltyPoints => _penaltyPoints.AsReadOnly();

    /// <summary>
    /// Gets the type of competition, which is always <see cref="CompetitionType.League"/> for this class.
    /// </summary>
    public override CompetitionType Type => CompetitionType.League;

    #region Matchdays

    /// <summary>
    /// Adds a matchday to the league schedule.
    /// </summary>
    /// <param name="matchdayId">The identifier of the matchday to add.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the added matchday ID if successful,
    /// or a failure result if the matchday already exists in the league.
    /// </returns>
    public Result<MatchdayId> AddMatchday(MatchdayId matchdayId)
    {
        if (_matchdays.Contains(matchdayId))
            return Failures.AlreadyExists<MatchdayId>(matchdayId.ToString());

        _matchdays.Add(matchdayId);

        return Result.Success(matchdayId);
    }

    /// <summary>
    /// Removes a matchday from the league schedule.
    /// </summary>
    /// <param name="matchdayId">The identifier of the matchday to remove.</param>
    /// <returns>True if the matchday was successfully removed; false if the matchday was not found.</returns>
    public bool RemoveMatchday(MatchdayId matchdayId) => _matchdays.Remove(matchdayId);

    /// <summary>
    /// Clears all matchdays from the league schedule.
    /// This operation removes all game weeks from the league.
    /// </summary>
    public void Clear() => _matchdays.Clear();

    #endregion

    #region Penalty

    /// <summary>
    /// Adds or updates penalty points for a team.
    /// Penalty points are typically deducted from a team's total points in the standings.
    /// </summary>
    /// <param name="teamId">The identifier of the team to apply penalty points to.</param>
    /// <param name="points">The number of penalty points to apply (typically negative).</param>
    public void AddPenalty(TeamId teamId, int points) => _ = _penaltyPoints.AddOrUpdate(teamId, points);

    /// <summary>
    /// Removes penalty points for a team.
    /// </summary>
    /// <param name="team">The identifier of the team to remove penalty points from.</param>
    /// <returns>True if penalty points were successfully removed; false if the team had no penalty points.</returns>
    public bool RemovePenalty(TeamId team) => _penaltyPoints.Remove(team);

    /// <summary>
    /// Clears all penalty points from all teams in the league.
    /// This operation removes all administrative penalties.
    /// </summary>
    public void ClearPenaltyPoints() => _penaltyPoints.Clear();

    #endregion
}
