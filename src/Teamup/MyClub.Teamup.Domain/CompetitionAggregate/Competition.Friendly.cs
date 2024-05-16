// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class Friendly : Competition<FriendlyRules, FriendlySeason>
    {
        public Friendly(string name, string shortName, Category category, FriendlyRules? rules = null, Guid? id = null) : base(name, shortName, category, rules ?? FriendlyRules.Default, id) { }

        public void AddSeason(Season season) => AddSeason(new FriendlySeason(this, season, Rules));
    }
}
