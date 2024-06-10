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

        public static Dictionary<MatchResultDetailled, int> DefaultPoints => new()
        {
                { MatchResultDetailled.Won, 3 },
                { MatchResultDetailled.Drawn, 1 },
                { MatchResultDetailled.Lost, 0 },
                { MatchResultDetailled.Withdrawn, -1 },
        };

        public static readonly RankingRules Default = new(DefaultPoints, RankingComparer.Default, DefaultComputers);

        public RankingRules(IReadOnlyDictionary<MatchResultDetailled, int> pointsNumberByResult,
                            RankingComparer comparer,
                            Dictionary<string, IRankingColumnComputer> computers)
        {
            PointsNumberByResult = pointsNumberByResult;
            Comparer = comparer;
            Computers = computers;
        }

        public IReadOnlyDictionary<MatchResultDetailled, int> PointsNumberByResult { get; }

        public RankingComparer Comparer { get; }

        public IReadOnlyDictionary<string, IRankingColumnComputer> Computers { get; }

        public int GetPoints(MatchResultDetailled result) => PointsNumberByResult.GetValueOrDefault(result);
    }
}
