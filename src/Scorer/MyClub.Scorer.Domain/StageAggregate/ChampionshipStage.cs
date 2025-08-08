// -----------------------------------------------------------------------
// <copyright file="ChampionshipStage.cs" company="Stéphane ANDRE">
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

namespace MyClub.Scorer.Domain.StageAggregate;

/// <summary>
/// Represents a championship stage in a tournament where all teams compete in a single league format.
/// This stage type implements a complete round-robin or league-style competition where teams
/// are ranked in a unified standings table to determine the overall champion.
/// </summary>
public sealed class ChampionshipStage : Stage, IChampionship
{
    private readonly List<MatchdayId> _matchdays = [];
    private readonly Dictionary<TeamId, int> _penaltyPoints = [];

    [UsedImplicitly(Reason = "Used by EF Core.")]
    private ChampionshipStage()
    {
        StandingRules = null!;
        Labels = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChampionshipStage"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the championship stage.</param>
    /// <param name="ancestorStageId">The identifier of the ancestor stage. Can be null for initial stages.</param>
    /// <param name="name">The name of the championship stage.</param>
    /// <param name="shortName">The short name of the championship stage.</param>
    /// <param name="format">The match format configuration for this stage.</param>
    /// <param name="rules">The match rules configuration for this stage.</param>
    /// <param name="standingRules">The standing calculation rules for championship rankings.</param>
    /// <param name="labels">The labels for different standing positions.</param>
    /// <param name="isConsolation">Whether this is a consolation championship.</param>
    private ChampionshipStage(StageId id, StageId? ancestorStageId, string name, string? shortName, MatchFormat format, MatchRules rules, StandingRuleSet standingRules, StandingLabels labels, bool isConsolation)
        : base(id, ancestorStageId, name, shortName, format, rules, isConsolation)
    {
        StandingRules = standingRules;
        Labels = labels;
    }

    /// <summary>
    /// Creates a new championship stage with the specified configuration.
    /// </summary>
    /// <param name="ancestorStageId">The identifier of the ancestor stage. Can be null for initial stages.</param>
    /// <param name="name">The name of the championship stage.</param>
    /// <param name="shortName">The short name of the championship stage. Will be auto-generated if not provided.</param>
    /// <param name="format">The match format. Uses default if not specified.</param>
    /// <param name="rules">The match rules. Uses default if not specified.</param>
    /// <param name="standingRules">The standing rules. Uses default if not specified.</param>
    /// <param name="standingRankStatuses">The standing labels. Uses empty collection if not specified.</param>
    /// <param name="isConsolation">Whether this is a consolation championship. Defaults to false.</param>
    /// <returns>A new <see cref="ChampionshipStage"/> instance.</returns>
    public static ChampionshipStage Create(StageId? ancestorStageId, string name, string? shortName = null, MatchFormat? format = null, MatchRules? rules = null, StandingRuleSet? standingRules = null, StandingLabels? standingRankStatuses = null, bool isConsolation = false)
        => new(StageId.New(), ancestorStageId, name, shortName, format ?? MatchFormat.Default, rules ?? MatchRules.Default, standingRules ?? StandingRuleSet.Default, standingRankStatuses ?? [], isConsolation);

    /// <summary>
    /// Gets the read-only collection of matchday identifiers associated with this championship stage.
    /// Matchdays organize the scheduling of league matches across multiple rounds.
    /// </summary>
    public IReadOnlyCollection<MatchdayId> Matchdays => _matchdays.AsReadOnly();

    /// <summary>
    /// Gets or sets the standing rules that determine how teams are ranked in the championship.
    /// These rules define points for wins/draws/losses and comprehensive tiebreaker criteria.
    /// </summary>
    public StandingRuleSet StandingRules { get; set; }

    /// <summary>
    /// Gets or sets the labels that define what each position in championship standings means.
    /// Examples include "Champion", "Runner-up", "Third Place", etc.
    /// </summary>
    public StandingLabels Labels { get; set; }

    /// <summary>
    /// Gets the read-only dictionary of penalty points assigned to teams.
    /// Penalty points are deducted from teams' total points for disciplinary reasons.
    /// </summary>
    public IReadOnlyDictionary<TeamId, int> PenaltyPoints => _penaltyPoints.AsReadOnly();

    /// <summary>
    /// Gets the stage type, which is always <see cref="StageType.Championship"/> for championship stages.
    /// </summary>
    public override StageType Type => StageType.Championship;

    #region Matchdays

    /// <summary>
    /// Adds a matchday to this championship stage.
    /// </summary>
    /// <param name="matchdayId">The identifier of the matchday to add.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the added matchday ID if successful.
    /// </returns>
    public Result<MatchdayId> AddMatchday(MatchdayId matchdayId)
    {
        _matchdays.Add(matchdayId);

        return Result.Success(matchdayId);
    }

    /// <summary>
    /// Removes a matchday from this championship stage.
    /// </summary>
    /// <param name="matchdayId">The identifier of the matchday to remove.</param>
    /// <returns>True if the matchday was successfully removed; false if the matchday was not found.</returns>
    public bool RemoveMatchday(MatchdayId matchdayId) => _matchdays.Remove(matchdayId);

    /// <summary>
    /// Removes all matchdays from this championship stage.
    /// </summary>
    public void Clear() => _matchdays.Clear();

    #endregion

    #region Penalty

    /// <summary>
    /// Adds or updates penalty points for a team.
    /// Penalty points are deducted from the team's total points in championship standings.
    /// </summary>
    /// <param name="teamId">The identifier of the team to penalize.</param>
    /// <param name="points">The number of penalty points to assign.</param>
    public void AddPenalty(TeamId teamId, int points) => _ = _penaltyPoints.AddOrUpdate(teamId, points);

    /// <summary>
    /// Removes penalty points for a team.
    /// </summary>
    /// <param name="team">The identifier of the team to remove penalties from.</param>
    /// <returns>True if penalty points were successfully removed; false if the team had no penalties.</returns>
    public bool RemovePenalty(TeamId team) => _penaltyPoints.Remove(team);

    /// <summary>
    /// Removes all penalty points from all teams in this championship.
    /// </summary>
    public void ClearPenaltyPoints() => _penaltyPoints.Clear();

    #endregion
}
