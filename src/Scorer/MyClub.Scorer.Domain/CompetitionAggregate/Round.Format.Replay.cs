// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.Extensions;
using System.Linq;
using System;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class ReplayFormat : IRoundFormat
    {
        public static readonly TwoLegsFormat Default = new(HalfFormat.Default);
        private readonly MatchFormat[] _matchFormats = new MatchFormat[2];

        public ReplayFormat(HalfFormat regulationTime, HalfFormat? extraTime = null, int numberOfPenaltyShootouts = 5)
        {
            RegulationTime = regulationTime;
            ExtraTime = extraTime;
            NumberOfPenaltyShootouts = numberOfPenaltyShootouts;

            _matchFormats[0] = new MatchFormat(regulationTime);
            _matchFormats[1] = new MatchFormat(regulationTime, extraTime, numberOfPenaltyShootouts);
        }

        public HalfFormat RegulationTime { get; }

        public HalfFormat? ExtraTime { get; }

        public int NumberOfPenaltyShootouts { get; }

        public bool CanAddStage(Round round) => round.Stages.Count < 2;

        public bool CanRemoveStage(RoundStage roundStage) => roundStage.Stage.Stages.IndexOf(roundStage) > 0;

        public bool AllowDraw() => true;

        public MatchFormat GetFormat(RoundStage stage)
        {
            var stageIndex = stage.Stage.Stages.IndexOf(stage);

            return _matchFormats[stageIndex >= 0 && stageIndex <= 1 ? stageIndex : 0];
        }

        public bool InvertTeams(RoundStage stage)
        {
            var stageIndex = stage.Stage.Stages.IndexOf(stage);

            return stageIndex == 1;
        }

        public ExtendedResult GetExtendedResultOf(Round round, IVirtualTeam team)
        {
            var availableMatches = round.Fixtures.Find(team)?.GetAllMatches().ToList() ?? [];
            if (availableMatches.Any(x => !x.HasResult())) return ExtendedResult.None;

            var goalsFor = availableMatches.Sum(x => x.GoalsFor(team));
            var goalsAgainst = availableMatches.Sum(x => x.GoalsAgainst(team));
            var shootoutFor = availableMatches.Sum(x => x.ShootoutFor(team));
            var shootoutAgainst = availableMatches.Sum(x => x.ShootoutAgainst(team));

            return goalsFor > goalsAgainst ? ExtendedResult.Won
                 : goalsFor < goalsAgainst ? ExtendedResult.Lost
                 : shootoutFor > shootoutAgainst ? ExtendedResult.WonAfterShootouts
                 : shootoutFor < shootoutAgainst ? ExtendedResult.LostAfterShootouts
                 : ExtendedResult.Drawn;
        }

        public void InitializeRound(Round round, DateTime[] dates) => round.AddStage(dates.GetByIndex(0, DateTime.Today));

        public bool CanAddFixtureInStage(RoundStage stage, Fixture fixture)
        {
            var stageIndex = stage.Stage.Stages.IndexOf(stage);

            return !stage.Matches.Select(x => x.Fixture).Contains(fixture) && (stageIndex == 0 || (fixture.IsPlayed() && fixture.GetWinner() is null));
        }

        public bool MustUseExtraTime(MatchOfFixture match) => match.Format.ExtraTimeIsEnabled && match.Fixture.IsDraw();

        public bool MustUseShootout(MatchOfFixture match) => match.Format.ShootoutIsEnabled && match.Fixture.IsDraw();
    }
}
