// -----------------------------------------------------------------------
// <copyright file="GroupStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Kernel.Results;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.StageAggregate;

/// <summary>
/// Represents a group stage in a tournament where teams are divided into multiple groups
/// that compete in round-robin format within each group. This stage type is commonly used
/// in major international tournaments as a qualification phase before knockout rounds.
/// </summary>
public sealed class GroupStage : Stage
{
    private readonly List<MatchdayId> _matchdays = [];
    private readonly List<Group> _groups = [];
    private readonly Dictionary<TeamId, int> _penaltyPoints = [];

    [UsedImplicitly(Reason = "Used by EF Core.")]
    private GroupStage()
    {
        StandingRules = null!;
        Labels = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupStage"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the group stage.</param>
    /// <param name="ancestorStageId">The identifier of the ancestor stage. Can be null for initial stages.</param>
    /// <param name="name">The name of the group stage.</param>
    /// <param name="shortName">The short name of the group stage.</param>
    /// <param name="format">The match format configuration for this stage.</param>
    /// <param name="rules">The match rules configuration for this stage.</param>
    /// <param name="standingRules">The standing calculation rules for group rankings.</param>
    /// <param name="labels">The labels for different standing positions.</param>
    /// <param name="isConsolation">Whether this is a consolation stage.</param>
    private GroupStage(StageId id, StageId? ancestorStageId, string name, string? shortName, MatchFormat format, MatchRules rules, StandingRuleSet standingRules, StandingLabels labels, bool isConsolation)
        : base(id, ancestorStageId, name, shortName, format, rules, isConsolation)
    {
        StandingRules = standingRules;
        Labels = labels;
    }

    /// <summary>
    /// Creates a new group stage with the specified configuration.
    /// </summary>
    /// <param name="ancestorStageId">The identifier of the ancestor stage. Can be null for initial stages.</param>
    /// <param name="name">The name of the group stage.</param>
    /// <param name="shortName">The short name of the group stage. Will be auto-generated if not provided.</param>
    /// <param name="format">The match format. Uses default if not specified.</param>
    /// <param name="rules">The match rules. Uses default if not specified.</param>
    /// <param name="standingRules">The standing rules. Uses default if not specified.</param>
    /// <param name="standingRankStatuses">The standing labels. Uses empty collection if not specified.</param>
    /// <param name="isConsolation">Whether this is a consolation stage. Defaults to false.</param>
    /// <returns>A new <see cref="GroupStage"/> instance.</returns>
    public static GroupStage Create(StageId? ancestorStageId, string name, string? shortName = null, MatchFormat? format = null, MatchRules? rules = null, StandingRuleSet? standingRules = null, StandingLabels? standingRankStatuses = null, bool isConsolation = false)
        => new(StageId.New(), ancestorStageId, name, shortName, format ?? MatchFormat.Default, rules ?? MatchRules.Default, standingRules ?? StandingRuleSet.Default, standingRankStatuses ?? [], isConsolation);

    /// <summary>
    /// Gets the read-only collection of matchday identifiers associated with this group stage.
    /// Matchdays coordinate the scheduling of group matches across multiple rounds.
    /// </summary>
    public IReadOnlyCollection<MatchdayId> Matchdays => _matchdays.AsReadOnly();

    /// <summary>
    /// Gets the read-only collection of groups within this stage.
    /// Each group contains a subset of teams competing in round-robin format.
    /// </summary>
    public IReadOnlyCollection<Group> Groups => _groups.AsReadOnly();

    /// <summary>
    /// Gets the read-only dictionary of penalty points assigned to teams.
    /// Penalty points are applied for disciplinary reasons and affect final standings.
    /// </summary>
    public IReadOnlyDictionary<TeamId, int> PenaltyPoints => _penaltyPoints.AsReadOnly();

    /// <summary>
    /// Gets or sets the standing rules that determine how teams are ranked within their groups.
    /// These rules define points for wins/draws/losses and tiebreaker criteria.
    /// </summary>
    public StandingRuleSet StandingRules { get; set; }

    /// <summary>
    /// Gets or sets the labels that define what each position in group standings means.
    /// Examples include "Qualified", "Eliminated", "Play-off", etc.
    /// </summary>
    public StandingLabels Labels { get; set; }

    /// <summary>
    /// Gets the stage type, which is always <see cref="StageType.Groups"/> for group stages.
    /// </summary>
    public override StageType Type => StageType.Groups;

    #region Groups

    /// <summary>
    /// Creates and adds a new group to this stage with the specified teams.
    /// </summary>
    /// <param name="teams">The teams to be assigned to the new group.</param>
    /// <param name="name">The name of the group (e.g., "Group A").</param>
    /// <param name="shortName">The short name of the group (e.g., "A").</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the created group if successful,
    /// or a failure result if a group with the same name already exists.
    /// </returns>
    public Result<Group> AddGroup(IEnumerable<TeamReference> teams, string name, string? shortName)
    {
        if (_groups.Any(x => x.DisplayName == name))
            return Failures.AlreadyExists<Group>(name);

        var group = Group.Create(this, teams, name, shortName);
        _groups.Add(group);

        return Result.Success(group);
    }

    /// <summary>
    /// Removes a group from this stage.
    /// </summary>
    /// <param name="group">The group to remove.</param>
    /// <returns>True if the group was successfully removed; false if the group was not found.</returns>
    public bool RemoveGroup(Group group) => _groups.Remove(group);

    /// <summary>
    /// Removes all groups from this stage.
    /// </summary>
    public void ClearGroups() => _groups.Clear();

    #endregion

    #region Matchdays

    /// <summary>
    /// Adds a matchday to this group stage.
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
    /// Removes a matchday from this group stage.
    /// </summary>
    /// <param name="matchdayId">The identifier of the matchday to remove.</param>
    /// <returns>True if the matchday was successfully removed; false if the matchday was not found.</returns>
    public bool RemoveMatchday(MatchdayId matchdayId) => _matchdays.Remove(matchdayId);

    /// <summary>
    /// Removes all matchdays from this group stage.
    /// </summary>
    public void ClearMatchdays() => _matchdays.Clear();

    #endregion

    #region Penalty

    /// <summary>
    /// Adds or updates penalty points for a team.
    /// Penalty points are deducted from the team's total points in standings calculations.
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
    /// Removes all penalty points from all teams in this stage.
    /// </summary>
    public void ClearPenaltyPoints() => _penaltyPoints.Clear();

    #endregion
}
