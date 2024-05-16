// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class League : Competition<LeagueRules, LeagueSeason>
    {
        public League(string name, string shortName, Category category, LeagueRules? rules = null, Guid? id = null) : base(name, shortName, category, rules ?? LeagueRules.Default, id) { }

        public void AddSeason(Season season) => AddSeason(new LeagueSeason(this, season, Rules));
    }
}

