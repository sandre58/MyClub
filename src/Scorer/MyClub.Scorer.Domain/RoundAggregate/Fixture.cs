// -----------------------------------------------------------------------
// <copyright file="Fixture.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.RoundAggregate;

public class Fixture : AuditableEntity<FixtureId>
{
    // <remarks>Used by EF Core</remarks>
    private Fixture()
        : base()
    {
        Team1 = null!;
        Team2 = null!;
    }

    private Fixture(FixtureId id, TeamReference team1, TeamReference team2)
        : base(id)
    {
        Team1 = team1;
        Team2 = team2;
    }

    public static Fixture Create(TeamReference team1, TeamReference team2) => new(FixtureId.New(), team1, team2);

    public TeamReference Team1 { get; }

    public TeamReference Team2 { get; }

    public bool Participate(TeamReference team) => GetTeams().Contains(team);

    public IEnumerable<TeamReference> GetTeams() => new List<TeamReference>() { Team1, Team2 }.Distinct();

    public override string ToString() => $"{Team1} vs {Team2}";
}
