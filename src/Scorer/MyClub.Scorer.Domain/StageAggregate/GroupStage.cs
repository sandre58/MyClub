// -----------------------------------------------------------------------
// <copyright file="GroupStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Kernel.Results;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.StageAggregate;

public class GroupStage : Stage
{
    private readonly List<MatchdayId> _matchdays = [];
    private readonly List<GroupId> _groups = [];
    private readonly Dictionary<TeamId, int> _penaltyPoints = [];

    // <remarks>Used by EF Core</remarks>
    private GroupStage()
        : base()
    {
        StandingRules = null!;
        StandingRankStatuses = null!;
    }

    private GroupStage(StageId id, StageId? ancestorStageId, string name, string? shortName, MatchFormat? format, MatchRules? rules, StandingRuleSet standingRules, StandingRankStatuses standingRankStatuses, bool isConsolation)
        : base(id, ancestorStageId, name, shortName, format, rules, isConsolation)
    {
        StandingRules = standingRules;
        StandingRankStatuses = standingRankStatuses;
    }

    public static GroupStage Create(StageId? ancestorStageId, string name, string? shortName = null, MatchFormat? format = null, MatchRules? rules = null, StandingRuleSet? standingRules = null, StandingRankStatuses? standingRankStatuses = null, bool isConsolation = false)
        => new(StageId.New(), ancestorStageId, name, shortName, format, rules, standingRules ?? StandingRuleSet.Default, standingRankStatuses ?? [], isConsolation);

    public IReadOnlyCollection<MatchdayId> Matchdays => _matchdays.AsReadOnly();

    public IReadOnlyCollection<GroupId> Groups => _groups.AsReadOnly();

    public StandingRuleSet StandingRules { get; set; }

    public StandingRankStatuses StandingRankStatuses { get; set; }

    public override StageType Type => StageType.Groups;

    #region Groups

    public Result<GroupId> AddGroup(GroupId groupId)
    {
        _groups.Add(groupId);

        return Result.Success(groupId);
    }

    public bool RemoveGroup(GroupId groupId) => _groups.Remove(groupId);

    public void ClearGroups() => _groups.Clear();

    #endregion

    #region Matchdays

    public Result<MatchdayId> AddMatchday(MatchdayId matchdayId)
    {
        _matchdays.Add(matchdayId);

        return Result.Success(matchdayId);
    }

    public bool RemoveMatchday(MatchdayId matchdayId) => _matchdays.Remove(matchdayId);

    public void ClearMatchays() => _matchdays.Clear();

    #endregion

    #region Penalty

    public virtual IReadOnlyDictionary<TeamId, int> GetPenaltyPoints() => _penaltyPoints;

    public virtual void AddPenalty(TeamId teamId, int points) => _ = _penaltyPoints.AddOrUpdate(teamId, points);

    public virtual bool RemovePenalty(TeamId team) => _penaltyPoints.Remove(team);

    public virtual void ClearPenaltyPoints() => _penaltyPoints.Clear();

    #endregion
}
