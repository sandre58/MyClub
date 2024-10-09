// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public class MatchOfRound : Match
    {
        public MatchOfRound(IRound stage, DateTime date, IVirtualTeam homeTeam, IVirtualTeam awayTeam, MatchFormat? matchFormat = null, Guid? id = null)
            : base(date, homeTeam, awayTeam, matchFormat ?? MatchFormat.NoDraw, stage.ProvideRules(), id) => Stage = stage;

        public IRound Stage { get; }
    }
}
