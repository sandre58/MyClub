// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class RankingComparer : IComparer<RankingRow>
    {
        private readonly ICollection<RankingSortingColumn> _rankingSortingColumns;

        public RankingComparer(ICollection<RankingSortingColumn> rankingSortingColumns) => _rankingSortingColumns = rankingSortingColumns;

        public int Compare(RankingRow? x, RankingRow? y)
        {
            if (x == null || y == null || x == y || (x.Get(RankingColumn.Played) == 0 && y.Get(RankingColumn.Played) == 0)) return 0;

            foreach (var column in _rankingSortingColumns)
            {
                var result = 0;

                switch (column)
                {
                    case RankingSortingColumn.Points:
                        result = CompareTo(x, y, RankingColumn.Points);
                        break;
                    case RankingSortingColumn.GoalsFor:
                        result = CompareTo(x, y, RankingColumn.GoalsFor);
                        break;
                    case RankingSortingColumn.GoalsAgainst:
                        result = -CompareTo(x, y, RankingColumn.GoalsAgainst);
                        break;
                    case RankingSortingColumn.GoalDifference:
                        result = CompareTo(x, y, RankingColumn.GoalsDifference);
                        break;
                    case RankingSortingColumn.ResultsBetweenTeams:
                        var ranking = x.GetRankingAgainst(y.Team, RankingRules.ResultsBetweenTeams);
                        result = -ranking.GetRank(y.Team).CompareTo(ranking.GetRank(x.Team));
                        break;
                    case RankingSortingColumn.Wins:
                        result = CompareTo(x, y, RankingColumn.GamesWon);
                        break;
                }

                if (result != 0) return result;
            }

            return 0;
        }

        private static int CompareTo(RankingRow x, RankingRow y, RankingColumn column)
        {
            var xValue = x.Get(column);
            var yValue = y.Get(column);
            var result = yValue.CompareTo(xValue);

            return result;
        }
    }
}
