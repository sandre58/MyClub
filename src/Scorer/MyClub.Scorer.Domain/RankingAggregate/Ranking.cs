// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Domain.RankingAggregate
{
    public class Ranking : IEnumerable<RankingRow>
    {
        private readonly List<RankingRow> _rows = [];
        private readonly IEnumerable<Match> _matches;

        public Ranking(Championship championship, Func<Match, ITeam, bool>? filterMatches = null)
            : this(championship.Teams,
                   championship.GetMatches(),
                   championship.GetRankingRules(),
                   championship.GetPenaltyPoints(),
                   championship.Labels,
                   filterMatches)
        { }

        public Ranking(IEnumerable<Match> matches,
                       RankingRules? rankingRules = null,
                       IReadOnlyDictionary<ITeam, int>? penaltyPoints = null,
                       IReadOnlyDictionary<AcceptableValueRange<int>, RankLabel>? labels = null,
                       Func<Match, ITeam, bool>? filterMatches = null)
            : this(matches.SelectMany(x => new[] { x.HomeTeam, x.AwayTeam }).Distinct(),
                   matches,
                   rankingRules ?? RankingRules.Default,
                   penaltyPoints,
                   labels,
                   filterMatches)
        { }

        public Ranking(IEnumerable<ITeam> teams,
                       IEnumerable<Match> matches,
                       RankingRules? rankingRules = null,
                       IReadOnlyDictionary<ITeam, int>? penaltyPoints = null,
                       IReadOnlyDictionary<AcceptableValueRange<int>, RankLabel>? labels = null,
                       Func<Match, ITeam, bool>? filterMatches = null)
        {
            _rows.AddRange(teams.Select(x => new RankingRow(this, x, filterMatches ?? new Func<Match, ITeam, bool>((x, _) => x.State == MatchState.Played))));
            Rules = rankingRules ?? RankingRules.Default;
            PenaltyPoints = penaltyPoints;
            Labels = labels;
            _matches = matches;

            Compute();
        }

        public RankingRules Rules { get; set; }

        public IReadOnlyDictionary<ITeam, int>? PenaltyPoints { get; set; }

        public IReadOnlyDictionary<AcceptableValueRange<int>, RankLabel>? Labels { get; set; }

        public IEnumerable<Match> GetMatches() => _matches;

        public int GetRank(ITeam team) => _rows.Find(x => x.Team.Equals(team)) is RankingRow row ? _rows.IndexOf(row) + 1 : 0;

        public int GetRank(Guid teamId) => _rows.Find(x => x.Team.Id == teamId) is RankingRow row ? _rows.IndexOf(row) + 1 : 0;

        public RankLabel? GetLabel(ITeam team) => GetLabel(GetRank(team));

        public RankLabel? GetLabel(Guid teamId) => GetLabel(GetRank(teamId));

        public RankLabel? GetLabel(int rank) => Labels?.FirstOrDefault(x => x.Key.IsValid(rank)).Value;

        public RankingRow? GetRow(ITeam team) => _rows.Find(x => x.Team.Equals(team));

        public RankingRow? GetRow(Guid teamId) => _rows.Find(x => x.Team.Id == teamId);

        public T? GetColumn<T>(ITeam team, string column) => GetRow(team) is not RankingRow row ? default : row.Get<T>(column);

        public int GetColumn(ITeam team, DefaultRankingColumn column) => GetRow(team)?.Get(column) ?? 0;

        public void Compute()
        {
            _rows.ForEach(x => x.Compute());

            var comparer = new RankingComparer(Rules.Comparer);
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
