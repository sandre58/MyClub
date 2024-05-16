// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Domain.Enums;

namespace MyClub.Scorer.Domain.RankingAggregate
{
    public abstract class RankingRowComparer : IComparer<RankingRow>
    {
        public int Compare(RankingRow? x, RankingRow? y) => x == null || y == null || x == y ? 0 : CompareTo(x, y);

        protected abstract int CompareTo(RankingRow x, RankingRow y);
    }

    public class RankingRowByComparableComparer : RankingRowComparer
    {
        private readonly Func<RankingRow, IComparable?> _compareExpression;

        public RankingRowByComparableComparer(Func<RankingRow, IComparable?> compareExpression) => _compareExpression = compareExpression;

        protected override int CompareTo(RankingRow x, RankingRow y)
        {
            var compareX = _compareExpression(x);
            var compareY = _compareExpression(y);
            return compareX == default && compareY == default ? 0 : compareX == default ? -1 : compareY == default ? 1 : compareX.CompareTo(compareY);
        }
    }

    public class RankingRowByColumnComparer : RankingRowByComparableComparer
    {
        private readonly bool _inverseResult;

        public RankingRowByColumnComparer(string column, bool inverseResult = false) : base(x => x.Get<IComparable>(column)) => _inverseResult = inverseResult;

        public RankingRowByColumnComparer(DefaultRankingColumn column, bool inverseResult = false) : base(x => x.Get(column)) => _inverseResult = inverseResult;

        protected override int CompareTo(RankingRow x, RankingRow y) => base.CompareTo(x, y) * (_inverseResult ? -1 : 1);
    }

    public class RankingRowByPointsComparer : RankingRowByComparableComparer
    {
        public RankingRowByPointsComparer() : base(x => x.GetPoints()) { }
    }

    public class RankingRowByGoalsForComparer : RankingRowByColumnComparer
    {
        public RankingRowByGoalsForComparer() : base(DefaultRankingColumn.GoalsFor) { }
    }

    public class RankingRowByGoalsAgainstComparer : RankingRowByColumnComparer
    {
        public RankingRowByGoalsAgainstComparer() : base(DefaultRankingColumn.GoalsFor, true) { }
    }

    public class RankingRowByGoalsDifferenceComparer : RankingRowByColumnComparer
    {
        public RankingRowByGoalsDifferenceComparer() : base(DefaultRankingColumn.GoalsDifference) { }
    }

    public class RankingRowByGamesWonComparer : RankingRowByColumnComparer
    {
        public RankingRowByGamesWonComparer() : base(DefaultRankingColumn.GamesWon) { }
    }

    public class RankingRowByGamesLostComparer : RankingRowByColumnComparer
    {
        public RankingRowByGamesLostComparer() : base(DefaultRankingColumn.GamesLost) { }
    }

    public class RankingRowByGamesWithdrawnComparer : RankingRowByColumnComparer
    {
        public RankingRowByGamesWithdrawnComparer() : base(DefaultRankingColumn.GamesWithdrawn) { }
    }

    public class RankingRowByResultsBetweenTeamsComparer : RankingRowComparer
    {
        protected override int CompareTo(RankingRow x, RankingRow y) => x.CompareWith(y.Team);
    }
}
