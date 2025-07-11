// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class RoundStage : MatchesStage<MatchOfFixture>
    {
        public RoundStage(Round stage, DateTime date, Guid? id = null) : base(date, id) => Stage = stage;

        public Round Stage { get; }

        protected override MatchOfFixture Create(DateTime date, IVirtualTeam homeTeam, IVirtualTeam awayTeam)
            => new(Stage.Fixtures.Find(homeTeam, awayTeam) ?? throw new InvalidOperationException("No fixture found with these teams"), this, date);

        public override MatchFormat ProvideFormat() => Stage.Format.GetFormat(this);

        public override MatchRules ProvideRules() => Stage.ProvideRules();

        public override SchedulingParameters ProvideSchedulingParameters() => Stage.ProvideSchedulingParameters();

        public MatchOfFixture AddMatch(Fixture fixture, DateTime? date = null, bool invertTeams = false) => AddMatch(new MatchOfFixture(fixture, this, date ?? Date, invertTeams));

        public override MatchOfFixture AddMatch(MatchOfFixture match) => !ReferenceEquals(match.Stage, this)
                ? throw new ArgumentException("Match stage is not this round", nameof(match))
                : base.AddMatch(match);

        public bool IsPlayed() => Matches.All(x => x.IsPlayed());
    }
}
