// -----------------------------------------------------------------------
// <copyright file="Competition.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.CompetitionAggregate.Stadiums;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Scorer.Domain.Primitives;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Domain.ValueObjects;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Domain.CompetitionAggregate;

public abstract class Competition : AuditableEntity<CompetitionId>, ITeamsContainer
{
    private readonly List<Team> _teams = [];
    private readonly List<Stadium> _stadiums = [];

    // <remarks>Used by EF Core</remarks>
    protected Competition()
        : base()
    {
        MatchFormat = null!;
        MatchRules = null!;
        DisplayName = null!;
    }

    protected Competition(CompetitionId id, string name, string? shortName, MatchFormat format, MatchRules rules)
        : base(id)
    {
        DisplayName = new(name, shortName);
        MatchFormat = format;
        MatchRules = rules;
    }

    public DisplayName DisplayName { get; }

    public byte? Logo { get; set; }

    public MatchFormat MatchFormat { get; set; }

    public MatchRules MatchRules { get; set; }

    public abstract CompetitionType Type { get; }

    public IReadOnlyCollection<Team> Teams => _teams.AsReadOnly();

    public IReadOnlyCollection<Stadium> Stadiums => _stadiums.AsReadOnly();

    IReadOnlyCollection<TeamReference> ITeamsContainer.Teams => Teams.Select(x => new ConcreteTeamReference(x.Id)).ToList().AsReadOnly();

    #region Teams

    public virtual void AddTeam(string name, string? shortName = null) => AddTeam(Team.Create(name, shortName));

    public virtual Result<Team> AddTeam(Team team)
    {
        if (_teams.Contains(team))
            return Failures.AlreadyExists<Team>(team.DisplayName);

        _teams.Add(team);

        return Result.Success(team);
    }

    public virtual bool RemoveTeam(Team team) => _teams.Remove(team);

    public bool HasSimilarTeams(string name, TeamId? excludeTeamId = null) => Teams.Any(t => !t.Id.Equals(excludeTeamId ?? TeamId.Empty) && t.DisplayName.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    #endregion

    #region Stadiums

    public virtual Result<Stadium> AddStadium(string name, Ground ground = Ground.Grass) => AddStadium(Stadium.Create(name, ground));

    public virtual Result<Stadium> AddStadium(Stadium stadium)
    {
        if (_stadiums.Contains(stadium))
            return Failures.AlreadyExists<Stadium>(stadium.DisplayName);

        _stadiums.Add(stadium);

        return Shared.Kernel.Results.Result.Success(stadium);
    }

    public virtual bool RemoveStadium(Stadium stadium) => _stadiums.Remove(stadium);

    public bool HasStadium(StadiumId id) => Stadiums.Any(s => s.Id == id);

    #endregion
}
