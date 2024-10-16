// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class RoundStage : MatchesStage<MatchOfFixture>
    {
        public RoundStage(RoundOfFixtures stage, DateTime date, string name, string? shortName = null, Guid? id = null) : base(date, name, shortName, id) => Stage = stage;

        public RoundOfFixtures Stage { get; }

        protected override MatchOfFixture Create(DateTime date, IVirtualTeam homeTeam, IVirtualTeam awayTeam)
            => new(Stage.Fixtures.Find(homeTeam, awayTeam) ?? throw new InvalidOperationException("No fixture found with these teams"), date, ProvideFormat());

        public override MatchFormat ProvideFormat() => Stage.ProvideMatchFormat(this);

        public override MatchRules ProvideRules() => Stage.MatchRules;

        public override SchedulingParameters ProvideSchedulingParameters() => Stage.SchedulingParameters;

        public override IEnumerable<IVirtualTeam> ProvideTeams() => Stage.Teams.AsEnumerable();

        public MatchOfFixture AddMatch(Fixture fixture, bool invertTeams = false) => AddMatch(new MatchOfFixture(fixture, Date, ProvideFormat(), invertTeams));

        public override MatchOfFixture AddMatch(MatchOfFixture match) => !ReferenceEquals(match.Stage, Stage)
                ? throw new ArgumentException("Match stage is not this round", nameof(match))
                : base.AddMatch(match);
    }
}
