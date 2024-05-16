// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyNet.Utilities;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class Ranking : IEnumerable<RankingRow>
    {
        private readonly List<RankingRow> _rows = [];
        private readonly IEnumerable<Match> _matches;
        private readonly Func<Match, Team, bool> _filterMatches;

        public Ranking(IChampionship championship, Func<Match, Team, bool>? filterMatches = null)
            : this(championship.Teams, championship.GetAllMatches(), championship.Rules.RankingRules, championship.Penalties, filterMatches) { }

        public Ranking(IEnumerable<Match> matches, RankingRules? rankingRules = null, IDictionary<Team, int>? penalties = null, Func<Match, Team, bool>? filterMatches = null)
            : this(matches.SelectMany(x => new[] { x.HomeTeam, x.AwayTeam }).Distinct(), matches, rankingRules ?? RankingRules.Default, penalties, filterMatches) { }

        public Ranking(IEnumerable<Team> teams, IEnumerable<Match> matches, RankingRules? rankingRules = null, IDictionary<Team, int>? penalties = null, Func<Match, Team, bool>? filterMatches = null)
        {
            _rows.AddRange(teams.Select(x => new RankingRow(x)));
            Rules = rankingRules ?? RankingRules.Default;
            Penalties = penalties;
            _matches = matches;
            _filterMatches = filterMatches ?? new Func<Match, Team, bool>((x, _) => x.State == MatchState.Played);

            Compute();
        }

        public RankingRules Rules { get; set; }

        public IDictionary<Team, int>? Penalties { get; set; }

        public int GetRank(Team team) => _rows.Find(x => x.Team == team) is RankingRow row ? _rows.IndexOf(row) + 1 : 0;

        public int GetRank(Guid teamId) => _rows.Find(x => x.Team.Id == teamId) is RankingRow row ? _rows.IndexOf(row) + 1 : 0;

        public RankingRow? GetRow(Team team) => _rows.Find(x => x.Team == team);

        public RankingRow? GetRow(Guid teamId) => _rows.Find(x => x.Team.Id == teamId);

        public void Compute() => Compute(_matches.ToList(), Rules, Penalties);

        private void Compute(ICollection<Match> matches, RankingRules rules, IDictionary<Team, int>? penalties = null)
        {
            foreach (var row in _rows)
            {
                var filteredMatches = matches.Where(x => x.Participate(row.Team) && _filterMatches.Invoke(x, row.Team));
                row.Compute(filteredMatches, rules, penalties);
            }

            var comparer = new RankingComparer(rules.SortingColumns);
            _rows.Sort(comparer);
        }

        public IEnumerator<RankingRow> GetEnumerator() => _rows.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            var str = new StringBuilder();

            this.ForEach((x, index) => str.AppendLine($"{index + 1} : {x.ToString()}"));

            return str.ToString();
        }
    }
}
