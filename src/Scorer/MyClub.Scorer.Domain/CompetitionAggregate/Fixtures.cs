// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities.Collections;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class Fixtures : AuditableEntity, IMatchesProvider
    {
        private readonly ExtendedObservableCollection<Match> _matches = [];
        private readonly Round _parent;

        internal Fixtures(Round parent, ITeam team1, ITeam team2, Guid? id = null) : base(id)
        {
            _parent = parent;
            Team1 = team1;
            Team2 = team2;
            Matches = new(_matches);
        }

        public ITeam Team1 { get; }

        public ITeam Team2 { get; }

        public ReadOnlyObservableCollection<Match> Matches { get; }

        public bool IsPlayed() => Matches.All(x => x.State == MatchState.Played);

        public ITeam? GetWinner() => !IsPlayed() ? null : GetRanking().FirstOrDefault()?.Team;

        public ITeam? GetLooser() => !IsPlayed() ? null : GetRanking().LastOrDefault()?.Team;

        private Ranking GetRanking() => new([Team1, Team2], _matches, RankingRules.Default);

        public Match AddMatch(DateTime date, ITeam homeTeam, ITeam awayTeam) => AddMatch(new Match(date, homeTeam, awayTeam, _parent.MatchFormat));

        private Match AddMatch(Match match)
        {
            _matches.Add(match);

            return match;
        }

        public bool RemoveMatch(Match item) => _matches.Remove(item);

        MatchFormat IMatchFormatProvider.ProvideFormat() => _parent.MatchFormat;

        SchedulingParameters ISchedulingParametersProvider.ProvideSchedulingParameters() => _parent.SchedulingParameters;
    }
}
