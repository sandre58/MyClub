// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Teamup.Domain.MatchAggregate;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class LeagueRules : ChampionshipRules
    {
        public static readonly LeagueRules Default = new();

        public LeagueRules(MatchFormat? matchFormat = null, RankingRules? rankingRules = null, TimeSpan? matchTime = null) : base(matchFormat, rankingRules, matchTime) { }
    }
}
