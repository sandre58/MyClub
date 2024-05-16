// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyNet.Utilities;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class RankingRow
    {
        private readonly IDictionary<RankingColumn, int> _columns = new Dictionary<RankingColumn, int>();

        internal RankingRow(Team team)
        {
            Team = team;
            Matches = new List<Match>().AsReadOnly();
        }

        public Team Team { get; }

        public IReadOnlyCollection<Match> Matches { get; private set; }

        public int Get(RankingColumn column) => _columns.GetOrDefault(column);

        private void Set(RankingColumn column, int value) => _columns.AddOrUpdate(column, value);

        internal void Compute(IEnumerable<Match> matches, RankingRules rules, IDictionary<Team, int>? penalties = null)
        {
            Matches = matches.OrderBy(x => x.Date).ToList().AsReadOnly();

            Set(RankingColumn.Played, Matches.Count);
            Set(RankingColumn.GamesWon, Matches.Count(x => x.IsWonBy(Team)));
            Set(RankingColumn.GamesDrawn, Matches.Count(x => x.GetResultOf(Team) == MatchResult.Drawn));
            Set(RankingColumn.GamesLost, Matches.Count(x => x.IsLostBy(Team)));
            Set(RankingColumn.GoalsFor, Matches.Sum(x => x.GoalsFor(Team)));
            Set(RankingColumn.GoalsAgainst, Matches.Sum(x => x.GoalsAgainst(Team)));
            Set(RankingColumn.GoalsDifference, Get(RankingColumn.GoalsFor) - Get(RankingColumn.GoalsAgainst));
            Set(RankingColumn.GamesWithdrawn, Matches.Count(x => x.IsWithdrawn(Team)));
            Set(RankingColumn.PenaltyPoints, (penalties?.GetOrDefault(Team, 0) ?? 0) + Matches.Sum(x => x.GetPenaltyPoints(Team)));
            Set(RankingColumn.Points, Get(RankingColumn.GamesWon) * rules.PointsByGamesWon + Get(RankingColumn.GamesDrawn) * rules.PointsByGamesDrawn + Get(RankingColumn.GamesLost) * rules.PointsByGamesLost - Get(RankingColumn.PenaltyPoints));
        }

        public Ranking GetRankingAgainst(Team team, RankingRules rules)
        {
            var matches = Matches.Where(x => x.Participate(team)).ToList();
            return new Ranking([Team, team], matches, rules);
        }

        public override string ToString()
        {
            var str = new StringBuilder($"{Team}" +
                $" | {Get(RankingColumn.Played)} PLD" +
                $" | {Get(RankingColumn.Points)} PTS" +
                $" | {Get(RankingColumn.GamesWon)} W" +
                $" | {Get(RankingColumn.GamesDrawn)} D" +
                $" | {Get(RankingColumn.GamesLost)} L" +
                $" | {Get(RankingColumn.GamesWithdrawn)} WD" +
                $" | {Get(RankingColumn.PenaltyPoints)} P" +
                $" | {Get(RankingColumn.GoalsFor)} GF" +
                $" | {Get(RankingColumn.GoalsAgainst)} GA" +
                $" | {Get(RankingColumn.GoalsDifference)} GD");

            return str.ToString();
        }

        public override bool Equals(object? obj) => obj is RankingRow row && row.Team == Team;

        public override int GetHashCode() => Team.GetHashCode();
    }
}
