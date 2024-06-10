// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace MyClub.Scorer.Domain.RankingAggregate
{
    public class RankingComparer : List<RankingRowComparer>, IComparer<RankingRow>
    {
        public static IDictionary<string, RankingRowComparer> AllAvailableComparers => new Dictionary<string, RankingRowComparer>
            {
                { nameof(RankingRowByPointsComparer), new RankingRowByPointsComparer() },
                { nameof(RankingRowByResultsBetweenTeamsComparer), new RankingRowByResultsBetweenTeamsComparer() },
                { nameof(RankingRowByGoalsDifferenceComparer), new RankingRowByGoalsDifferenceComparer() },
                { nameof(RankingRowByGoalsForComparer), new RankingRowByGoalsForComparer() },
                { nameof(RankingRowByGoalsAgainstComparer), new RankingRowByGoalsAgainstComparer() },
                { nameof(RankingRowByGamesWonComparer), new RankingRowByGamesWonComparer() },
                { nameof(RankingRowByGamesWonAfterShootoutsComparer), new RankingRowByGamesWonAfterShootoutsComparer() },
                { nameof(RankingRowByGamesLostComparer), new RankingRowByGamesLostComparer() },
                { nameof(RankingRowByGamesLostAfterShootoutsComparer), new RankingRowByGamesLostAfterShootoutsComparer() },
                { nameof(RankingRowByGamesWithdrawnComparer), new RankingRowByGamesWithdrawnComparer() },
            };

        public static RankingComparer Default => new(
            [
                new RankingRowByPointsComparer(),
                new RankingRowByGoalsDifferenceComparer(),
                new RankingRowByResultsBetweenTeamsComparer(),
                new RankingRowByGoalsForComparer(),
            ]);

        public RankingComparer(IEnumerable<RankingRowComparer> sortingRules) : base(sortingRules) { }

        public int Compare(RankingRow? x, RankingRow? y)
        {
            if (x == null || y == null || x == y || !x.GetMatches().Any() && !y.GetMatches().Any()) return 0;

            foreach (var rule in this)
            {
                var ruleResult = rule.Compare(x, y);

                if (ruleResult != 0) return -ruleResult;
            }

            return 0;
        }
    }
}
