// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public class MatchOfMatchday : Match
    {
        public MatchOfMatchday(Matchday stage, DateTime date, IVirtualTeam homeTeam, IVirtualTeam awayTeam, Guid? id = null)
            : base(date, homeTeam, awayTeam, stage.ProvideFormat(), stage.ProvideRules(), id) => Stage = stage;

        public Matchday Stage { get; }

        public override MatchFormat Format => Stage.ProvideFormat();

        public override MatchRules Rules => Stage.ProvideRules();
    }
}
