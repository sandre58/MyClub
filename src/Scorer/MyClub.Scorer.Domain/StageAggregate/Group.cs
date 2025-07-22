// -----------------------------------------------------------------------
// <copyright file="Group.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Domain.Primitives;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Domain.ValueObjects;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.StageAggregate;

public class Group : AuditableEntity<GroupId>, IChampionship
{
    private readonly List<TeamReference> _teams = [];
    private readonly GroupStage _groupStage;

    // <remarks>Used by EF Core</remarks>
    private Group()
        : base()
    {
        DisplayName = null!;
        _groupStage = null!;
    }

    private Group(GroupId id, GroupStage groupStage, IEnumerable<TeamReference> teams, string name, string? shortName)
        : base(id)
    {
        _groupStage = groupStage;
        DisplayName = new(name, shortName);
        _teams = [.. teams];
    }

    public static Group Create(GroupStage groupStage, IEnumerable<TeamReference> teams, string name, string? shortName = null)
        => new(GroupId.New(), groupStage, teams, name, shortName);

    public DisplayName DisplayName { get; set; }

    public IReadOnlyCollection<TeamReference> Teams => _teams.AsReadOnly();

    public StandingRuleSet StandingRules => _groupStage.StandingRules;

    public StandingRankStatuses StandingRankStatuses => _groupStage.StandingRankStatuses;

    public IReadOnlyCollection<MatchdayId> Matchdays => _groupStage.Matchdays;

    public IReadOnlyDictionary<TeamId, int> GetPenaltyPoints() => _groupStage.GetPenaltyPoints().Where(x => _teams.Contains(x.Key)).ToDictionary();

    public override string ToString() => DisplayName;
}
