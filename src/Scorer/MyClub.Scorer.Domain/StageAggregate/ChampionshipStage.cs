// -----------------------------------------------------------------------
// <copyright file="ChampionshipStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Domain.Primitives;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Kernel.Results;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.StageAggregate;

public class ChampionshipStage : Stage, IChampionship
{
    private readonly List<MatchdayId> _matchdays = [];
    private readonly Dictionary<TeamId, int> _penaltyPoints = [];

    // <remarks>Used by EF Core</remarks>
    private ChampionshipStage()
        : base()
    {
        StandingRules = null!;
        StandingRankStatuses = null!;
    }

    private ChampionshipStage(StageId id, StageId? ancestorStageId, string name, string? shortName, MatchFormat? format, MatchRules? rules, StandingRuleSet standingRules, StandingRankStatuses standingRankStatuses, bool isConsolation)
        : base(id, ancestorStageId, name, shortName, format, rules, isConsolation)
    {
        StandingRules = standingRules;
        StandingRankStatuses = standingRankStatuses;
    }

    public static ChampionshipStage Create(StageId? ancestorStageId, string name, string? shortName = null, MatchFormat? format = null, MatchRules? rules = null, StandingRuleSet? standingRules = null, StandingRankStatuses? standingRankStatuses = null, bool isConsolation = false)
        => new(StageId.New(), ancestorStageId, name, shortName, format, rules, standingRules ?? StandingRuleSet.Default, standingRankStatuses ?? [], isConsolation);

    public IReadOnlyCollection<MatchdayId> Matchdays => _matchdays.AsReadOnly();

    public StandingRuleSet StandingRules { get; set; }

    public StandingRankStatuses StandingRankStatuses { get; set; }

    public override StageType Type => StageType.Championship;

    #region Matchdays

    public Result<MatchdayId> AddMatchday(MatchdayId matchdayId)
    {
        _matchdays.Add(matchdayId);

        return Result.Success(matchdayId);
    }

    public bool RemoveMatchday(MatchdayId matchdayId) => _matchdays.Remove(matchdayId);

    public void Clear() => _matchdays.Clear();

    #endregion

    #region Penalty

    public virtual IReadOnlyDictionary<TeamId, int> GetPenaltyPoints() => _penaltyPoints;

    public virtual void AddPenalty(TeamId teamId, int points) => _ = _penaltyPoints.AddOrUpdate(teamId, points);

    public virtual bool RemovePenalty(TeamId team) => _penaltyPoints.Remove(team);

    public virtual void ClearPenaltyPoints() => _penaltyPoints.Clear();

    #endregion
}
