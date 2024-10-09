// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class RoundStage : MatchesStage<MatchOfRound>
    {
        private readonly MatchFormat _matchFormat;

        public RoundStage(RoundOfFixtures stage, DateTime date, string name, string? shortName = null, MatchFormat? matchFormat = null, Guid? id = null) : base(date, name, shortName, id)
        {
            _matchFormat = matchFormat ?? stage.FixtureFormat;
            Stage = stage;
        }

        public RoundOfFixtures Stage { get; }

        protected override MatchOfRound Create(DateTime date, IVirtualTeam homeTeam, IVirtualTeam awayTeam) => new(Stage, date, homeTeam, awayTeam);

        public override MatchFormat ProvideFormat() => _matchFormat;

        public override MatchRules ProvideRules() => Stage.MatchRules;

        public override SchedulingParameters ProvideSchedulingParameters() => Stage.SchedulingParameters;

        public override IEnumerable<IVirtualTeam> ProvideTeams() => Stage.Teams.AsEnumerable();

        public override MatchOfRound AddMatch(MatchOfRound match) => !ReferenceEquals(match.Stage, this)
                ? throw new ArgumentException("Match stage is not this round stage", nameof(match))
                : base.AddMatch(match);
    }
}
