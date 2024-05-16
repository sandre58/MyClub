// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities;
using MyClub.Teamup.Domain.MatchAggregate;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class ChampionshipRules : CompetitionRules
    {
        public ChampionshipRules(MatchFormat? matchFormat = null, RankingRules? rankingRules = null, TimeSpan? matchTime = null) : base(matchFormat ?? MatchFormat.Default, matchTime ?? 15.Hours()) => RankingRules = rankingRules ?? RankingRules.Default;

        public RankingRules RankingRules { get; }
    }
}
