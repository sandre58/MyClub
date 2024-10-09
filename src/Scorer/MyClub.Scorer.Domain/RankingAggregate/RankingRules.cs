// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.Domain.Enums;

namespace MyClub.Scorer.Domain.RankingAggregate
{
    public class RankingRules : ValueObject
    {
        public static IRankingColumnComputer CreateComputer(DefaultRankingColumn rankingColumn)
            => rankingColumn switch
            {
                DefaultRankingColumn.GamesPlayed => new PlayedColumnComputer(),
                DefaultRankingColumn.GamesWon => new GamesWonColumnComputer(),
                DefaultRankingColumn.GamesWonAfterShootouts => new GamesWonAfterShootoutsColumnComputer(),
                DefaultRankingColumn.GamesDrawn => new GamesDrawnColumnComputer(),
                DefaultRankingColumn.GamesLost => new GamesLostColumnComputer(),
                DefaultRankingColumn.GamesLostAfterShootouts => new GamesLostAfterShootoutsColumnComputer(),
                DefaultRankingColumn.GamesWithdrawn => new GamesWithdrawnColumnComputer(),
                DefaultRankingColumn.GoalsFor => new GoalsForColumnComputer(),
                DefaultRankingColumn.GoalsAgainst => new GoalsAgainstColumnComputer(),
                DefaultRankingColumn.GoalsDifference => new GoalsDifferenceColumnComputer(),
                _ => throw new InvalidOperationException("Invalid RankingColumn"),
            };

        public static Dictionary<string, IRankingColumnComputer> DefaultComputers => Enum.GetValues<DefaultRankingColumn>().ToDictionary(x => x.ToString(), CreateComputer);

        public static Dictionary<ExtendedResult, int> DefaultPoints => new()
        {
                { ExtendedResult.Won, 3 },
                { ExtendedResult.Drawn, 1 },
                { ExtendedResult.Lost, 0 },
                { ExtendedResult.Withdrawn, -1 },
        };

        public static readonly RankingRules Default = new(DefaultPoints, RankingComparer.Default, DefaultComputers);

        public RankingRules(IReadOnlyDictionary<ExtendedResult, int> pointsNumberByResult,
                            RankingComparer comparer,
                            Dictionary<string, IRankingColumnComputer> computers)
        {
            PointsNumberByResult = pointsNumberByResult;
            Comparer = comparer;
            Computers = computers;
        }

        public IReadOnlyDictionary<ExtendedResult, int> PointsNumberByResult { get; }

        public RankingComparer Comparer { get; }

        public IReadOnlyDictionary<string, IRankingColumnComputer> Computers { get; }

        public int GetPoints(ExtendedResult result) => PointsNumberByResult.GetValueOrDefault(result);
    }
}
