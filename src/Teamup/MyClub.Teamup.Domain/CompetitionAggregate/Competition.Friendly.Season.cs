// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.Teamup.Domain.TeamAggregate;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class FriendlySeason : CompetitionSeason<FriendlyRules>, IHasMatches
    {
        private readonly ObservableCollection<Match> _matches = [];

        public FriendlySeason(Friendly competition, Season season, FriendlyRules rules, DateTime? startDate = null, DateTime? endDate = null, Guid? id = null)
            : base(competition, season, rules, startDate, endDate, id) => Matches = new(_matches);

        public ReadOnlyObservableCollection<Match> Matches { get; }

        MatchFormat IHasMatches.MatchFormat => Rules.MatchFormat;

        public override IEnumerable<Match> GetAllMatches() => [.. Matches];

        public Match AddMatch(DateTime date, Team homeTeam, Team awayTeam) => AddMatch(new Match(date, homeTeam, awayTeam, Rules.MatchFormat));

        public Match AddMatch(Match match)
        {
            _matches.Add(match);

            return match;
        }

        public override bool RemoveMatch(Match match) => _matches.Remove(match);
    }
}
