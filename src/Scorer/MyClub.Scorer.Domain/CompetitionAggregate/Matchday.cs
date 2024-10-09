// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class Matchday : MatchesStage<MatchOfMatchday>
    {
        public Matchday(IMatchdaysStage stage, DateTime date, string name, string? shortName = null, Guid? id = null) : base(date, name, shortName, id) => Stage = stage;

        public IMatchdaysStage Stage { get; }

        protected override MatchOfMatchday Create(DateTime date, IVirtualTeam homeTeam, IVirtualTeam awayTeam) => new(this, date, homeTeam, awayTeam);

        public override MatchFormat ProvideFormat() => Stage.ProvideFormat();

        public override MatchRules ProvideRules() => Stage.ProvideRules();

        public override SchedulingParameters ProvideSchedulingParameters() => Stage.ProvideSchedulingParameters();

        public override IEnumerable<IVirtualTeam> ProvideTeams() => Stage.ProvideTeams();

        public override MatchOfMatchday AddMatch(MatchOfMatchday match)
            => !ReferenceEquals(match.Stage, this)
                ? throw new ArgumentException("Match stage is not this matchday", nameof(match))
                : base.AddMatch(match);
    }
}
