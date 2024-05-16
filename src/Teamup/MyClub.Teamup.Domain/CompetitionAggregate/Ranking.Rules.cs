// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Domain.Enums;
using MyNet.Utilities;
using MyNet.Utilities.Sequences;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class RankingRules : ValueObject
    {
        public static readonly ICollection<RankingSortingColumn> DefaultSortingColumns = [RankingSortingColumn.Points, RankingSortingColumn.GoalDifference, RankingSortingColumn.ResultsBetweenTeams, RankingSortingColumn.GoalsFor];
        public static readonly RankingRules Default = new(3, 1, 0, DefaultSortingColumns);
        public static readonly RankingRules ResultsBetweenTeams = new(3, 1, 0, [RankingSortingColumn.Points, RankingSortingColumn.GoalDifference]);

        public RankingRules(int pointsByGamesWon,
                            int pointsByGamesDrawn,
                            int pointsByGamesLost,
                            ICollection<RankingSortingColumn> sortingColumns,
                            IDictionary<AcceptableValueRange<int>, RankLabel>? rankLabels = null)
        {
            PointsByGamesWon = pointsByGamesWon;
            PointsByGamesDrawn = pointsByGamesDrawn;
            PointsByGamesLost = pointsByGamesLost;
            SortingColumns = [.. sortingColumns];
            Labels = rankLabels ?? new Dictionary<AcceptableValueRange<int>, RankLabel>();
        }

        public int PointsByGamesWon { get; }

        public int PointsByGamesDrawn { get; }

        public int PointsByGamesLost { get; }

        public ICollection<RankingSortingColumn> SortingColumns { get; }

        public IDictionary<AcceptableValueRange<int>, RankLabel> Labels { get; }
    }
}
