// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyNet.Utilities;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyClub.Scorer.Domain.Extensions;

namespace MyClub.Scorer.Domain.RankingAggregate
{
    public class RankingRow
    {
        public const string GamesWonAfterShootouts = "GamesWonAfterShootouts";
        public const string GamesLostAfterShootouts = "GamesLostAfterShootouts";

        private readonly Ranking _ranking;
        private readonly Func<Match, IVirtualTeam, bool> _filterMatches;
        private readonly Dictionary<string, object> _cache = [];
        private int? _points;

        internal RankingRow(Ranking ranking, IVirtualTeam team, Func<Match, IVirtualTeam, bool> filterMatches)
        {
            _ranking = ranking;
            _filterMatches = filterMatches ?? new Func<Match, IVirtualTeam, bool>((x, _) => x.State == MatchState.Played);
            Team = team;
        }

        public IVirtualTeam Team { get; }

        public IEnumerable<Match> GetMatches() => _ranking.GetMatches().Where(x => x.Participate(Team) && _filterMatches.Invoke(x, Team));

        public int GetPenaltyPoints() => _ranking.PenaltyPoints?.GetValueOrDefault(Team) ?? 0;

        public int GetPoints()
        {
            if (!_points.HasValue)
                ComputePoints();

            return _points!.Value;
        }

        public object? Get(string column)
        {
            if (!_cache.ContainsKey(column))
                Compute(column);

            return _cache.GetOrDefault(column);
        }

        public T? Get<T>(string column) => (T?)Get(column);

        public int Get(DefaultRankingColumn column) => Get<int?>(column.ToString()) ?? 0;

        public int CompareWith(IVirtualTeam team)
        {
            var ranking = GetRankingAgainst([team]);

            return -ranking.GetRank(Team).CompareTo(ranking.GetRank(team));
        }

        public Ranking GetRankingAgainst(IEnumerable<IVirtualTeam> teams)
        {
            var againstTeams = teams.Union([Team]).ToList();
            return new(againstTeams,
                       _ranking.GetMatches().AsEnumerable().Where(x => againstTeams.Contains(x.HomeTeam) && againstTeams.Contains(x.AwayTeam)),
                       new RankingRules(_ranking.Rules.PointsNumberByResult, new RankingComparer(
                       [
                           new RankingRowByPointsComparer(),
                           new RankingRowByGoalsDifferenceComparer(),
                       ]),
                       new Dictionary<string, IRankingColumnComputer>()
                       {
                           { DefaultRankingColumn.GoalsDifference.ToString(), RankingRules.CreateComputer(DefaultRankingColumn.GoalsDifference) }
                       }));
        }

        internal void Compute()
        {
            ComputePoints();
            _ranking.Rules.Computers.Values.ForEach(x => x.Compute(this));
        }

        private void Compute(string column)
        {
            var computers = _ranking.Rules.Computers;
            if (computers.ContainsKey(column))
                _cache.AddOrUpdate(column, computers[column].Compute(this));
        }

        private void ComputePoints() => _points = Team.GetTeam() is Team team ? GetMatches().Sum(x => _ranking.Rules.GetPoints(x.GetExtendedResultOf(team))) - GetPenaltyPoints() : 0;

        public override string ToString()
        {
            var str = new StringBuilder($"{Team} | {GetPoints()} PTS | ");

            str.Append(string.Join(" | ", _ranking.Rules.Computers.Select(x => $"{Get(x.Key)} ({x.Key})")));

            return str.ToString();
        }

        public override bool Equals(object? obj) => obj is RankingRow row && row.Team == Team;

        public override int GetHashCode() => Team.GetHashCode();
    }
}
