// -----------------------------------------------------------------------
// <copyright file="Round.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.RoundAggregate.Format;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Domain.ValueObjects;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Results;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.RoundAggregate;

public class Round : AuditableEntity<RoundId>
{
    private readonly List<TeamReference> _teams = [];
    private readonly List<RoundStage> _stages = [];
    private readonly List<Fixture> _fixtures = [];

    // <remarks>Used by EF Core</remarks>
    private Round()
        : base()
    {
        Format = null!;
        DisplayName = null!;
    }

    private Round(RoundId id,
                  RoundId? ancestorId,
                  string name,
                  string? shortName,
                  IRoundFormat format,
                  MatchRules? customMatchRules,
                  bool isConsolation)
        : base(id)
    {
        IsConsolation = isConsolation;
        AncestorRoundId = ancestorId;
        Format = format;
        CustomMatchRules = customMatchRules;
        DisplayName = new(name, shortName);
    }

    public static Round Create(RoundId? ancestorId,
                               IRoundFormat format,
                               string name,
                               string? shortName = null,
                               MatchRules? customMatchRules = null,
                               bool isConsolation = false)
        => new(RoundId.New(), ancestorId, name, shortName, format, customMatchRules, isConsolation);

    public DisplayName DisplayName { get; }

    public RoundId? AncestorRoundId { get; }

    public bool IsConsolation { get; private set; }

    public IRoundFormat Format { get; private set; }

    public MatchRules? CustomMatchRules { get; private set; }

    public IReadOnlyCollection<TeamReference> Teams => _teams.AsReadOnly();

    public IReadOnlyCollection<Fixture> Fixtures => _fixtures.AsReadOnly();

    public IReadOnlyCollection<RoundStage> Stages => _stages.AsReadOnly();

    #region Fixtures

    public virtual Result<Fixture> AddFixture(TeamReference team1, TeamReference team2) => AddFixture(Fixture.Create(team1, team2));

    public virtual Result<Fixture> AddFixture(Fixture fixture)
    {
        var teams = Fixtures.SelectMany(x => new List<TeamReference> { x.Team1, x.Team2 }).ToList();
        if (teams.Contains(fixture.Team1))
            return Failures.TeamIsAlreadyAssignedToFeature<Fixture>(fixture.Team1.ToString());
        if (teams.Contains(fixture.Team2))
            return Failures.TeamIsAlreadyAssignedToFeature<Fixture>(fixture.Team2.ToString());

        _fixtures.Add(fixture);

        return Result.Success(fixture);
    }

    public virtual bool RemoveFixture(Fixture item) => _fixtures.Remove(item);

    #endregion

    #region Teams

    public Result<TeamReference> AddTeam(TeamReference team)
    {
        if (Teams.Contains(team))
            return Failures.AlreadyExists<TeamReference>(team.ToString());

        _teams.Add(team);

        return Result.Success(team);
    }

    public virtual bool RemoveTeam(TeamReference team)
    {
        Fixtures.Where(x => x.Participate(team)).ToList().ForEach(y => RemoveFixture(y));
        return _teams.Remove(team);
    }

    #endregion
}
