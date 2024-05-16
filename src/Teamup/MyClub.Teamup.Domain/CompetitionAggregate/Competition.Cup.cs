// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class Cup : Competition<CupRules, CupSeason>
    {
        public Cup(string name, string shortName, Category category, CupRules? rules = null, Guid? id = null) : base(name, shortName, category, rules ?? CupRules.Default, id) { }

        public CupSeason AddSeason(Season season) => AddSeason(new CupSeason(this, season, Rules));
    }
}

